using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AutonomusCRM.API.Hubs;

[Authorize(Roles = "Admin,Manager,Owner")]
public sealed class DbIntelligenceProgressHub : Hub
{
    private readonly IDbIntelligenceTenantGuard _tenantGuard;
    private readonly ApplicationDbContext _db;

    public DbIntelligenceProgressHub(IDbIntelligenceTenantGuard tenantGuard, ApplicationDbContext db)
    {
        _tenantGuard = tenantGuard;
        _db = db;
    }

    public static string JobGroup(Guid jobId) => $"dbi-job:{jobId}";
    public static string TenantGroup(Guid tenantId) => $"dbi-tenant:{tenantId}";

    public async Task SubscribeDiscoveryJob(Guid jobId, Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        var job = await _db.DbDiscoveryJobs.FindAsync([jobId], Context.ConnectionAborted);
        if (job == null || job.TenantId != tenantId)
            throw new HubException("Discovery job not found or access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, JobGroup(jobId));
    }

    public async Task SubscribeBusinessDiscoveryJob(Guid jobId, Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        var job = await _db.DbBusinessDiscoveryJobs.FindAsync([jobId], Context.ConnectionAborted);
        if (job == null || job.TenantId != tenantId)
            throw new HubException("Business discovery job not found or access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, BusinessJobGroup(jobId));
    }

    public static string BusinessJobGroup(Guid jobId) => $"dbi-business-job:{jobId}";

    public async Task SubscribeHealthScanJob(Guid jobId, Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        var job = await _db.DataHealthJobs.FindAsync([jobId], Context.ConnectionAborted);
        if (job == null || job.TenantId != tenantId)
            throw new HubException("Health scan job not found or access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, HealthJobGroup(jobId));
    }

    public static string HealthJobGroup(Guid jobId) => $"dbi-health-job:{jobId}";

    public async Task SubscribeGraphBuildJob(Guid jobId, Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        var job = await _db.DbBusinessGraphJobs.FindAsync([jobId], Context.ConnectionAborted);
        if (job == null || job.TenantId != tenantId)
            throw new HubException("Graph build job not found or access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GraphJobGroup(jobId));
    }

    public static string GraphJobGroup(Guid jobId) => $"dbi-graph-job:{jobId}";

    public async Task SubscribeSyncJob(Guid jobId, Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        var job = await _db.DbSyncJobs.FindAsync([jobId], Context.ConnectionAborted);
        if (job == null || job.TenantId != tenantId)
            throw new HubException("Sync job not found or access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, SyncJobGroup(jobId));
    }

    public static string SyncJobGroup(Guid jobId) => $"dbi-sync-job:{jobId}";

    public async Task SubscribeOperationJob(Guid jobId, Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        var job = await _db.DbOperationJobs.FindAsync([jobId], Context.ConnectionAborted);
        if (job == null || job.TenantId != tenantId)
            throw new HubException("Operation job not found or access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, OperationJobGroup(jobId));
    }

    public static string OperationJobGroup(Guid jobId) => $"dbi-operation-job:{jobId}";

    public async Task SubscribeTenant(Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");
        if (!_tenantGuard.IsAdminOrOwner())
            throw new HubException("Tenant-wide subscription requires Admin or Owner role.");
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
    }
}

public sealed class DbIntelligenceProgressNotifier : IDbIntelligenceProgressNotifier, IDbIntelligenceBusinessProgressNotifier, IDbIntelligenceHealthProgressNotifier, IDbIntelligenceGraphProgressNotifier, IDbIntelligenceSyncProgressNotifier, IDbOperationProgressNotifier
{
    private readonly IHubContext<DbIntelligenceProgressHub> _hub;

    public DbIntelligenceProgressNotifier(IHubContext<DbIntelligenceProgressHub> hub) => _hub = hub;

    public Task NotifyDiscoveryStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.JobGroup(jobId))
            .SendAsync("DiscoveryStarted", new { tenantId, jobId, connectionId }, cancellationToken);

    public Task NotifySchemaDiscoveredAsync(Guid tenantId, Guid jobId, string schemaName, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.JobGroup(jobId))
            .SendAsync("SchemaDiscovered", new { tenantId, jobId, schemaName }, cancellationToken);

    public Task NotifyTableDiscoveredAsync(Guid tenantId, Guid jobId, string schemaName, string tableName, string objectType, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.JobGroup(jobId))
            .SendAsync("TableDiscovered", new { tenantId, jobId, schemaName, tableName, objectType }, cancellationToken);

    public Task NotifyDiscoveryProgressAsync(Guid tenantId, Guid jobId, DbDiscoveryProgress progress, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.JobGroup(jobId))
            .SendAsync("DiscoveryProgress", new { tenantId, jobId, progress }, cancellationToken);

    public Task NotifyDiscoveryCompletedAsync(Guid tenantId, Guid jobId, Guid snapshotId, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.JobGroup(jobId))
            .SendAsync("DiscoveryCompleted", new { tenantId, jobId, snapshotId }, cancellationToken);

    public Task NotifyDiscoveryFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.JobGroup(jobId))
            .SendAsync("DiscoveryFailed", new { tenantId, jobId, message = safeMessage }, cancellationToken);

    public Task NotifyBusinessDiscoveryStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.BusinessJobGroup(jobId))
            .SendAsync("BusinessDiscoveryStarted", new { tenantId, jobId, connectionId }, cancellationToken);

    public Task NotifyBusinessDiscoveryProgressAsync(Guid tenantId, Guid jobId, BusinessDiscoveryProgress progress, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.BusinessJobGroup(jobId))
            .SendAsync("BusinessDiscoveryProgress", new { tenantId, jobId, progress }, cancellationToken);

    public Task NotifyBusinessDiscoveryCompletedAsync(Guid tenantId, Guid jobId, int mappingsCount, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.BusinessJobGroup(jobId))
            .SendAsync("BusinessDiscoveryCompleted", new { tenantId, jobId, mappingsCount }, cancellationToken);

    public Task NotifyBusinessDiscoveryFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.BusinessJobGroup(jobId))
            .SendAsync("BusinessDiscoveryFailed", new { tenantId, jobId, message = safeMessage }, cancellationToken);

    public Task NotifyHealthScanStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.HealthJobGroup(jobId))
            .SendAsync("HealthScanStarted", new { tenantId, jobId, connectionId }, cancellationToken);

    public Task NotifyHealthScanProgressAsync(Guid tenantId, Guid jobId, DataHealthProgress progress, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.HealthJobGroup(jobId))
            .SendAsync("HealthScanProgress", new { tenantId, jobId, progress }, cancellationToken);

    public Task NotifyHealthScanCompletedAsync(Guid tenantId, Guid jobId, int globalScore, int findingsCount, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.HealthJobGroup(jobId))
            .SendAsync("HealthScanCompleted", new { tenantId, jobId, globalScore, findingsCount }, cancellationToken);

    public Task NotifyHealthScanFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.HealthJobGroup(jobId))
            .SendAsync("HealthScanFailed", new { tenantId, jobId, message = safeMessage }, cancellationToken);

