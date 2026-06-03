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
    private readonly IServiceProvider _sp;

    public TrustInboxModel(
        IAiTrustService trust,
        ITenantTrustPolicyService policy,
        ITrustMetricsService metrics,
        IServiceProvider sp)
    {
        _trust = trust;
        _policy = policy;
        _metrics = metrics;
        _sp = sp;
    }

    public IReadOnlyList<ApprovalInboxItemDto> Items { get; set; } = Array.Empty<ApprovalInboxItemDto>();
    public TrustMetricsDto? Metrics { get; set; }
    public int ApprovalThreshold { get; set; } = 70;
    public string? Message { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        Items = await _trust.GetInboxAsync(tenantId);
        Metrics = await _metrics.GetMetricsAsync(tenantId);
        ApprovalThreshold = await _policy.GetApprovalThresholdAsync(tenantId);
    }

    public async Task<IActionResult> OnPostSetThresholdAsync(int threshold)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        await _policy.SetApprovalThresholdAsync(tenantId, threshold);
        Message = $"Umbral de aprobación actualizado a {Math.Clamp(threshold, 50, 95)}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid approvalId)
    {
        try
        {
            var tenantId = await this.GetTenantIdForPageAsync(_sp);
            await _trust.ApproveAsync(tenantId, approvalId, GetUserId(), executeDecision: true);
            Message = "Decisión aprobada y ejecutada.";
        }
        catch (Exception ex) { Error = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid approvalId, string? note)
    {
        try
        {
            var tenantId = await this.GetTenantIdForPageAsync(_sp);
            await _trust.RejectAsync(tenantId, approvalId, GetUserId(), note);
            Message = "Decisión rechazada.";
        }
        catch (Exception ex) { Error = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRollbackAsync(Guid approvalId, string note)
    {
        try
        {
            var tenantId = await this.GetTenantIdForPageAsync(_sp);
            await _trust.RollbackAsync(tenantId, approvalId, GetUserId(), note);
            Message = "Decisión revertida (rollback registrado).";
        }
        catch (Exception ex) { Error = ex.Message; }
        return RedirectToPage();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
