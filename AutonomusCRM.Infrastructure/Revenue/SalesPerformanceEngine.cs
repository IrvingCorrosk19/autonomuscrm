using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;

namespace AutonomusCRM.Infrastructure.Revenue;

public class SalesPerformanceEngine : ISalesPerformanceEngine
{
    private readonly IDealRepository _dealRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISalesQuotaRepository _quotaRepository;

    public SalesPerformanceEngine(
        IDealRepository dealRepository,
        IUserRepository userRepository,
        ISalesQuotaRepository quotaRepository)
    {
        _dealRepository = dealRepository;
        _userRepository = userRepository;
        _quotaRepository = quotaRepository;
    }

    public async Task<IReadOnlyList<RepPerformanceDto>> GetLeaderboardAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetActiveUserSummariesAsync(tenantId, cancellationToken);
        var (periodStart, periodEnd) = RevenueAnalyticsCore.CurrentMonthlyPeriod();
        var now = DateTime.UtcNow;
        var aggregates = (await _dealRepository.GetRepPerformanceAggregatesAsync(
            tenantId, periodStart, periodEnd, cancellationToken))
            .ToDictionary(a => a.UserId);
        var quotas = await _quotaRepository.GetActiveMonthlyQuotaTargetsAsync(tenantId, now, cancellationToken);

        var rows = users.Select(user =>
        {
            aggregates.TryGetValue(user.Id, out var stats);
            var target = quotas.GetValueOrDefault(user.Id);
            var closed = stats?.RevenueClosed ?? 0m;
            var openWeighted = stats?.OpenWeighted ?? 0m;
            var attainment = target > 0 ? (double)(closed / target * 100) : 0;
            var coverage = target > 0 ? (double)(openWeighted / target * 100) : 0;

            return new RepPerformanceDto(
                user.Id,
                user.Email,
                target,
                closed,
                openWeighted,
                Math.Round(attainment, 1),
                Math.Round(coverage, 1),
                0,
                stats?.OpenCount ?? 0,
                stats?.WonCount ?? 0,
                stats?.LostCount ?? 0);
        }).ToList();

        var ranked = rows.OrderByDescending(r => r.RevenueClosed).ThenByDescending(r => r.AttainmentPercent).ToList();
        return ranked.Select((r, i) => r with { Rank = i + 1 }).ToList();
    }
}
