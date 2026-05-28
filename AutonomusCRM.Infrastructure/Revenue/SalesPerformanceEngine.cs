using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;

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

    public async Task<IReadOnlyList<RepPerformanceDto>> GetLeaderboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var users = (await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(u => u.IsActive).ToList();
        var (periodStart, periodEnd) = RevenueAnalyticsCore.CurrentMonthlyPeriod();
        var now = DateTime.UtcNow;

        var rows = new List<RepPerformanceDto>();
        foreach (var user in users)
        {
            var userDeals = deals.Where(d => d.AssignedToUserId == user.Id).ToList();
            var closed = userDeals.Where(d => d.Stage == DealStage.ClosedWon
                && d.ClosedAt >= periodStart && d.ClosedAt <= periodEnd).Sum(d => d.Amount);
            var openWeighted = userDeals.Where(d => d.Status == DealStatus.Open).Sum(RevenueAnalyticsCore.WeightedAmount);
            var quota = await _quotaRepository.GetActiveForUserAsync(tenantId, user.Id, QuotaPeriodTypes.Monthly, now, cancellationToken);
            var target = quota?.TargetAmount ?? 0;
            var attainment = target > 0 ? (double)(closed / target * 100) : 0;
            var coverage = target > 0 ? (double)(openWeighted / target * 100) : 0;

            rows.Add(new RepPerformanceDto(
                user.Id,
                user.Email,
                target,
                closed,
                openWeighted,
                Math.Round(attainment, 1),
                Math.Round(coverage, 1),
                0,
                userDeals.Count(d => d.Status == DealStatus.Open),
                userDeals.Count(d => d.Stage == DealStage.ClosedWon),
                userDeals.Count(d => d.Stage == DealStage.ClosedLost)));
        }

        var ranked = rows.OrderByDescending(r => r.RevenueClosed).ThenByDescending(r => r.AttainmentPercent).ToList();
        return ranked.Select((r, i) => r with { Rank = i + 1 }).ToList();
    }
}
