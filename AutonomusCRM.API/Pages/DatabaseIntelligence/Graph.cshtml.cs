using System.Text.Json;
using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class GraphModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IDbConnectionProfileService _connections;
    private readonly IDbBusinessGraphService _graph;
    private readonly IServiceProvider _sp;

    public GraphModel(IDbConnectionProfileService connections, IDbBusinessGraphService graph, IServiceProvider sp)
    {
        _connections = connections;
        _graph = graph;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ConnectionId { get; set; }

    public Guid SelectedConnectionId { get; private set; }
    public IReadOnlyList<DbConnectionProfileDto> Connections { get; private set; } = Array.Empty<DbConnectionProfileDto>();
    public DbBusinessGraphDto? Graph { get; private set; }
    public string? GraphJson { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetBuildAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            StatusMessage = "Only administrators can build the business graph.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (ConnectionId == Guid.Empty) { await LoadAsync(cancellationToken); return Page(); }

        try
        {
            var result = await _graph.BuildGraphAsync(
                tenantId,
                Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty,
                ConnectionId,
                new BuildDbBusinessGraphRequest(true, true),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            Graph = result.Graph;
            GraphJson = JsonSerializer.Serialize(Graph, JsonOptions);
            StatusMessage = $"Business graph built — {result.Graph.Nodes.Count} areas, {result.Graph.Edges.Count} relationships.";
            Connections = await _connections.ListAsync(tenantId, cancellationToken);
            SelectedConnectionId = ConnectionId;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            await LoadAsync(cancellationToken);
        }

        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Connections = await _connections.ListAsync(tenantId, cancellationToken);
        SelectedConnectionId = ConnectionId != Guid.Empty ? ConnectionId : Connections.FirstOrDefault()?.Id ?? Guid.Empty;
        if (SelectedConnectionId == Guid.Empty) return;
        Graph = await _graph.GetGraphAsync(tenantId, SelectedConnectionId, cancellationToken);
        if (Graph != null)
            GraphJson = JsonSerializer.Serialize(Graph, JsonOptions);
    }
}
