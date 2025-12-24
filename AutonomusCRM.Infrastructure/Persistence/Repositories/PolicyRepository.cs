using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class PolicyRepository : Repository<Policy>, IPolicyRepository
{
    public PolicyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Policy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Policy>> GetActiveByTenantAndNameAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.TenantId == tenantId && p.IsActive && p.Name == name)
            .ToListAsync(cancellationToken);
    }
}

