using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;

namespace AutonomusCRM.Infrastructure.Revenue;

public class RevenueForecastEngine : IRevenueForecastEngine
{
    private readonly IDealRepository _dealRepository;
    private static readonly int[] Horizons = { 30, 60, 90, 180 };

    public RevenueForecastEngine(IDealRepository dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task<IReadOnlyList<RevenueForecastDto>> GetForecastAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var winRate = (await _dealRepository.GetWinRateCountsAsync(tenantId, cancellationToken)).WinRatePercent;
        var confidence = Math.Clamp(0.5 + (winRate / 100.0 * 0.5), 0.35, 0.95);
        var horizons = await _dealRepository.GetForecastHorizonsAsync(tenantId, Horizons, cancellationToken);

        return horizons
            .Select(h => new RevenueForecastDto(
                h.HorizonDays,
                Math.Round(h.WeightedSum * (decimal)confidence, 2),
                h.TotalAmount,
                winRate,
                confidence))
            .ToList();
    }
}
