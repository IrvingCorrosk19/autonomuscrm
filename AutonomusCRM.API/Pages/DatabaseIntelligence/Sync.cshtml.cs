using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class SyncModel : PageModel
{
    private readonly IDbConnectionProfileService _connections;
    private readonly IDbSyncOrchestrator _sync;
    private readonly IDbSyncScheduleService _schedules;
    private readonly IServiceProvider _sp;

    public SyncModel(
        IDbConnectionProfileService connections,
        IDbSyncOrchestrator sync,
        IDbSyncScheduleService schedules,
        IServiceProvider sp)
    {
        _connections = connections;
        _sync = sync;
        _schedules = schedules;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ConnectionId { get; set; }

    public Guid SelectedConnectionId { get; private set; }
    public IReadOnlyList<DbConnectionProfileDto> Connections { get; private set; } = Array.Empty<DbConnectionProfileDto>();
    public IReadOnlyList<DbSyncHistoryItemDto> History { get; private set; } = Array.Empty<DbSyncHistoryItemDto>();
    public IReadOnlyList<DbSyncScheduleDto> Schedules { get; private set; } = Array.Empty<DbSyncScheduleDto>();
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetFullSyncAsync(CancellationToken cancellationToken) =>
        await RunSyncAsync(DbSyncMode.Full, cancellationToken);

    public async Task<IActionResult> OnGetDeltaSyncAsync(CancellationToken cancellationToken) =>
        await RunSyncAsync(DbSyncMode.Delta, cancellationToken);

    private async Task<IActionResult> RunSyncAsync(string mode, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            StatusMessage = "Only administrators can start a sync.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (ConnectionId == Guid.Empty) { await LoadAsync(cancellationToken); return Page(); }

        try
        {
            var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
            var job = mode == DbSyncMode.Delta
                ? await _sync.StartDeltaSyncAsync(tenantId, userId, ConnectionId, DbSyncConflictPolicy.SourceWins, null, null, cancellationToken)
                : await _sync.StartFullSyncAsync(tenantId, userId, ConnectionId, DbSyncConflictPolicy.SourceWins, null, null, cancellationToken);
            StatusMessage = $"{mode} sync started — job {job.Id} ({job.Status}).";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Connections = await _connections.ListAsync(tenantId, cancellationToken);
        SelectedConnectionId = ConnectionId != Guid.Empty ? ConnectionId : Connections.FirstOrDefault()?.Id ?? Guid.Empty;
        if (SelectedConnectionId == Guid.Empty) return;
        History = await _sync.GetHistoryAsync(tenantId, SelectedConnectionId, 20, cancellationToken);
        Schedules = await _schedules.ListSchedulesAsync(tenantId, SelectedConnectionId, cancellationToken);
    }
}
