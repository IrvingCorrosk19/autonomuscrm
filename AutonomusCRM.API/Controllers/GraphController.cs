using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Common.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/graph")]
[Authorize]
public class GraphController : ControllerBase
{
    private readonly IKnowledgeGraphService _graph;
    private readonly IGraphReasoningFoundation _reasoning;
    private readonly ITenantContext _tenant;

    public GraphController(IKnowledgeGraphService graph, IGraphReasoningFoundation reasoning, ITenantContext tenant)
    {
        _graph = graph;
        _reasoning = reasoning;
        _tenant = tenant;
    }

    [HttpPost("build")]
    public async Task<IActionResult> BuildGraph(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var count = await _graph.BuildGraphAsync(tenantId, cancellationToken);
        return Ok(new { edgesCreated = count });
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetCustomerGraph(Guid customerId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _graph.GetCustomerGraphAsync(tenantId, customerId, cancellationToken));
    }

    [HttpGet("business")]
    public async Task<IActionResult> GetBusinessGraph([FromQuery] int maxNodes = 150, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _graph.GetBusinessGraphAsync(tenantId, maxNodes, cancellationToken));
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueGraph(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _graph.GetRevenueGraphAsync(tenantId, cancellationToken));
    }

    [HttpGet("decision/{auditId:guid}")]
    public async Task<IActionResult> GetDecisionGraph(Guid auditId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var result = await _graph.GetDecisionGraphAsync(tenantId, auditId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("outcome/{outcomeId:guid}")]
    public async Task<IActionResult> GetOutcomeGraph(Guid outcomeId, [FromQuery] bool fromMemory = true, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var result = await _graph.GetOutcomeGraphAsync(tenantId, outcomeId, fromMemory, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchGraph([FromQuery] string q, [FromQuery] int take = 40, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _graph.SearchGraphAsync(tenantId, q ?? "", take, cancellationToken));
    }

    [HttpGet("reasoning-foundation")]
    public async Task<IActionResult> GetReasoningFoundation([FromQuery] string scenario = "default", CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _reasoning.PrepareReasoningContextAsync(tenantId, scenario, cancellationToken));
    }
}
