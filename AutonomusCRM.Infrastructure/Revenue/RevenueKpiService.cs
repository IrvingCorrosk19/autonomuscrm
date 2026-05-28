using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;

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
        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var leads = (await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var users = (await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken)).Count(u => u.IsActive);

        var won = deals.Where(d => d.Stage == DealStage.ClosedWon).ToList();
        var lost = deals.Where(d => d.Stage == DealStage.ClosedLost).ToList();
        var revenueClosed = won.Sum(d => d.Amount);
        var lostRevenue = lost.Sum(d => d.Amount);
        var winRate = (won.Count + lost.Count) > 0 ? won.Count * 100.0 / (won.Count + lost.Count) : 0;
        var avgDeal = won.Any() ? revenueClosed / won.Count : 0;

        var cycleDays = won.Where(d => d.ClosedAt.HasValue)
            .Select(d => (d.ClosedAt!.Value - d.CreatedAt).TotalDays).Where(d => d >= 0).ToList();
        double? avgCycle = cycleDays.Any() ? cycleDays.Average() : null;

        var openWeighted = deals.Where(d => d.Status == DealStatus.Open).Sum(RevenueAnalyticsCore.WeightedAmount);
        var forecasts = await _forecast.GetForecastAsync(tenantId, cancellationToken);
        var forecast90 = forecasts.FirstOrDefault(f => f.HorizonDays == 90)?.WeightedForecast ?? 0;
        var accuracyProxy = revenueClosed > 0 && forecast90 > 0
            ? Math.Min(100, (double)(revenueClosed / forecast90 * 100))
            : 0;

        var coverageList = await _coverage.GetCoverageAsync(tenantId, cancellationToken);
        var teamCoverage = coverageList.FirstOrDefault(c => c.UserId == null)?.CoveragePercent ?? 0;

        var qualified = leads.Count(l => l.Status == LeadStatus.Qualified);
        var conversion = leads.Count > 0 ? qualified * 100.0 / leads.Count : 0;
        var revenuePerRep = users > 0 ? revenueClosed / users : revenueClosed;

        return new RevenueKpiSnapshotDto(
            revenueClosed,
            Math.Round(winRate, 1),
            avgDeal,
            avgCycle.HasValue ? Math.Round(avgCycle.Value, 1) : null,
            Math.Round(accuracyProxy, 1),
            Math.Round(teamCoverage, 1),
            Math.Round(conversion, 1),
            revenuePerRep,
            lostRevenue,
            openWeighted);
    }
}
