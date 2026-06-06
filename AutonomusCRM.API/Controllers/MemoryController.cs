using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Application.Common.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/memory")]
[Authorize]
public class MemoryController : ControllerBase
{
    private readonly ISemanticMemoryService _semantic;
    private readonly ITenantContext _tenant;

    public MemoryController(ISemanticMemoryService semantic, ITenantContext tenant)
    {
        _semantic = semantic;
        _tenant = tenant;
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _semantic.GetTimelineAsync(tenantId, take, cancellationToken));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _semantic.SearchAsync(tenantId, q ?? "", take, cancellationToken));
    }

    [HttpGet("context")]
    public async Task<IActionResult> GetContext([FromQuery] string q, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _semantic.GetBusinessContextAsync(tenantId, q ?? "", cancellationToken));
    }

    [HttpGet("similar")]
    public async Task<IActionResult> FindSimilar([FromQuery] string text, [FromQuery] int take = 15, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _semantic.FindSimilarMemoriesAsync(tenantId, text ?? "", take, cancellationToken));
    }

    [HttpGet("learnings")]
    public async Task<IActionResult> GetRelatedLearnings([FromQuery] string q, [FromQuery] int take = 10, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _semantic.GetRelatedLearningsAsync(tenantId, q ?? "", take, cancellationToken));
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _semantic.GetDashboardAsync(tenantId, cancellationToken));
    }

    [HttpGet("customers/{customerId:guid}/profile")]
    public async Task<IActionResult> GetCustomerProfile(Guid customerId, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _semantic.GetOrBuildCustomerProfileAsync(tenantId, customerId, cancellationToken));
    }

    [HttpPost("index")]
    public async Task<IActionResult> TriggerIndex([FromQuery] int takePerType = 50, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        await _semantic.IndexBusinessMemorySourcesAsync(tenantId, takePerType, cancellationToken);
        return Ok(new { indexed = true, takePerType });
    }
}
