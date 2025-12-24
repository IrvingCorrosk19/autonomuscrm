using AutonomusCRM.Application.Tenants.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly IRequestHandler<CreateTenantCommand, Guid> _createTenantHandler;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        IRequestHandler<CreateTenantCommand, Guid> createTenantHandler,
        ILogger<TenantsController> logger)
    {
        _createTenantHandler = createTenantHandler;
        _logger = logger;
    }

    [HttpPost]
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
    public ActionResult GetTenant(Guid id)
    {
        // TODO: Implementar GetTenantQuery
        return Ok(new { id });
    }
}

