using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tasks.Queries;

public record GetWorkflowTasksQuery(
    Guid TenantId,
    string? Status = null,
    Guid? AssignedToUserId = null,
    bool? OverdueOnly = null,
    string? Priority = null
) : IRequest<IEnumerable<WorkflowTaskDto>>;

public record WorkflowTaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    string? TaskType,
    DateTime? DueDate,
    bool IsOverdue,
    Guid? AssignedToUserId,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    DateTime CreatedAt);
