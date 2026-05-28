using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Revenue;

public class ExecutiveSalesDashboardService : IExecutiveSalesDashboardService
{
    private readonly IRevenueKpiService _kpis;
    private readonly IRevenueForecastEngine _forecast;
    private readonly ISalesPerformanceEngine _performance;
    private readonly IPipelineCoverageService _coverage;
    private readonly ICommercialSlaEngine _sla;
    private readonly IWinLossAnalyticsService _winLoss;
    private readonly IDealRepository _dealRepository;

    public ExecutiveSalesDashboardService(
        IRevenueKpiService kpis,
        IRevenueForecastEngine forecast,
        ISalesPerformanceEngine performance,
        IPipelineCoverageService coverage,
        ICommercialSlaEngine sla,
        IWinLossAnalyticsService winLoss,
        IDealRepository dealRepository)
    {
        _kpis = kpis;
        _forecast = forecast;
        _performance = performance;
        _coverage = coverage;
        _sla = sla;
        _winLoss = winLoss;
        _dealRepository = dealRepository;
    }

    public async Task<ExecutiveSalesDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var deals = await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var atRisk = deals.Count(d => d.Status == DealStatus.Open
            && d.Metadata.TryGetValue("AtRisk", out var v) && v?.ToString() == "true");

        return new ExecutiveSalesDashboardDto(
            await _kpis.GetSnapshotAsync(tenantId, cancellationToken),
            await _forecast.GetForecastAsync(tenantId, cancellationToken),
            await _performance.GetLeaderboardAsync(tenantId, cancellationToken),
            await _coverage.GetCoverageAsync(tenantId, cancellationToken),
            await _sla.DetectBreachesAsync(tenantId, cancellationToken),
            (await _winLoss.GetAnalysisAsync(tenantId, "reason", cancellationToken)).Take(10).ToList(),
            atRisk);
    }
}
