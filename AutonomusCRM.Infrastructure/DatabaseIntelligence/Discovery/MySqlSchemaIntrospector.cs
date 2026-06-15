using AutonomusCRM.Application.DatabaseIntelligence;
using MySqlConnector;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

internal sealed class MySqlSchemaIntrospector : IDbSchemaIntrospector
{
    public DbEngineType EngineType => DbEngineType.MySQL;

    public async Task<PhysicalSchemaDiscoveryResult> DiscoverAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken = default)
        => await DiscoverMySqlFamilyAsync(endpoint, secrets, readOnly, timeoutSeconds, progress, cancellationToken);

    internal static Task<PhysicalSchemaDiscoveryResult> DiscoverMySqlFamilyAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken,
        DbEngineType engineType = DbEngineType.MySQL)
    {
        return DiscoverCoreAsync(endpoint, secrets, readOnly, timeoutSeconds, progress, cancellationToken);
    }

    internal static PhysicalSchemaDiscoveryResult ApplyFixture(PhysicalSchemaDiscoveryResult fixture)
    {
        DbRelationshipHeuristics.ApplyNamingHeuristics(fixture);
        return fixture;
    }

    private static async Task<PhysicalSchemaDiscoveryResult> DiscoverCoreAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken)
    {
        var result = new PhysicalSchemaDiscoveryResult();
        var builder = new MySqlConnectionStringBuilder
        {
            Server = endpoint.Host,
            Port = (uint)endpoint.Port,
            Database = endpoint.DatabaseName,
            UserID = endpoint.Username,
            Password = secrets.Password,
            ConnectionTimeout = (uint)timeoutSeconds,
            DefaultCommandTimeout = (uint)timeoutSeconds,
            Pooling = false
        };

        await using var conn = new MySqlConnection(builder.ConnectionString);
        await conn.OpenAsync(cancellationToken);
        if (readOnly)
        {
            await using var ro = new MySqlCommand("SET SESSION TRANSACTION READ ONLY", conn);
            await ro.ExecuteNonQueryAsync(cancellationToken);
        }

        progress?.Report(new DbDiscoveryProgress("Connecting", 5, Message: "Connected to MySQL/MariaDB"));
        await LoadTablesAsync(conn, endpoint.DatabaseName, result, progress, cancellationToken);
        await LoadColumnsAsync(conn, endpoint.DatabaseName, result, cancellationToken);
        await LoadKeysAsync(conn, endpoint.DatabaseName, result, cancellationToken);
        await LoadIndexesAsync(conn, endpoint.DatabaseName, result, cancellationToken);
        DbRelationshipHeuristics.ApplyNamingHeuristics(result);
        progress?.Report(new DbDiscoveryProgress("Completed", 100));
        return result;
    }

    private static async Task LoadTablesAsync(MySqlConnection conn, string db, PhysicalSchemaDiscoveryResult result,
        IProgress<DbDiscoveryProgress>? progress, CancellationToken ct)
    {
        var sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE, IFNULL(TABLE_ROWS, 0)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @db
            ORDER BY TABLE_NAME
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@db", db);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var type = reader.GetString(2);
            var rows = reader.IsDBNull(3) ? 0L : Convert.ToInt64(reader.GetValue(3));
            var objectType = type.Contains("VIEW", StringComparison.OrdinalIgnoreCase)
                ? DbCatalogObjectTypes.View : DbCatalogObjectTypes.Table;
            result.Schemas.Add(new PhysicalSchemaInfo { SchemaName = schema });
            result.Tables.Add(new PhysicalTableInfo
            {
                SchemaName = schema,
                ObjectName = name,
                ObjectType = objectType,
                EstimatedRowCount = rows
            });
            progress?.Report(new DbDiscoveryProgress("TableDiscovered", 30, schema, name, ObjectType: objectType));
        }
    }

    private static async Task LoadColumnsAsync(MySqlConnection conn, string db, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        var sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT, ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db
            ORDER BY TABLE_NAME, ORDINAL_POSITION
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@db", db);
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

    private static async Task LoadKeysAsync(MySqlConnection conn, string db, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        var sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, REFERENCED_TABLE_SCHEMA, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = @db AND REFERENCED_TABLE_NAME IS NOT NULL
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@db", db);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var fromSchema = reader.GetString(0);
            var fromTable = reader.GetString(1);
            var fromColumn = reader.GetString(2);
            var col = result.Columns.FirstOrDefault(c =>
                c.SchemaName == fromSchema && c.ObjectName == fromTable && c.ColumnName == fromColumn);
            if (col != null) col.IsForeignKey = true;
            result.Relationships.Add(new PhysicalRelationshipInfo
            {
                FromSchema = fromSchema,
                FromTable = fromTable,
                FromColumn = fromColumn,
                ToSchema = reader.GetString(3),
                ToTable = reader.GetString(4),
                ToColumn = reader.GetString(5),
                Source = DbRelationshipSource.ExplicitForeignKey,
                ConfidencePercent = 100
            });
        }
    }

    private static async Task LoadIndexesAsync(MySqlConnection conn, string db, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        var sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, INDEX_NAME, NON_UNIQUE, COLUMN_NAME
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = @db
            ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@db", db);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var indexName = reader.GetString(2);
            var colName = reader.GetString(4);
            var col = result.Columns.FirstOrDefault(c =>
                c.SchemaName == schema && c.ObjectName == table && c.ColumnName == colName);
            if (col != null) col.IsIndexed = true;
            var existing = result.Indexes.FirstOrDefault(i =>
                i.SchemaName == schema && i.ObjectName == table && i.IndexName == indexName);
            if (existing == null)
            {
                result.Indexes.Add(new PhysicalIndexInfo
                {
                    SchemaName = schema,
                    ObjectName = table,
                    IndexName = indexName,
                    IsUnique = reader.GetInt32(3) == 0,
                    ColumnNames = [colName]
                });
            }
        }
    }
}

internal sealed class MariaDbSchemaIntrospector : IDbSchemaIntrospector
{
    public DbEngineType EngineType => DbEngineType.MariaDB;

    public Task<PhysicalSchemaDiscoveryResult> DiscoverAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken = default)
        => MySqlSchemaIntrospector.DiscoverMySqlFamilyAsync(
            endpoint, secrets, readOnly, timeoutSeconds, progress, cancellationToken, DbEngineType.MariaDB);
}
