using AutonomusCRM.Application.DatabaseIntelligence;
using Oracle.ManagedDataAccess.Client;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

internal sealed class OracleSchemaIntrospector : IDbSchemaIntrospector
{
    public DbEngineType EngineType => DbEngineType.Oracle;

    public async Task<PhysicalSchemaDiscoveryResult> DiscoverAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var result = new PhysicalSchemaDiscoveryResult();
        var dataSource = $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={endpoint.Host})(PORT={endpoint.Port}))(CONNECT_DATA=(SERVICE_NAME={endpoint.DatabaseName})))";
        var builder = new OracleConnectionStringBuilder
        {
            DataSource = dataSource,
            UserID = endpoint.Username,
            Password = secrets.Password,
            ConnectionTimeout = timeoutSeconds,
            Pooling = false
        };

        await using var conn = new OracleConnection(builder.ConnectionString);
        await conn.OpenAsync(cancellationToken);
        progress?.Report(new DbDiscoveryProgress("Connecting", 5, Message: "Connected to Oracle"));

        await LoadTablesAsync(conn, endpoint.Username.ToUpperInvariant(), result, progress, cancellationToken);
        await LoadColumnsAsync(conn, endpoint.Username.ToUpperInvariant(), result, cancellationToken);
        await LoadConstraintsAsync(conn, endpoint.Username.ToUpperInvariant(), result, cancellationToken);
        DbRelationshipHeuristics.ApplyNamingHeuristics(result);
        progress?.Report(new DbDiscoveryProgress("Completed", 100));
        return result;
    }

    internal static PhysicalSchemaDiscoveryResult ApplyFixture(PhysicalSchemaDiscoveryResult fixture)
    {
        DbRelationshipHeuristics.ApplyNamingHeuristics(fixture);
        return fixture;
    }

    private static async Task LoadTablesAsync(OracleConnection conn, string owner, PhysicalSchemaDiscoveryResult result,
        IProgress<DbDiscoveryProgress>? progress, CancellationToken ct)
    {
        const string sql = """
            SELECT owner, table_name, 'TABLE' AS object_type, NVL(num_rows, 0) AS estimated_rows
            FROM all_tables WHERE owner = :owner
            UNION ALL
            SELECT owner, view_name, 'VIEW', 0 FROM all_views WHERE owner = :owner
            ORDER BY 2
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new OracleParameter("owner", owner));
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

    private static async Task LoadColumnsAsync(OracleConnection conn, string owner, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT owner, table_name, column_name, data_type, nullable, data_default, column_id
            FROM all_tab_columns
            WHERE owner = :owner
            ORDER BY table_name, column_id
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new OracleParameter("owner", owner));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Columns.Add(new PhysicalColumnInfo
            {
                SchemaName = reader.GetString(0),
                ObjectName = reader.GetString(1),
                ColumnName = reader.GetString(2),
                DataType = reader.GetString(3),
                IsNullable = reader.GetString(4).Equals("Y", StringComparison.OrdinalIgnoreCase),
                DefaultValue = reader.IsDBNull(5) ? null : reader.GetString(5),
                Ordinal = reader.GetInt32(6)
            });
        }
    }

    private static async Task LoadConstraintsAsync(OracleConnection conn, string owner, PhysicalSchemaDiscoveryResult result, CancellationToken ct)
    {
        const string sql = """
            SELECT c.owner, c.table_name, c.constraint_name, c.constraint_type,
                   cc.column_name, r.owner AS ref_owner, r.table_name AS ref_table, rc.column_name AS ref_column
            FROM all_constraints c
            LEFT JOIN all_cons_columns cc ON c.owner = cc.owner AND c.constraint_name = cc.constraint_name
            LEFT JOIN all_constraints r ON c.r_owner = r.owner AND c.r_constraint_name = r.constraint_name
            LEFT JOIN all_cons_columns rc ON r.owner = rc.owner AND r.constraint_name = rc.constraint_name AND cc.position = rc.position
            WHERE c.owner = :owner
            """;
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new OracleParameter("owner", owner));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var constraint = reader.GetString(2);
            var type = reader.GetString(3);
            var column = reader.IsDBNull(4) ? null : reader.GetString(4);
            if (column == null) continue;

            if (type == "P")
            {
                var col = result.Columns.FirstOrDefault(c => c.SchemaName == schema && c.ObjectName == table && c.ColumnName == column);
                if (col != null) col.IsPrimaryKey = true;
            }
            else if (type == "R" && !reader.IsDBNull(5))
            {
                var col = result.Columns.FirstOrDefault(c => c.SchemaName == schema && c.ObjectName == table && c.ColumnName == column);
                if (col != null) col.IsForeignKey = true;
                result.Relationships.Add(new PhysicalRelationshipInfo
                {
                    FromSchema = schema,
                    FromTable = table,
                    FromColumn = column,
                    ToSchema = reader.GetString(5),
                    ToTable = reader.GetString(6),
                    ToColumn = reader.IsDBNull(7) ? "ID" : reader.GetString(7),
                    Source = DbRelationshipSource.ExplicitForeignKey,
                    ConfidencePercent = 100
                });
            }

            result.Constraints.Add(new PhysicalConstraintInfo
            {
                SchemaName = schema,
                ObjectName = table,
                ConstraintName = constraint,
                ConstraintType = type,
                ColumnNames = [column]
            });
        }
    }
}
