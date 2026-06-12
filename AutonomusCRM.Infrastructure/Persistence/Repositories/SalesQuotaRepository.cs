using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class SalesQuotaRepository : Repository<SalesQuota>, ISalesQuotaRepository
{
    public SalesQuotaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SalesQuota>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(q => q.TenantId == tenantId).ToListAsync(cancellationToken);

    public async Task<SalesQuota?> GetActiveForUserAsync(
        Guid tenantId, Guid userId, string periodType, DateTime asOf, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(q => q.TenantId == tenantId && q.UserId == userId && q.PeriodType == periodType
                        && q.PeriodStart <= asOf && q.PeriodEnd >= asOf)
            .OrderByDescending(q => q.PeriodStart)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetActiveMonthlyQuotaTargetsAsync(
        Guid tenantId,
        DateTime asOf,
        CancellationToken cancellationToken = default)
    {
        var quotas = await _dbSet.AsNoTracking()
            .Where(q => q.TenantId == tenantId
                        && q.PeriodType == QuotaPeriodTypes.Monthly
                        && q.PeriodStart <= asOf
                        && q.PeriodEnd >= asOf)
            .OrderByDescending(q => q.PeriodStart)
            .ToListAsync(cancellationToken);

        return quotas
            .GroupBy(q => q.UserId)
            .ToDictionary(g => g.Key, g => g.First().TargetAmount);
    }
}
