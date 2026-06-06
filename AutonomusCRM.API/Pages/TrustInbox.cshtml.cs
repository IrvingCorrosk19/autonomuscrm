using AutonomusCRM.API.Resources;
using AutonomusCRM.Application.Autonomous;
using Microsoft.Extensions.Localization;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AutonomusCRM.API.Pages;

public class TrustInboxModel : PageModel
{
    private readonly IAiTrustService _trust;
    private readonly ITenantTrustPolicyService _policy;
    private readonly ITrustMetricsService _metrics;
    private readonly ITrustSlaService _sla;
    private readonly IOutcomeFabricService _outcomeFabric;
    private readonly IDecisionIntelligenceEngine _decisionIntel;
    private readonly IServiceProvider _sp;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public TrustInboxModel(
        IAiTrustService trust,
        ITenantTrustPolicyService policy,
        ITrustMetricsService metrics,
        ITrustSlaService sla,
        IOutcomeFabricService outcomeFabric,
        IDecisionIntelligenceEngine decisionIntel,
        IServiceProvider sp,
        IStringLocalizer<SharedResource> localizer)
    {
        _trust = trust;
        _policy = policy;
        _metrics = metrics;
        _sla = sla;
        _outcomeFabric = outcomeFabric;
        _decisionIntel = decisionIntel;
        _sp = sp;
        _localizer = localizer;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Preview { get; set; }

    public IReadOnlyList<TrustQueueItem> Queue { get; set; } = Array.Empty<TrustQueueItem>();
    public ApprovalInboxItemDto? Selected { get; set; }
    public OutcomeFabricStatusDto? SelectedOutcome { get; set; }
    public DecisionIntelligenceResultDto? TrustExplainability { get; set; }
    public TrustMetricsDto? Metrics { get; set; }
    public int ApprovalThreshold { get; set; } = 70;
    public bool SimulateMode { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }

    public record TrustQueueItem(ApprovalInboxItemDto Item, string Severity, int SortOrder);

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        var items = await _trust.GetInboxAsync(tenantId);
        var slaAlerts = await _sla.GetOverdueApprovalsAsync(tenantId);
        var slaByApproval = slaAlerts.ToDictionary(a => a.ApprovalId, a => a.Severity);

        Queue = items.Select(i =>
        {
            var sev = MapSeverity(i, slaByApproval.GetValueOrDefault(i.Id));
            return new TrustQueueItem(i, sev.Label, sev.Order);
        }).OrderBy(q => q.SortOrder).ThenByDescending(q => q.Item.CreatedAt).ToList();

        Metrics = await _metrics.GetMetricsAsync(tenantId);
        ApprovalThreshold = await _policy.GetApprovalThresholdAsync(tenantId);
        SimulateMode = string.Equals(Preview, "simulate", StringComparison.OrdinalIgnoreCase);

        var selectedId = Id ?? Queue.FirstOrDefault()?.Item.Id;
        if (selectedId is Guid sid)
        {
            Selected = Queue.FirstOrDefault(q => q.Item.Id == sid)?.Item ?? items.FirstOrDefault(i => i.Id == sid);
            if (Selected != null)
            {
                SelectedOutcome = await _outcomeFabric.GetStatusAsync(Selected.AuditId);
                try
                {
                    TrustExplainability = await _decisionIntel.ExplainTrustApprovalAsync(tenantId, Selected.Id);
                }
                catch
                {
                    TrustExplainability = null;
                }
            }
        }
    }

    private static (string Label, int Order) MapSeverity(ApprovalInboxItemDto item, string? slaSeverity)
    {
        if (slaSeverity == "critical") return ("critical", 0);
        if (item.RiskLevel == "Alto") return ("high", 1);
        if (slaSeverity == "warning") return ("high", 1);
        if (item.RiskLevel == "Medio") return ("medium", 2);
        return ("low", 3);
    }

    public async Task<IActionResult> OnPostSetThresholdAsync(int threshold)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        await _policy.SetApprovalThresholdAsync(tenantId, threshold);
        Message = _localizer["Trust_ThresholdUpdated", Math.Clamp(threshold, 50, 95)];
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid approvalId)
    {
        try
        {
            var tenantId = await this.GetTenantIdForPageAsync(_sp);
            await _trust.ApproveAsync(tenantId, approvalId, GetUserId(), executeDecision: true);
            Message = _localizer["Trust_ApprovedMessage"];
        }
        catch (Exception ex) { Error = ApiLocalization.Message(_localizer, ex.Message); }
        return RedirectToPage(new { id = approvalId });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid approvalId, string? note)
    {
        try
        {
            var tenantId = await this.GetTenantIdForPageAsync(_sp);
            await _trust.RejectAsync(tenantId, approvalId, GetUserId(), note);
            Message = _localizer["Trust_RejectedMessage"];
        }
        catch (Exception ex) { Error = ApiLocalization.Message(_localizer, ex.Message); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRollbackAsync(Guid approvalId, string note)
    {
        try
        {
            var tenantId = await this.GetTenantIdForPageAsync(_sp);
            await _trust.RollbackAsync(tenantId, approvalId, GetUserId(), note);
            Message = _localizer["Trust_RollbackMessage"];
        }
        catch (Exception ex) { Error = ApiLocalization.Message(_localizer, ex.Message); }
        return RedirectToPage();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
