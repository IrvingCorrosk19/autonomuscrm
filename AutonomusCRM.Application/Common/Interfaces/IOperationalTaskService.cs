using AutonomusCRM.Application.Automation.Workflows;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IOperationalTaskService
{
    Task<WorkflowTask> CreateTaskAsync(
        Guid tenantId,
        string title,
        string? description,
        string relatedEntityType,
        Guid relatedEntityId,
        Guid? assignedToUserId,
        DateTime? dueDate,
        string priority,
        string taskType,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsOpenTaskAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        string taskType,
        CancellationToken cancellationToken = default);
}
