using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<bool> IsKillSwitchEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(tenantId, cancellationToken);
        return tenant?.IsKillSwitchEnabled ?? false;
    }
}

