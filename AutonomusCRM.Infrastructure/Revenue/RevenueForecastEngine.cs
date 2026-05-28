using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Revenue;

public class RevenueForecastEngine : IRevenueForecastEngine
{
    private readonly IDealRepository _dealRepository;
    private static readonly int[] Horizons = { 30, 60, 90, 180 };

    public RevenueForecastEngine(IDealRepository dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task<IReadOnlyList<RevenueForecastDto>> GetForecastAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var winRate = RevenueAnalyticsCore.HistoricalWinRate(deals) / 100.0;
        var confidence = Math.Clamp(0.5 + (winRate * 0.5), 0.35, 0.95);
        var now = DateTime.UtcNow;
        var open = deals.Where(d => d.Status == DealStatus.Open).ToList();

        var results = new List<RevenueForecastDto>();
        foreach (var days in Horizons)
        {
            var horizonEnd = now.AddDays(days);
            var inHorizon = open.Where(d =>
                !d.ExpectedCloseDate.HasValue || d.ExpectedCloseDate.Value <= horizonEnd).ToList();

            var weighted = inHorizon.Sum(RevenueAnalyticsCore.WeightedAmount);
            var adjusted = weighted * (decimal)confidence;

            results.Add(new RevenueForecastDto(
                days,
                Math.Round(adjusted, 2),
                inHorizon.Sum(d => d.Amount),
                winRate * 100,
                confidence));
        }

        return results;
    }
}
