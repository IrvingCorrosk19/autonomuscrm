using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class InsightsModel : PageModel
{
    private readonly IDbConnectionProfileService _connections;
    private readonly IDbIntelligenceInsightService _insights;
    private readonly IServiceProvider _sp;

    public InsightsModel(
        IDbConnectionProfileService connections,
        IDbIntelligenceInsightService insights,
        IServiceProvider sp)
    {
        _connections = connections;
        _insights = insights;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ConnectionId { get; set; }

    public Guid SelectedConnectionId { get; private set; }
    public IReadOnlyList<DbConnectionProfileDto> Connections { get; private set; } = Array.Empty<DbConnectionProfileDto>();
    public IReadOnlyList<DbIntelligenceInsightDto> Insights { get; private set; } = Array.Empty<DbIntelligenceInsightDto>();
    public DbIntelligenceInsightJobDto? Job { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetGenerateAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner") && !User.IsInRole("Manager"))
        {
            StatusMessage = "You do not have permission to generate insights.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (ConnectionId == Guid.Empty) { await LoadAsync(cancellationToken); return Page(); }

        try
        {
            var result = await _insights.GenerateInsightsAsync(
                tenantId,
                Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty,
                ConnectionId,
                new GenerateDbIntelligenceInsightsRequest(true),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            Insights = result.Insights;
            Job = result.Job;
            StatusMessage = $"Generated {result.Insights.Count} prioritized insights.";
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

    public string CategoryBadgeClass(string category) => category switch
    {
        DbIntelligenceInsightCategory.Risk => "danger",
        DbIntelligenceInsightCategory.Opportunity => "success",
        _ => "secondary"
    };

    public string TypeLabel(string type) => type switch
    {
        DbIntelligenceInsightType.CriticalTable => "Critical table",
        DbIntelligenceInsightType.UnusedData => "Unused data",
        DbIntelligenceInsightType.MigrationOpportunity => "Migration opportunity",
        DbIntelligenceInsightType.QualityRisk => "Quality risk",
        DbIntelligenceInsightType.UnmappedEntity => "Unmapped entity",
        _ => type
    };

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Connections = await _connections.ListAsync(tenantId, cancellationToken);
        SelectedConnectionId = ConnectionId != Guid.Empty ? ConnectionId : Connections.FirstOrDefault()?.Id ?? Guid.Empty;
        if (SelectedConnectionId == Guid.Empty) return;

        var latest = await _insights.GetLatestInsightsAsync(tenantId, SelectedConnectionId, cancellationToken);
        if (latest != null)
        {
            Insights = latest.Insights;
            Job = latest.Job;
        }
    }
}
