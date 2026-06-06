using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Application.Leads.Queries;
using LeadDto = AutonomusCRM.Application.Leads.Queries.LeadDto;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly IRequestHandler<CreateLeadCommand, Guid> _createHandler;
    private readonly IRequestHandler<QualifyLeadCommand, bool> _qualifyHandler;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(
        IRequestHandler<CreateLeadCommand, Guid> createHandler,
        IRequestHandler<QualifyLeadCommand, bool> qualifyHandler,
        IStringLocalizer<SharedResource> localizer,
        ILogger<LeadsController> logger)
    {
        _createHandler = createHandler;
        _qualifyHandler = qualifyHandler;
        _localizer = localizer;
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
            return BadRequest(ApiLocalization.Error(_localizer, ex.Message));
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
    public async Task<ActionResult<LeadDto>> GetLead(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var handler = HttpContext.RequestServices.GetRequiredService<IRequestHandler<GetLeadByIdQuery, LeadDto?>>();
        var lead = await handler.HandleAsync(new GetLeadByIdQuery(id, tenantId), cancellationToken);
        if (lead is null)
            return NotFound();
        return Ok(lead);
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
