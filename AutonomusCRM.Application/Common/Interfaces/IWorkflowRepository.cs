using AutonomusCRM.Application.Automation.Workflows;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IWorkflowRepository : IRepository<Workflow>
{
    Task<IEnumerable<Workflow>> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

