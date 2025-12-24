using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class LeadRepository : Repository<Lead>, ILeadRepository
{
    public LeadRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Lead>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lead>> GetByStatusAsync(Guid tenantId, LeadStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.TenantId == tenantId && l.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lead>> GetByAssignedUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.TenantId == tenantId && l.AssignedToUserId == userId).ToListAsync(cancellationToken);
    }
}

