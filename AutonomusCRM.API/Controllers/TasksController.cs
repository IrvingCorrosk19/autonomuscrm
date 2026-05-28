using AutonomusCRM.Application.Tasks.Commands;
using AutonomusCRM.Application.Tasks.Queries;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowTaskDto>>> GetTasks(
        [FromQuery] Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] Guid? assignedToUserId,
        [FromQuery] bool? overdueOnly,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        var handler = HttpContext.RequestServices.GetRequiredService<IRequestHandler<GetWorkflowTasksQuery, IEnumerable<WorkflowTaskDto>>>();
        var tasks = await handler.HandleAsync(new GetWorkflowTasksQuery(tenantId, status, assignedToUserId, overdueOnly, priority), cancellationToken);
        return Ok(tasks);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var handler = HttpContext.RequestServices.GetRequiredService<IRequestHandler<CompleteWorkflowTaskCommand, bool>>();
        var ok = await handler.HandleAsync(new CompleteWorkflowTaskCommand(id, tenantId), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult> Assign(Guid id, [FromQuery] Guid tenantId, [FromBody] AssignTaskRequest body, CancellationToken cancellationToken)
    {
        var handler = HttpContext.RequestServices.GetRequiredService<IRequestHandler<AssignWorkflowTaskCommand, bool>>();
        var ok = await handler.HandleAsync(new AssignWorkflowTaskCommand(id, tenantId, body.UserId), cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}

public record AssignTaskRequest(Guid UserId);
