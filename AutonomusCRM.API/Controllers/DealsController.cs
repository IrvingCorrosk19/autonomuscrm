using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealsController : ControllerBase
{
    private readonly IRequestHandler<CreateDealCommand, Guid> _createHandler;
    private readonly IRequestHandler<UpdateDealStageCommand, bool> _updateStageHandler;
    private readonly IRequestHandler<CloseDealCommand, bool> _closeHandler;
    private readonly ILogger<DealsController> _logger;

    public DealsController(
        IRequestHandler<CreateDealCommand, Guid> createHandler,
        IRequestHandler<UpdateDealStageCommand, bool> updateStageHandler,
        IRequestHandler<CloseDealCommand, bool> closeHandler,
        ILogger<DealsController> logger)
    {
        _createHandler = createHandler;
        _updateStageHandler = updateStageHandler;
        _closeHandler = closeHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateDeal([FromBody] CreateDealCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var dealId = await _createHandler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetDeal), new { id = dealId }, dealId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DealDto>>> GetDeals([FromQuery] Guid tenantId, [FromQuery] DealStatus? status, [FromQuery] DealStage? stage, CancellationToken cancellationToken)
    {
        var query = new GetDealsByTenantQuery(tenantId, status, stage);
        var handler = HttpContext.RequestServices.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
        var deals = await handler.HandleAsync(query, cancellationToken);
        return Ok(deals);
    }

    [HttpGet("{id}")]
    public ActionResult GetDeal(Guid id)
    {
        // TODO: Implementar GetDealQuery
        return Ok(new { id });
    }

    [HttpPut("{id}/stage")]
    public async Task<ActionResult> UpdateStage(Guid id, [FromBody] UpdateDealStageCommand command, CancellationToken cancellationToken)
    {
        if (id != command.DealId)
            return BadRequest(new { error = "ID mismatch" });

        var result = await _updateStageHandler.HandleAsync(command, cancellationToken);
        
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult> CloseDeal(Guid id, [FromBody] CloseDealCommand command, CancellationToken cancellationToken)
    {
        if (id != command.DealId)
            return BadRequest(new { error = "ID mismatch" });

        var result = await _closeHandler.HandleAsync(command, cancellationToken);
        
        if (!result)
            return NotFound();

        return NoContent();
    }
}

