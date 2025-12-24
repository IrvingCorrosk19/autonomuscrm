using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class DealRepository : Repository<Deal>, IDealRepository
{
    public DealRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Deal>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(d => d.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deal>> GetByCustomerIdAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(d => d.TenantId == tenantId && d.CustomerId == customerId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deal>> GetByStatusAsync(Guid tenantId, DealStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(d => d.TenantId == tenantId && d.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deal>> GetByStageAsync(Guid tenantId, DealStage stage, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(d => d.TenantId == tenantId && d.Stage == stage).ToListAsync(cancellationToken);
    }
}

