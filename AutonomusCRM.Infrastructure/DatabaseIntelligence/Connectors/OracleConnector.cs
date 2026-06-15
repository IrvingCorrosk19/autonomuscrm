using AutonomusCRM.Application.DatabaseIntelligence;
using Oracle.ManagedDataAccess.Client;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Connectors;

internal sealed class OracleConnector : DbConnectorBase
{
    public override DbEngineType EngineType => DbEngineType.Oracle;
    public override bool SupportsReadOnlyMode => true;

    protected override async Task OpenAndPingAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
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
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = readOnly ? "SELECT 1 FROM DUAL" : "SELECT 1 FROM DUAL";
        cmd.CommandTimeout = timeoutSeconds;
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}
