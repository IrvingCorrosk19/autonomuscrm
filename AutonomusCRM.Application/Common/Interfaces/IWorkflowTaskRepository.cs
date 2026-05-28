using AutonomusCRM.Application.Automation.Workflows;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IWorkflowTaskRepository : IRepository<WorkflowTask>
{
    Task<IEnumerable<WorkflowTask>> GetOpenByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
