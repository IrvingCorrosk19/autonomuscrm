using AutonomusCRM.Domain.Tenants;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> IsKillSwitchEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

