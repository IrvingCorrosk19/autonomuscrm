using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class CustomerContractRepository : Repository<CustomerContract>, ICustomerContractRepository
{
    public CustomerContractRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CustomerContract>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(c => c.TenantId == tenantId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<CustomerContract>> GetActiveByCustomerAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(c => c.TenantId == tenantId && c.CustomerId == customerId
                        && c.Status != CustomerSuccessConstants.ContractChurned)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<CustomerContract>> GetRenewingWithinDaysAsync(
        Guid tenantId, int days, CancellationToken cancellationToken = default)
    {
        var maxDate = DateTime.UtcNow.Date.AddDays(days);
        return await _dbSet
            .Where(c => c.TenantId == tenantId
                        && c.Status == CustomerSuccessConstants.ContractActive
                        && c.RenewalDate.Date <= maxDate
                        && c.RenewalDate.Date >= DateTime.UtcNow.Date)
            .OrderBy(c => c.RenewalDate)
            .ToListAsync(cancellationToken);
    }
}
