using AutonomusCRM.Application.DatabaseIntelligence;
using Npgsql;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Connectors;

internal sealed class PostgreSqlConnector : DbConnectorBase
{
    public override DbEngineType EngineType => DbEngineType.PostgreSQL;

    protected override async Task OpenAndPingAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
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
        await using var cmd = new NpgsqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}
