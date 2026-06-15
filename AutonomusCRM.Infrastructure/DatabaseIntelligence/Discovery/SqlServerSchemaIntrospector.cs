using AutonomusCRM.Application.DatabaseIntelligence;
using Microsoft.Data.SqlClient;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

internal sealed class SqlServerSchemaIntrospector : IDbSchemaIntrospector
{
    public DbEngineType EngineType => DbEngineType.SqlServer;

    public async Task<PhysicalSchemaDiscoveryResult> DiscoverAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var result = new PhysicalSchemaDiscoveryResult();
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{endpoint.Host},{endpoint.Port}",
            InitialCatalog = endpoint.DatabaseName,
            UserID = endpoint.Username,
            Password = secrets.Password,
            ConnectTimeout = timeoutSeconds,
            TrustServerCertificate = true,
            Pooling = false
        };
        if (readOnly) builder.ApplicationIntent = ApplicationIntent.ReadOnly;

        await using var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync(cancellationToken);
        progress?.Report(new DbDiscoveryProgress("Connecting", 5, Message: "Connected to SQL Server"));

        await LoadSchemasAsync(conn, result, progress, cancellationToken);
        await LoadTablesAndViewsAsync(conn, result, progress, cancellationToken);
        await LoadColumnsAsync(conn, result, cancellationToken);
        await LoadKeysAsync(conn, result, cancellationToken);
        await LoadIndexesAsync(conn, result, cancellationToken);
        DbRelationshipHeuristics.ApplyNamingHeuristics(result);
        progress?.Report(new DbDiscoveryProgress("Completed", 100));
        return result;
    }

    internal static PhysicalSchemaDiscoveryResult ApplyFixture(PhysicalSchemaDiscoveryResult fixture)
    {
        DbRelationshipHeuristics.ApplyNamingHeuristics(fixture);
        return fixture;
    }

    private static async Task LoadSchemasAsync(SqlConnection conn, PhysicalSchemaDiscoveryResult result,
        IProgress<DbDiscoveryProgress>? progress, CancellationToken ct)
    {
        const string sql = "SELECT DISTINCT TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_SCHEMA";
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            result.Schemas.Add(new PhysicalSchemaInfo { SchemaName = schema });
            progress?.Report(new DbDiscoveryProgress("SchemaDiscovered", 15, schema));
        }
    }

    private static async Task LoadTablesAndViewsAsync(SqlConnection conn, PhysicalSchemaDiscoveryResult result,
        IProgress<DbDiscoveryProgress>? progress, CancellationToken ct)
    {
        const string sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
            FROM INFORMATION_SCHEMA.TABLES
            ORDER BY TABLE_SCHEMA, TABLE_NAME
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var type = reader.GetString(2);
            var objectType = type.Contains("VIEW", StringComparison.OrdinalIgnoreCase)
                ? DbCatalogObjectTypes.View : DbCatalogObjectTypes.Table;
            result.Tables.Add(new PhysicalTableInfo { SchemaName = schema, ObjectName = name, ObjectType = objectType });
            progress?.Report(new DbDiscoveryProgress("TableDiscovered", 30, schema, name, ObjectType: objectType));
        }
    }

    private static async Task LoadColumnsAsync(SqlConnection conn, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT, ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.COLUMNS
            ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Columns.Add(new PhysicalColumnInfo
            {
                SchemaName = reader.GetString(0),
                ObjectName = reader.GetString(1),
                ColumnName = reader.GetString(2),
                DataType = reader.GetString(3),
                IsNullable = reader.GetString(4).Equals("YES", StringComparison.OrdinalIgnoreCase),
                DefaultValue = reader.IsDBNull(5) ? null : reader.GetString(5),
                Ordinal = reader.GetInt32(6)
            });
        }
    }

    private static async Task LoadKeysAsync(SqlConnection conn, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT
                fk.name AS fk_name,
                sch_from.name AS from_schema, t_from.name AS from_table, c_from.name AS from_column,
                sch_to.name AS to_schema, t_to.name AS to_table, c_to.name AS to_column
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables t_from ON fkc.parent_object_id = t_from.object_id
            INNER JOIN sys.schemas sch_from ON t_from.schema_id = sch_from.schema_id
            INNER JOIN sys.columns c_from ON fkc.parent_object_id = c_from.object_id AND fkc.parent_column_id = c_from.column_id
            INNER JOIN sys.tables t_to ON fkc.referenced_object_id = t_to.object_id
            INNER JOIN sys.schemas sch_to ON t_to.schema_id = sch_to.schema_id
            INNER JOIN sys.columns c_to ON fkc.referenced_object_id = c_to.object_id AND fkc.referenced_column_id = c_to.column_id
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var fromSchema = reader.GetString(1);
            var fromTable = reader.GetString(2);
            var fromColumn = reader.GetString(3);
            var col = result.Columns.FirstOrDefault(c =>
                c.SchemaName == fromSchema && c.ObjectName == fromTable && c.ColumnName == fromColumn);
            if (col != null) col.IsForeignKey = true;
            result.Relationships.Add(new PhysicalRelationshipInfo
            {
                FromSchema = fromSchema,
                FromTable = fromTable,
                FromColumn = fromColumn,
                ToSchema = reader.GetString(4),
                ToTable = reader.GetString(5),
                ToColumn = reader.GetString(6),
                Source = DbRelationshipSource.ExplicitForeignKey,
                ConfidencePercent = 100
            });
        }

        const string pkSql = """
            SELECT s.name, t.name, c.name, i.name
            FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE i.is_primary_key = 1
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(pkSql);
        await using var pkCmd = new SqlCommand(pkSql, conn);
        await using var pkReader = await pkCmd.ExecuteReaderAsync(ct);
        while (await pkReader.ReadAsync(ct))
        {
            var schema = pkReader.GetString(0);
            var table = pkReader.GetString(1);
            var column = pkReader.GetString(2);
            var col = result.Columns.FirstOrDefault(c =>
                c.SchemaName == schema && c.ObjectName == table && c.ColumnName == column);
            if (col != null) col.IsPrimaryKey = true;
        }
    }

    private static async Task LoadIndexesAsync(SqlConnection conn, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT s.name, t.name, i.name, i.is_unique
            FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE i.name IS NOT NULL AND i.is_primary_key = 0
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Indexes.Add(new PhysicalIndexInfo
            {
                SchemaName = reader.GetString(0),
                ObjectName = reader.GetString(1),
                IndexName = reader.GetString(2),
                IsUnique = reader.GetBoolean(3),
                ColumnNames = Array.Empty<string>()
            });
        }
    }
}
