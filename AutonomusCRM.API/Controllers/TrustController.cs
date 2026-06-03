using System.Security.Claims;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.Trust;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/trust")]
public class TrustController : ControllerBase
{
    private readonly IAiTrustService _trust;
    private readonly ITrustMetricsService _metrics;
    private readonly ITenantTrustPolicyService _policy;
    private readonly ITrustSlaService _sla;
    private readonly ITenantContext _tenant;

    public TrustController(
        IAiTrustService trust,
        ITrustMetricsService metrics,
        ITenantTrustPolicyService policy,
        ITrustSlaService sla,
        ITenantContext tenant)
    {
        _trust = trust;
        _metrics = metrics;
        _policy = policy;
        _sla = sla;
        _tenant = tenant;
    }

    [HttpGet("sla/alerts")]
    public async Task<IActionResult> SlaAlerts([FromQuery] int hours = 24, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _sla.GetOverdueApprovalsAsync(tenantId, hours, cancellationToken));
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _metrics.GetMetricsAsync(tenantId, cancellationToken));
    }

    [HttpGet("policy")]
    public async Task<IActionResult> GetPolicy(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(new { approvalThreshold = await _policy.GetApprovalThresholdAsync(tenantId, cancellationToken) });
    }

    [HttpPut("policy")]
    public async Task<IActionResult> SetPolicy([FromBody] TrustPolicyUpdateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        await _policy.SetApprovalThresholdAsync(tenantId, request.ApprovalThreshold, cancellationToken);
        return NoContent();
    }

    [HttpGet("inbox")]
    public async Task<IActionResult> Inbox(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _trust.GetInboxAsync(tenantId, cancellationToken));
    }

    [HttpPost("inbox/{approvalId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid approvalId, [FromQuery] bool execute = true, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        await _trust.ApproveAsync(tenantId, approvalId, GetUserId(), execute, cancellationToken);
        return NoContent();
    }

    [HttpPost("inbox/{approvalId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid approvalId, [FromBody] string? note, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        await _trust.RejectAsync(tenantId, approvalId, GetUserId(), note, cancellationToken);
        return NoContent();
    }

    [HttpPost("inbox/{approvalId:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid approvalId, [FromBody] string note, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        await _trust.RollbackAsync(tenantId, approvalId, GetUserId(), note, cancellationToken);
        return NoContent();
    }

    public record TrustPolicyUpdateRequest(int ApprovalThreshold);

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
