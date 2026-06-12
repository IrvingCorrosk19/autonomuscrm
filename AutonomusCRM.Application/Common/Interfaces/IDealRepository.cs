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
    Task<DealRevenueKpiAggregates> GetRevenueKpiAggregatesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<DealWinRateCounts> GetWinRateCountsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealForecastHorizonRow>> GetForecastHorizonsAsync(
        Guid tenantId,
        IReadOnlyList<int> horizonDays,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepPerformanceAggregate>> GetRepPerformanceAggregatesAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);
    Task<decimal> GetOpenPipelineAmountSumAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<decimal> GetWonRevenueMonthlyAverageAsync(Guid tenantId, int trailingMonths, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetWonAmountByCustomerAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<decimal> GetWonAmountForCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<DealJourneyMetrics> GetJourneyDealMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetOpenAssignmentLoadByUserAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record DealWinRateCounts(int WonCount, int LostCount)
{
    public double WinRatePercent => (WonCount + LostCount) > 0
        ? WonCount * 100.0 / (WonCount + LostCount)
        : 50.0;
}

public sealed record DealForecastHorizonRow(int HorizonDays, decimal WeightedSum, decimal TotalAmount);

public sealed record RepPerformanceAggregate(
    Guid UserId,
    decimal RevenueClosed,
    decimal OpenWeighted,
    int OpenCount,
    int WonCount,
    int LostCount);

public sealed record DealJourneyMetrics(
    int OpenDealCount,
    int ClosedOutcomeCount,
    int WonCount,
    int DealsWithLeadMetadataCount,
    double? AverageCycleDays);

public sealed record DealRevenueKpiAggregates(
    int WonCount,
    int LostCount,
    decimal RevenueClosed,
    decimal LostRevenue,
    decimal OpenWeightedPipeline,
    double? AverageSalesCycleDays);

public sealed record DealListSummary(
    decimal Forecast30,
    decimal Forecast60,
    decimal Forecast90,
    double WinRate,
    decimal RevenueClosed);

