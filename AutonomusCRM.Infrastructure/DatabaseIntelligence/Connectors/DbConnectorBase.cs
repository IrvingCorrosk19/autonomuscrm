using System.Diagnostics;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Connectors;

internal abstract class DbConnectorBase : IDbConnector
{
    public abstract DbEngineType EngineType { get; }
    public virtual bool SupportsReadOnlyMode => true;

    public async Task<DbConnectionTestResultDto> TestConnectionAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await OpenAndPingAsync(endpoint, secrets, readOnly, timeoutSeconds, cancellationToken);
            sw.Stop();
            return new DbConnectionTestResultDto(
                true,
                "Connection successful.",
                (int)sw.ElapsedMilliseconds,
                EngineType,
                readOnly);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DbConnectionTestResultDto(
                false,
                DbConnectionStringValidator.SanitizeErrorMessage(ex.Message),
                (int)sw.ElapsedMilliseconds,
                EngineType,
                readOnly);
        }
    }

    protected abstract Task OpenAndPingAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}
