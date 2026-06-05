using AutonomusCRM.Application.Automation.Workflows;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IWorkflowTaskRepository : IRepository<WorkflowTask>
{
    Task<IEnumerable<WorkflowTask>> GetOpenByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowTask>> GetByTenantAsync(
        Guid tenantId,
        string? status = null,
        Guid? assignedToUserId = null,
        bool? overdueOnly = null,
        string? priority = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsOpenTaskAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        string taskType,
        CancellationToken cancellationToken = default);

    Task<int> CountByTenantAsync(Guid tenantId, string? status = null, CancellationToken cancellationToken = default);

    Task<int> CountOverdueOpenAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
