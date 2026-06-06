using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.KnowledgeGraph;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/decision-intelligence")]
[Authorize]
public class DecisionIntelligenceController : ControllerBase
{
    private readonly IDecisionIntelligenceEngine _engine;
    private readonly ITenantContext _tenant;

    public DecisionIntelligenceController(IDecisionIntelligenceEngine engine, ITenantContext tenant)
    {
        _engine = engine;
        _tenant = tenant;
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> AnalyzeCustomer(Guid customerId, [FromQuery] string? agent, CancellationToken cancellationToken)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _engine.AnalyzeCustomerDecisionAsync(tenantId, customerId, agent, cancellationToken));
    }

    [HttpGet("audit/{auditId:guid}")]
    public async Task<IActionResult> AnalyzeAudit(Guid auditId, CancellationToken cancellationToken)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _engine.AnalyzeAuditDecisionAsync(tenantId, auditId, cancellationToken));
    }

    [HttpGet("trust/{approvalId:guid}")]
    public async Task<IActionResult> ExplainTrust(Guid approvalId, CancellationToken cancellationToken)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _engine.ExplainTrustApprovalAsync(tenantId, approvalId, cancellationToken));
    }
}
