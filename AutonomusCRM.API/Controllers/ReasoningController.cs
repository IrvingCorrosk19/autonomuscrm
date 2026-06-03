using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.KnowledgeGraph;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/reasoning")]
[Authorize]
public class ReasoningController : ControllerBase
{
    private readonly IGraphReasoningEngine _reasoning;
    private readonly IGraphReasoningFoundation _foundation;
    private readonly ITenantContext _tenant;

    public ReasoningController(IGraphReasoningEngine reasoning, IGraphReasoningFoundation foundation, ITenantContext tenant)
    {
        _reasoning = reasoning;
        _foundation = foundation;
        _tenant = tenant;
    }

    [HttpGet("customer/{customerId:guid}/risk")]
    public async Task<IActionResult> ExplainRisk(Guid customerId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _reasoning.ExplainCustomerRiskAsync(tenantId, customerId, cancellationToken));
    }

    [HttpGet("customer/{customerId:guid}/renewal")]
    public async Task<IActionResult> ExplainRenewal(Guid customerId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _reasoning.ExplainCustomerRenewalAsync(tenantId, customerId, cancellationToken));
    }

    [HttpGet("decision/{auditId:guid}")]
    public async Task<IActionResult> ExplainDecision(Guid auditId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _reasoning.ExplainDecisionAsync(tenantId, auditId, cancellationToken));
    }

    [HttpGet("revenue/leak")]
    public async Task<IActionResult> DetectLeak(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _reasoning.DetectRevenueLeakAsync(tenantId, cancellationToken));
    }

    [HttpGet("foundation")]
    public async Task<IActionResult> Foundation([FromQuery] string scenario = "default", CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _foundation.PrepareReasoningContextAsync(tenantId, scenario, cancellationToken));
    }
}
