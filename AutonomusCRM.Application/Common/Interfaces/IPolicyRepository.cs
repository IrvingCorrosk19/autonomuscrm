using AutonomusCRM.Application.Policies;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IPolicyRepository : IRepository<Policy>
{
    Task<IEnumerable<Policy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Policy>> GetActiveByTenantAndNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);
}

