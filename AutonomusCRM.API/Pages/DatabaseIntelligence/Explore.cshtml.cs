using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class ExploreModel : PageModel
{
    private readonly IDbConnectionProfileService _connections;
    private readonly IDbSchemaDiscoveryService _discovery;
    private readonly IServiceProvider _sp;

    public ExploreModel(IDbConnectionProfileService connections, IDbSchemaDiscoveryService discovery, IServiceProvider sp)
    {
        _connections = connections;
        _discovery = discovery;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ConnectionId { get; set; }

    public Guid SelectedConnectionId { get; private set; }
    public IReadOnlyList<DbConnectionProfileDto> Connections { get; private set; } = Array.Empty<DbConnectionProfileDto>();
    public DbCatalogSnapshotDto? Snapshot { get; private set; }
    public IReadOnlyList<DbCatalogTableDto> Tables { get; private set; } = Array.Empty<DbCatalogTableDto>();
    public IReadOnlyList<DbCatalogColumnDto> Columns { get; private set; } = Array.Empty<DbCatalogColumnDto>();
    public IReadOnlyList<DbCatalogRelationshipDto> Relationships { get; private set; } = Array.Empty<DbCatalogRelationshipDto>();
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetDiscoverAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            StatusMessage = "Only administrators can start discovery.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (ConnectionId == Guid.Empty) { await LoadAsync(cancellationToken); return Page(); }

        try
        {
            var result = await _discovery.DiscoverNowAsync(
                tenantId,
                Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty,
                ConnectionId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            StatusMessage = $"Discovery completed — {result.Snapshot.TableCount} tables and {result.Snapshot.ViewCount} views found.";
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

        Snapshot = await _discovery.GetLatestCatalogForConnectionAsync(tenantId, SelectedConnectionId, cancellationToken);
        Tables = await _discovery.ListCatalogTablesAsync(tenantId, SelectedConnectionId, cancellationToken);
        Columns = await _discovery.ListCatalogColumnsAsync(tenantId, SelectedConnectionId, cancellationToken);
        Relationships = await _discovery.ListCatalogRelationshipsAsync(tenantId, SelectedConnectionId, cancellationToken);
    }
}
