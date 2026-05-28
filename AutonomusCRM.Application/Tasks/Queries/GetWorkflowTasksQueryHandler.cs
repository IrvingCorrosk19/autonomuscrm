using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tasks.Queries;

public class GetWorkflowTasksQueryHandler : IRequestHandler<GetWorkflowTasksQuery, IEnumerable<WorkflowTaskDto>>
{
    private readonly IWorkflowTaskRepository _repository;

    public GetWorkflowTasksQueryHandler(IWorkflowTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<WorkflowTaskDto>> HandleAsync(GetWorkflowTasksQuery request, CancellationToken cancellationToken = default)
    {
        var tasks = await _repository.GetByTenantAsync(
            request.TenantId,
            request.Status,
            request.AssignedToUserId,
            request.OverdueOnly,
            request.Priority,
            cancellationToken);

        return tasks.Select(t => new WorkflowTaskDto(
            t.Id,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.TaskType,
            t.DueDate,
            t.IsOverdue,
            t.AssignedToUserId,
            t.RelatedEntityId,
            t.RelatedEntityType,
            t.CreatedAt));
    }
}
