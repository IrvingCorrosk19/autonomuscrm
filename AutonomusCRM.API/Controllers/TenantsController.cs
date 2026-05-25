using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Tenants.Commands;
using AutonomusCRM.Application.Tenants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IRequestHandler<CreateTenantCommand, Guid> _createTenantHandler;
    private readonly IRequestHandler<GetTenantQuery, TenantDto?> _getTenantHandler;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        IRequestHandler<CreateTenantCommand, Guid> createTenantHandler,
        IRequestHandler<GetTenantQuery, TenantDto?> getTenantHandler,
        ILogger<TenantsController> logger)
    {
        _createTenantHandler = createTenantHandler;
        _getTenantHandler = getTenantHandler;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    public async Task<ActionResult<Guid>> CreateTenant([FromBody] CreateTenantCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = await _createTenantHandler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetTenant), new { id = tenantId }, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDto>> GetTenant(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _getTenantHandler.HandleAsync(new GetTenantQuery(id), cancellationToken);
        if (tenant is null)
            return NotFound();
        return Ok(tenant);
    }
}
