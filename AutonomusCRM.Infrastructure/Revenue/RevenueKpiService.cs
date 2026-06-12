using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;

namespace AutonomusCRM.Infrastructure.Revenue;

public class RevenueKpiService : IRevenueKpiService
{
    private readonly IDealRepository _dealRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPipelineCoverageService _coverage;
    private readonly IRevenueForecastEngine _forecast;

    public RevenueKpiService(
        IDealRepository dealRepository,
        ILeadRepository leadRepository,
        IUserRepository userRepository,
        IPipelineCoverageService coverage,
        IRevenueForecastEngine forecast)
    {
        _dealRepository = dealRepository;
        _leadRepository = leadRepository;
        _userRepository = userRepository;
        _coverage = coverage;
        _forecast = forecast;
    }

    public async Task<RevenueKpiSnapshotDto> GetSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var dealAgg = await _dealRepository.GetRevenueKpiAggregatesAsync(tenantId, cancellationToken);
        var leadStats = await _leadRepository.GetConversionStatsAsync(tenantId, cancellationToken);
        var activeUsers = await _userRepository.CountActiveByTenantAsync(tenantId, cancellationToken);

        var winRate = (dealAgg.WonCount + dealAgg.LostCount) > 0
            ? dealAgg.WonCount * 100.0 / (dealAgg.WonCount + dealAgg.LostCount)
            : 0;
        var avgDeal = dealAgg.WonCount > 0 ? dealAgg.RevenueClosed / dealAgg.WonCount : 0;

        var forecasts = await _forecast.GetForecastAsync(tenantId, cancellationToken);
        var forecast90 = forecasts.FirstOrDefault(f => f.HorizonDays == 90)?.WeightedForecast ?? 0;
        var accuracyProxy = dealAgg.RevenueClosed > 0 && forecast90 > 0
            ? Math.Min(100, (double)(dealAgg.RevenueClosed / forecast90 * 100))
            : 0;

        var coverageList = await _coverage.GetCoverageAsync(tenantId, cancellationToken);
        var teamCoverage = coverageList.FirstOrDefault(c => c.UserId == null)?.CoveragePercent ?? 0;
        var revenuePerRep = activeUsers > 0 ? dealAgg.RevenueClosed / activeUsers : dealAgg.RevenueClosed;

        return new RevenueKpiSnapshotDto(
            dealAgg.RevenueClosed,
            Math.Round(winRate, 1),
            avgDeal,
            dealAgg.AverageSalesCycleDays.HasValue ? Math.Round(dealAgg.AverageSalesCycleDays.Value, 1) : null,
            Math.Round(accuracyProxy, 1),
            Math.Round(teamCoverage, 1),
            Math.Round(leadStats.ConversionPercent, 1),
            revenuePerRep,
            dealAgg.LostRevenue,
            dealAgg.OpenWeightedPipeline);
    }
}
