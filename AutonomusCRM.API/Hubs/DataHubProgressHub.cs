using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AutonomusCRM.API.Hubs;

[Authorize(Roles = "Admin,Manager,Owner")]
public sealed class DataHubProgressHub : Hub
{
    private readonly IDataHubTenantGuard _tenantGuard;
    private readonly IDataHubRepository _repo;

    public DataHubProgressHub(IDataHubTenantGuard tenantGuard, IDataHubRepository repo)
    {
        _tenantGuard = tenantGuard;
        _repo = repo;
    }

    public static string JobGroup(Guid jobId) => $"job:{jobId}";
    public static string TenantGroup(Guid tenantId) => $"tenant:{tenantId}";

    public async Task SubscribeJob(Guid jobId, Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        var job = await _repo.GetJobAsync(tenantId, jobId, Context.ConnectionAborted);
        if (job == null)
            throw new HubException("Job not found or access denied.");

        if (!CanAccessJob(job))
            throw new HubException("Job access denied for current user.");

        await Groups.AddToGroupAsync(Context.ConnectionId, JobGroup(jobId));
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
    }

    private bool CanAccessJob(DataHubImportJob job)
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole("Admin") || user.IsInRole("Owner")) return true;
        var claim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var userId) && userId == job.CreatedByUserId;
    }

    public async Task SubscribeTenant(Guid tenantId)
    {
        if (!_tenantGuard.IsSameTenant(tenantId))
            throw new HubException("Cross-tenant access denied.");

        if (!IsAdminOrOwner())
            throw new HubException("Tenant-wide subscription requires Admin or Owner role.");

        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
    }

    private bool IsAdminOrOwner()
    {
        var user = Context.User;
        return user?.Identity?.IsAuthenticated == true &&
               (user.IsInRole("Admin") || user.IsInRole("Owner"));
    }
}

public sealed class DataHubProgressNotifier : IDataHubProgressNotifier
{
    private readonly IHubContext<DataHubProgressHub> _hub;

    public DataHubProgressNotifier(IHubContext<DataHubProgressHub> hub) => _hub = hub;

    public async Task NotifyProgressAsync(DataHubProgressUpdateDto update, CancellationToken cancellationToken = default)
    {
        await _hub.Clients.Group(DataHubProgressHub.JobGroup(update.JobId))
            .SendAsync("ProgressUpdate", update, cancellationToken);
        await _hub.Clients.Group(DataHubProgressHub.TenantGroup(update.TenantId))
            .SendAsync("ProgressUpdate", update, cancellationToken);
    }
}
