using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/business-memory")]
[Authorize]
public class BusinessMemoryController : ControllerBase
{
    private readonly IBusinessMemoryService _memory;
    private readonly ITenantContext _tenant;

    public BusinessMemoryController(IBusinessMemoryService memory, ITenantContext tenant)
    {
        _memory = memory;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetBusinessMemory([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _memory.GetBusinessMemoryAsync(tenantId, take, cancellationToken));
    }

    [HttpGet("customers/{customerId:guid}")]
    public async Task<IActionResult> GetCustomerMemory(Guid customerId, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _memory.GetCustomerMemoryAsync(tenantId, customerId, cancellationToken));
    }

    [HttpGet("decisions/{auditId:guid}")]
    public async Task<IActionResult> GetDecisionMemory(Guid auditId, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        var decision = await _memory.GetDecisionMemoryAsync(tenantId, auditId, cancellationToken);
        return decision is null ? NotFound() : Ok(decision);
    }

    [HttpGet("{memoryId:guid}/outcomes")]
    public async Task<IActionResult> GetOutcomeMemory(Guid memoryId, CancellationToken cancellationToken = default)
    {
        return Ok(await _memory.GetOutcomeMemoryAsync(memoryId, cancellationToken));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchMemory([FromQuery] string q, [FromQuery] int take = 30, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _memory.SearchMemoryAsync(tenantId, q ?? "", take, cancellationToken));
    }

    [HttpGet("learnings")]
    public async Task<IActionResult> GetLearningHistory([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantGuard.Require(_tenant);
        return Ok(await _memory.GetLearningHistoryAsync(tenantId, take, cancellationToken));
    }
}
