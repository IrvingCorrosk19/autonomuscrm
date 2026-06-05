using AutonomusCRM.Application.Common;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IDealRepository : IRepository<Deal>
{
    Task<IEnumerable<Deal>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deal>> GetByCustomerIdAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deal>> GetByStatusAsync(Guid tenantId, DealStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deal>> GetByStageAsync(Guid tenantId, DealStage stage, CancellationToken cancellationToken = default);
    Task<PagedResult<Deal>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        DealStatus? status,
        DealStage? stage,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<DealListSummary> GetListSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record DealListSummary(
    decimal Forecast30,
    decimal Forecast60,
    decimal Forecast90,
    double WinRate,
    decimal RevenueClosed);

