using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DealsController : ControllerBase
{
    private readonly IRequestHandler<CreateDealCommand, Guid> _createHandler;
    private readonly IRequestHandler<UpdateDealStageCommand, bool> _updateStageHandler;
    private readonly IRequestHandler<CloseDealCommand, bool> _closeHandler;
    private readonly IRequestHandler<LoseDealCommand, bool> _loseHandler;
    private readonly IRequestHandler<GetDealByIdQuery, DealDto?> _getDealHandler;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<DealsController> _logger;

    public DealsController(
        IRequestHandler<CreateDealCommand, Guid> createHandler,
        IRequestHandler<UpdateDealStageCommand, bool> updateStageHandler,
        IRequestHandler<CloseDealCommand, bool> closeHandler,
        IRequestHandler<LoseDealCommand, bool> loseHandler,
        IRequestHandler<GetDealByIdQuery, DealDto?> getDealHandler,
        IStringLocalizer<SharedResource> localizer,
        ILogger<DealsController> logger)
    {
        _createHandler = createHandler;
        _updateStageHandler = updateStageHandler;
        _closeHandler = closeHandler;
        _loseHandler = loseHandler;
        _getDealHandler = getDealHandler;
        _localizer = localizer;
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
            return BadRequest(ApiLocalization.Error(_localizer, ex.Message));
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
    public async Task<ActionResult<DealDto>> GetDeal(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var deal = await _getDealHandler.HandleAsync(new GetDealByIdQuery(id, tenantId), cancellationToken);
        if (deal == null)
            return NotFound();
        return Ok(deal);
    }

    [HttpPut("{id}/stage")]
    public async Task<ActionResult> UpdateStage(Guid id, [FromBody] UpdateDealStageCommand command, CancellationToken cancellationToken)
    {
        if (id != command.DealId)
            return BadRequest(ApiLocalization.Error(_localizer, "Api_Error_IdMismatch"));

        var result = await _updateStageHandler.HandleAsync(command, cancellationToken);
        
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult> CloseDeal(Guid id, [FromBody] CloseDealCommand command, CancellationToken cancellationToken)
    {
        if (id != command.DealId)
            return BadRequest(ApiLocalization.Error(_localizer, "Api_Error_IdMismatch"));

        var result = await _closeHandler.HandleAsync(command, cancellationToken);
        
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/lose")]
    public async Task<ActionResult> LoseDeal(Guid id, [FromBody] LoseDealCommand command, CancellationToken cancellationToken)
    {
        if (id != command.DealId)
            return BadRequest(ApiLocalization.Error(_localizer, "Api_Error_IdMismatch"));

        var result = await _loseHandler.HandleAsync(command, cancellationToken);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
