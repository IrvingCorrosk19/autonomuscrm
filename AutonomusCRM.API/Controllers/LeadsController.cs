using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Application.Leads.Queries;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly IRequestHandler<CreateLeadCommand, Guid> _createHandler;
    private readonly IRequestHandler<QualifyLeadCommand, bool> _qualifyHandler;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(
        IRequestHandler<CreateLeadCommand, Guid> createHandler,
        IRequestHandler<QualifyLeadCommand, bool> qualifyHandler,
        ILogger<LeadsController> logger)
    {
        _createHandler = createHandler;
        _qualifyHandler = qualifyHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateLead([FromBody] CreateLeadCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var leadId = await _createHandler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetLead), new { id = leadId }, leadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lead");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] Guid tenantId, [FromQuery] LeadStatus? status, CancellationToken cancellationToken)
    {
        var query = new GetLeadsByTenantQuery(tenantId, status);
        var handler = HttpContext.RequestServices.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
        var leads = await handler.HandleAsync(query, cancellationToken);
        return Ok(leads);
    }

    [HttpGet("{id}")]
    public ActionResult GetLead(Guid id)
    {
        // TODO: Implementar GetLeadQuery
        return Ok(new { id });
    }

    [HttpPost("{id}/qualify")]
    public async Task<ActionResult> QualifyLead(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var command = new QualifyLeadCommand(id, tenantId);
        var result = await _qualifyHandler.HandleAsync(command, cancellationToken);
        
        if (!result)
            return NotFound();

        return NoContent();
    }
}

