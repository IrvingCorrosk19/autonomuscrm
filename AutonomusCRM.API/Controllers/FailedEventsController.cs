using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/ops/failed-events")]
[Authorize]
public class FailedEventsController : ControllerBase
{
    private readonly IFailedEventReplayService _replay;
    private readonly ITenantContext _tenant;

    public FailedEventsController(IFailedEventReplayService replay, ITenantContext tenant)
    {
        _replay = replay;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _replay.ListAsync(tenantId, take, cancellationToken));
    }

    [HttpPost("{id:guid}/replay")]
    public async Task<IActionResult> RequestReplay(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var ok = await _replay.MarkReplayRequestedAsync(tenantId, id, cancellationToken);
        return ok ? Accepted(new { id, status = "replay-requested" }) : NotFound();
    }
}
