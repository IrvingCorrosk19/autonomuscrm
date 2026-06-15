using AutonomusCRM.Application.DatabaseIntelligence;
using MySqlConnector;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Connectors;

internal sealed class MySqlConnectorImpl : DbConnectorBase
{
    public override DbEngineType EngineType => DbEngineType.MySQL;

    protected override async Task OpenAndPingAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
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

        await using var cmd = new MySqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}

internal sealed class MariaDbConnector : DbConnectorBase
{
    public override DbEngineType EngineType => DbEngineType.MariaDB;

    protected override async Task OpenAndPingAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
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

        await using var cmd = new MySqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}
