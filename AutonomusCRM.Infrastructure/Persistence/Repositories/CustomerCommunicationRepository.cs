using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class CustomerCommunicationRepository : Repository<CustomerCommunicationLog>, ICustomerCommunicationRepository
{
    public CustomerCommunicationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CustomerCommunicationLog>> GetByTenantAsync(
        Guid tenantId, int take = 100, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
}
