using AutonomusCRM.Application.DatabaseIntelligence;
using Npgsql;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

internal sealed class PostgreSqlSchemaIntrospector : IDbSchemaIntrospector
{
    public DbEngineType EngineType => DbEngineType.PostgreSQL;

    public async Task<PhysicalSchemaDiscoveryResult> DiscoverAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var result = new PhysicalSchemaDiscoveryResult();
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = endpoint.Host,
            Port = endpoint.Port,
            Database = endpoint.DatabaseName,
            Username = endpoint.Username,
            Password = secrets.Password,
            Timeout = timeoutSeconds,
            CommandTimeout = timeoutSeconds,
            Pooling = false
        };
        if (readOnly)
            builder.Options = "-c default_transaction_read_only=on";

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync(cancellationToken);
        progress?.Report(new DbDiscoveryProgress("Connecting", 5, Message: "Connected to PostgreSQL"));

        await LoadSchemasAsync(conn, result, progress, cancellationToken);
        await LoadTablesAndViewsAsync(conn, result, progress, cancellationToken);
        await LoadColumnsAsync(conn, result, progress, cancellationToken);
        await LoadPrimaryKeysAsync(conn, result, cancellationToken);
        await LoadForeignKeysAsync(conn, result, cancellationToken);
        await LoadIndexesAsync(conn, result, cancellationToken);
        await LoadRowCountsAsync(conn, result, cancellationToken);

        MarkIndexedColumns(result);
        DbRelationshipHeuristics.ApplyNamingHeuristics(result);
        progress?.Report(new DbDiscoveryProgress("Completed", 100, Message: "Discovery finished"));
        return result;
    }

    private static async Task LoadSchemasAsync(
        NpgsqlConnection conn, PhysicalSchemaDiscoveryResult result, IProgress<DbDiscoveryProgress>? progress, CancellationToken ct)
    {
        const string sql = """
            SELECT DISTINCT table_schema
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            result.Schemas.Add(new PhysicalSchemaInfo { SchemaName = schema });
            progress?.Report(new DbDiscoveryProgress("SchemaDiscovered", 15, schema, Message: schema));
        }
    }

    private static async Task LoadTablesAndViewsAsync(
        NpgsqlConnection conn, PhysicalSchemaDiscoveryResult result, IProgress<DbDiscoveryProgress>? progress, CancellationToken ct)
    {
        const string sql = """
            SELECT table_schema, table_name, table_type
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var type = reader.GetString(2);
            var objectType = type.Contains("VIEW", StringComparison.OrdinalIgnoreCase)
                ? DbCatalogObjectTypes.View
                : DbCatalogObjectTypes.Table;
            result.Tables.Add(new PhysicalTableInfo
            {
                SchemaName = schema,
                ObjectName = name,
                ObjectType = objectType
            });
            progress?.Report(new DbDiscoveryProgress("TableDiscovered", 30, schema, name, ObjectType: objectType));
        }
    }

    private static async Task LoadColumnsAsync(
        NpgsqlConnection conn, PhysicalSchemaDiscoveryResult result, IProgress<DbDiscoveryProgress>? progress, CancellationToken ct)
    {
        const string sql = """
            SELECT table_schema, table_name, column_name, data_type, is_nullable, column_default, ordinal_position
            FROM information_schema.columns
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name, ordinal_position
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new NpgsqlCommand(sql, conn);
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
        progress?.Report(new DbDiscoveryProgress("ColumnsLoaded", 50, Message: $"{result.Columns.Count} columns"));
    }

    private static async Task LoadPrimaryKeysAsync(NpgsqlConnection conn, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT tc.table_schema, tc.table_name, kcu.column_name, tc.constraint_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
              ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
            WHERE tc.constraint_type = 'PRIMARY KEY'
              AND tc.table_schema NOT IN ('pg_catalog', 'information_schema')
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var column = reader.GetString(2);
            var constraint = reader.GetString(3);
            var col = result.Columns.FirstOrDefault(c =>
                c.SchemaName == schema && c.ObjectName == table && c.ColumnName == column);
            if (col != null) col.IsPrimaryKey = true;
            result.Constraints.Add(new PhysicalConstraintInfo
            {
                SchemaName = schema,
                ObjectName = table,
                ConstraintName = constraint,
                ConstraintType = "PRIMARY KEY",
                ColumnNames = [column]
            });
        }
    }

    private static async Task LoadForeignKeysAsync(NpgsqlConnection conn, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT
                kcu.table_schema AS from_schema,
                kcu.table_name AS from_table,
                kcu.column_name AS from_column,
                ccu.table_schema AS to_schema,
                ccu.table_name AS to_table,
                ccu.column_name AS to_column,
                tc.constraint_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
              ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage ccu
              ON ccu.constraint_name = tc.constraint_name AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND kcu.table_schema NOT IN ('pg_catalog', 'information_schema')
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var fromSchema = reader.GetString(0);
            var fromTable = reader.GetString(1);
            var fromColumn = reader.GetString(2);
            var toSchema = reader.GetString(3);
            var toTable = reader.GetString(4);
            var toColumn = reader.GetString(5);
            var constraint = reader.GetString(6);

            var col = result.Columns.FirstOrDefault(c =>
                c.SchemaName == fromSchema && c.ObjectName == fromTable && c.ColumnName == fromColumn);
            if (col != null) col.IsForeignKey = true;

            result.Relationships.Add(new PhysicalRelationshipInfo
            {
                FromSchema = fromSchema,
                FromTable = fromTable,
                FromColumn = fromColumn,
                ToSchema = toSchema,
                ToTable = toTable,
                ToColumn = toColumn,
                Source = DbRelationshipSource.ExplicitForeignKey,
                ConfidencePercent = 100
            });
            result.Constraints.Add(new PhysicalConstraintInfo
            {
                SchemaName = fromSchema,
                ObjectName = fromTable,
                ConstraintName = constraint,
                ConstraintType = "FOREIGN KEY",
                ColumnNames = [fromColumn]
            });
        }
    }

    private static async Task LoadIndexesAsync(NpgsqlConnection conn, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT schemaname, tablename, indexname, indexdef
            FROM pg_indexes
            WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var indexName = reader.GetString(2);
            var indexDef = reader.IsDBNull(3) ? "" : reader.GetString(3);
            result.Indexes.Add(new PhysicalIndexInfo
            {
                SchemaName = schema,
                ObjectName = table,
                IndexName = indexName,
                IsUnique = indexDef.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase),
                ColumnNames = ExtractColumnsFromIndexDef(indexDef)
            });
        }
    }

    private static async Task LoadRowCountsAsync(NpgsqlConnection conn, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT n.nspname AS schema_name, c.relname AS table_name,
                   GREATEST(c.reltuples::bigint, 0) AS estimated_rows
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind IN ('r', 'v')
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var rows = reader.GetInt64(2);
            var t = result.Tables.FirstOrDefault(x => x.SchemaName == schema && x.ObjectName == table);
            if (t != null) t.EstimatedRowCount = rows;
        }
    }

    private static void MarkIndexedColumns(PhysicalSchemaDiscoveryResult result)
    {
        foreach (var idx in result.Indexes)
        {
            foreach (var colName in idx.ColumnNames)
            {
                var col = result.Columns.FirstOrDefault(c =>
                    c.SchemaName == idx.SchemaName && c.ObjectName == idx.ObjectName &&
                    c.ColumnName.Equals(colName, StringComparison.OrdinalIgnoreCase));
                if (col != null) col.IsIndexed = true;
            }
        }
    }

    private static IReadOnlyList<string> ExtractColumnsFromIndexDef(string indexDef)
    {
        var start = indexDef.IndexOf('(');
        var end = indexDef.LastIndexOf(')');
        if (start < 0 || end <= start) return Array.Empty<string>();
        return indexDef[(start + 1)..end]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => c.Trim('"'))
            .ToList();
    }
}
