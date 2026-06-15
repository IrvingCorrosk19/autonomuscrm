using AutonomusCRM.Application.DatabaseIntelligence;
using Microsoft.Data.SqlClient;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Connectors;

internal sealed class SqlServerConnector : DbConnectorBase
{
    public override DbEngineType EngineType => DbEngineType.SqlServer;

    protected override async Task OpenAndPingAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
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
        if (readOnly)
            builder.ApplicationIntent = ApplicationIntent.ReadOnly;

        await using var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand("SELECT 1", conn) { CommandTimeout = timeoutSeconds };
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}
