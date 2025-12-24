using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowRepository workflowRepository,
        ILogger<WorkflowsController> logger)
    {
        _workflowRepository = workflowRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Workflow>>> GetWorkflows([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var workflows = await _workflowRepository.GetActiveByTenantAsync(tenantId, cancellationToken);
        return Ok(workflows);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Workflow>> GetWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id, cancellationToken);
        if (workflow == null)
            return NotFound();

        return Ok(workflow);
    }
}

