using AutonomusCRM.Application.Revenue;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface ISalesQuotaRepository : IRepository<SalesQuota>
{
    Task<IEnumerable<SalesQuota>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SalesQuota?> GetActiveForUserAsync(Guid tenantId, Guid userId, string periodType, DateTime asOf, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetActiveMonthlyQuotaTargetsAsync(
        Guid tenantId,
        DateTime asOf,
        CancellationToken cancellationToken = default);
}
