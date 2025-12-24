using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Customer>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByStatusAsync(Guid tenantId, CustomerStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.TenantId == tenantId && c.Status == status).ToListAsync(cancellationToken);
    }
}

