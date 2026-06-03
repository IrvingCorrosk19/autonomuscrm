using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.KnowledgeGraph;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/simulation")]
[Authorize]
public class SimulationController : ControllerBase
{
    private readonly IBusinessSimulationEngine _simulation;
    private readonly ITenantContext _tenant;

    public SimulationController(IBusinessSimulationEngine simulation, ITenantContext tenant)
    {
        _simulation = simulation;
        _tenant = tenant;
    }

    [HttpGet("scenarios")]
    public IActionResult ListScenarios() => Ok(_simulation.GetAvailableScenarios());

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromQuery] string scenario, [FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _simulation.RunScenarioAsync(tenantId, scenario, customerId, cancellationToken));
    }
}