    public Task NotifyGraphBuildStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.GraphJobGroup(jobId))
            .SendAsync("GraphBuildStarted", new { tenantId, jobId, connectionId }, cancellationToken);

    public Task NotifyGraphBuildProgressAsync(Guid tenantId, Guid jobId, DbBusinessGraphProgress progress, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.GraphJobGroup(jobId))
            .SendAsync("GraphBuildProgress", new { tenantId, jobId, progress }, cancellationToken);

    public Task NotifyGraphBuildCompletedAsync(Guid tenantId, Guid jobId, int nodeCount, int edgeCount, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.GraphJobGroup(jobId))
            .SendAsync("GraphBuildCompleted", new { tenantId, jobId, nodeCount, edgeCount }, cancellationToken);

    public Task NotifyGraphBuildFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.GraphJobGroup(jobId))
            .SendAsync("GraphBuildFailed", new { tenantId, jobId, message = safeMessage }, cancellationToken);

    public Task NotifySyncStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.SyncJobGroup(jobId))
            .SendAsync("SyncStarted", new { tenantId, jobId, connectionId }, cancellationToken);

    public Task NotifySyncProgressAsync(Guid tenantId, Guid jobId, DbSyncProgress progress, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.SyncJobGroup(jobId))
            .SendAsync("SyncProgress", new { tenantId, jobId, progress }, cancellationToken);

    public Task NotifySyncCompletedAsync(Guid tenantId, Guid jobId, int imported, int errors, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.SyncJobGroup(jobId))
            .SendAsync("SyncCompleted", new { tenantId, jobId, imported, errors }, cancellationToken);

    public Task NotifySyncFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.SyncJobGroup(jobId))
            .SendAsync("SyncFailed", new { tenantId, jobId, message = safeMessage }, cancellationToken);

    public Task NotifyOperationStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.OperationJobGroup(jobId))
            .SendAsync("OperationStarted", new { tenantId, jobId, connectionId }, cancellationToken);

    public Task NotifyOperationProgressAsync(Guid tenantId, Guid jobId, DbOperationProgress progress, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.OperationJobGroup(jobId))
            .SendAsync("OperationProgress", new { tenantId, jobId, progress }, cancellationToken);

    public Task NotifyOperationCompletedAsync(Guid tenantId, Guid jobId, DbOperationResultDto result, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.OperationJobGroup(jobId))
            .SendAsync("OperationCompleted", new { tenantId, jobId, result }, cancellationToken);

    public Task NotifyOperationFailedAsync(Guid tenantId, Guid jobId, string error, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(DbIntelligenceProgressHub.OperationJobGroup(jobId))
            .SendAsync("OperationFailed", new { tenantId, jobId, message = error }, cancellationToken);
}
