using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence;

public sealed class DbIntelligenceNullProgressNotifier :
    IDbIntelligenceProgressNotifier,
    IDbIntelligenceBusinessProgressNotifier,
    IDbIntelligenceHealthProgressNotifier,
    IDbIntelligenceGraphProgressNotifier,
    IDbIntelligenceSyncProgressNotifier,
    IDbOperationProgressNotifier
{
    public Task NotifyDiscoveryStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifySchemaDiscoveredAsync(Guid tenantId, Guid jobId, string schemaName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyTableDiscoveredAsync(Guid tenantId, Guid jobId, string schemaName, string tableName, string objectType, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyDiscoveryProgressAsync(Guid tenantId, Guid jobId, DbDiscoveryProgress progress, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyDiscoveryCompletedAsync(Guid tenantId, Guid jobId, Guid snapshotId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyDiscoveryFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyBusinessDiscoveryStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyBusinessDiscoveryProgressAsync(Guid tenantId, Guid jobId, BusinessDiscoveryProgress progress, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyBusinessDiscoveryCompletedAsync(Guid tenantId, Guid jobId, int mappingsCount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyBusinessDiscoveryFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyHealthScanStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyHealthScanProgressAsync(Guid tenantId, Guid jobId, DataHealthProgress progress, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyHealthScanCompletedAsync(Guid tenantId, Guid jobId, int globalScore, int findingsCount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyHealthScanFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyGraphBuildStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyGraphBuildProgressAsync(Guid tenantId, Guid jobId, DbBusinessGraphProgress progress, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyGraphBuildCompletedAsync(Guid tenantId, Guid jobId, int nodeCount, int edgeCount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyGraphBuildFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifySyncStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifySyncProgressAsync(Guid tenantId, Guid jobId, DbSyncProgress progress, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifySyncCompletedAsync(Guid tenantId, Guid jobId, int imported, int errors, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifySyncFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOperationStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOperationProgressAsync(Guid tenantId, Guid jobId, DbOperationProgress progress, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOperationCompletedAsync(Guid tenantId, Guid jobId, DbOperationResultDto result, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOperationFailedAsync(Guid tenantId, Guid jobId, string error, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
