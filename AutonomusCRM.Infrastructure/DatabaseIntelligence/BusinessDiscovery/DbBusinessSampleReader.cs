using System.Text.RegularExpressions;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;
using Npgsql;
using MySqlConnector;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;

public sealed class DbBusinessSampleReader
{
    private static readonly Regex SafeIdentifier = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadTopNAsync(
        DbConnectionProfile connection,
        DbConnectionSecrets secrets,
        string schemaName,
        string tableName,
        IReadOnlyList<string> columnNames,
        int limit,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(schemaName);
        ValidateIdentifier(tableName);
        var safeColumns = columnNames.Where(c => SafeIdentifier.IsMatch(c)).Take(12).ToList();
        if (safeColumns.Count == 0)
            safeColumns = ["*"];

        var sql = BuildQuery(connection.EngineType, schemaName, tableName, safeColumns, limit);
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery(sql);

        return connection.EngineType switch
        {
            DbEngineType.PostgreSQL => await ReadPostgresAsync(connection, secrets, sql, safeColumns, timeoutSeconds, cancellationToken),
            DbEngineType.SqlServer => await ReadSqlServerAsync(connection, secrets, sql, safeColumns, timeoutSeconds, cancellationToken),
            DbEngineType.MySQL or DbEngineType.MariaDB => await ReadMySqlAsync(connection, secrets, sql, safeColumns, timeoutSeconds, cancellationToken),
            DbEngineType.Oracle => await ReadOracleAsync(connection, secrets, sql, safeColumns, timeoutSeconds, cancellationToken),
            _ => Array.Empty<IReadOnlyDictionary<string, string?>>()
        };
    }

    private static string BuildQuery(
        DbEngineType engine,
        string schema,
        string table,
        IReadOnlyList<string> columns,
        int limit)
    {
        var columnList = columns.Count == 1 && columns[0] == "*"
            ? "*"
            : string.Join(", ", columns.Select(c => Quote(engine, c)));

        var qualified = engine switch
        {
            DbEngineType.PostgreSQL => $"{Quote(engine, schema)}.{Quote(engine, table)}",
            DbEngineType.SqlServer => $"{Quote(engine, schema)}.{Quote(engine, table)}",
            DbEngineType.MySQL or DbEngineType.MariaDB => $"{Quote(engine, schema)}.{Quote(engine, table)}",
            DbEngineType.Oracle => $"{Quote(engine, schema)}.{Quote(engine, table)}",
            _ => throw new NotSupportedException()
        };

        return engine switch
        {
            DbEngineType.SqlServer => $"SELECT TOP ({limit}) {columnList} FROM {qualified}",
            DbEngineType.Oracle => $"SELECT {columnList} FROM {qualified} FETCH FIRST {limit} ROWS ONLY",
            _ => $"SELECT {columnList} FROM {qualified} LIMIT {limit}"
        };
    }

    private static string Quote(DbEngineType engine, string identifier) => engine switch
    {
        DbEngineType.PostgreSQL => $"\"{identifier}\"",
        DbEngineType.SqlServer => $"[{identifier}]",
        DbEngineType.MySQL or DbEngineType.MariaDB => $"`{identifier}`",
        DbEngineType.Oracle => $"\"{identifier.ToUpperInvariant()}\"",
        _ => identifier
    };

    private static void ValidateIdentifier(string value)
    {
        if (!SafeIdentifier.IsMatch(value))
            throw new DbIntelligenceValidationException("Invalid schema or table identifier.");
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadPostgresAsync(
        DbConnectionProfile connection,
        DbConnectionSecrets secrets,
        string sql,
        IReadOnlyList<string> columns,
        int timeoutSeconds,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(BuildPostgresConnectionString(connection, secrets));
        conn.Open();
        await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = timeoutSeconds };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await ReadRowsAsync(reader, columns, ct);
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadSqlServerAsync(
        DbConnectionProfile connection,
        DbConnectionSecrets secrets,
        string sql,
        IReadOnlyList<string> columns,
        int timeoutSeconds,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(
            $"Server={connection.Host},{connection.Port};Database={connection.DatabaseName};User Id={connection.Username};Password={secrets.Password};TrustServerCertificate=True;");
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = timeoutSeconds };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await ReadRowsAsync(reader, columns, ct);
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadMySqlAsync(
        DbConnectionProfile connection,
        DbConnectionSecrets secrets,
        string sql,
        IReadOnlyList<string> columns,
        int timeoutSeconds,
        CancellationToken ct)
    {
        await using var conn = new MySqlConnection(
            $"Server={connection.Host};Port={connection.Port};Database={connection.DatabaseName};User ID={connection.Username};Password={secrets.Password};");
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = timeoutSeconds };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await ReadRowsAsync(reader, columns, ct);
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadOracleAsync(
        DbConnectionProfile connection,
        DbConnectionSecrets secrets,
        string sql,
        IReadOnlyList<string> columns,
        int timeoutSeconds,
        CancellationToken ct)
    {
        await using var conn = new OracleConnection(
            $"User Id={connection.Username};Password={secrets.Password};Data Source={connection.Host}:{connection.Port}/{connection.DatabaseName}");
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = timeoutSeconds;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await ReadRowsAsync(reader, columns, ct);
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadRowsAsync(
        System.Data.Common.DbDataReader reader,
        IReadOnlyList<string> columns,
        CancellationToken ct)
    {
        var rows = new List<IReadOnlyDictionary<string, string?>>();
        var fieldCount = reader.FieldCount;
        var names = Enumerable.Range(0, fieldCount).Select(reader.GetName).ToList();

        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                var ordinal = reader.GetOrdinal(name);
                row[name] = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString();
            }
            rows.Add(row);
        }

        return rows;
    }

    private static string BuildPostgresConnectionString(DbConnectionProfile connection, DbConnectionSecrets secrets) =>
        $"Host={connection.Host};Port={connection.Port};Database={connection.DatabaseName};Username={connection.Username};Password={secrets.Password};Timeout=15";
}
