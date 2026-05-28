using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Infrastructure.Automation;

public class OperationalTaskService : IOperationalTaskService
{
    private readonly IWorkflowTaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OperationalTaskService(IWorkflowTaskRepository taskRepository, IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkflowTask> CreateTaskAsync(
        Guid tenantId,
        string title,
        string? description,
        string relatedEntityType,
        Guid relatedEntityId,
        Guid? assignedToUserId,
        DateTime? dueDate,
        string priority,
        string taskType,
        CancellationToken cancellationToken = default)
    {
        var task = WorkflowTask.Create(
            tenantId,
            OperationalConstants.SystemWorkflowId,
            title,
            description,
            relatedEntityId,
            relatedEntityType,
            assignedToUserId,
            dueDate,
            priority,
            taskType);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task;
    }

    public Task<bool> ExistsOpenTaskAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        string taskType,
        CancellationToken cancellationToken = default)
        => _taskRepository.ExistsOpenTaskAsync(tenantId, relatedEntityType, relatedEntityId, taskType, cancellationToken);
}
