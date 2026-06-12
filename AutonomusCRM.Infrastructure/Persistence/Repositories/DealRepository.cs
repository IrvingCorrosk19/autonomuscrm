using AutonomusCRM.Application.Common;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class DealRepository : Repository<Deal>, IDealRepository
{
    public DealRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Deal>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(d => d.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deal>> GetByCustomerIdAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(d => d.TenantId == tenantId && d.CustomerId == customerId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deal>> GetByStatusAsync(Guid tenantId, DealStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(d => d.TenantId == tenantId && d.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deal>> GetByStageAsync(Guid tenantId, DealStage stage, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(d => d.TenantId == tenantId && d.Stage == stage).ToListAsync(cancellationToken);
    }

    public Task<PagedResult<Deal>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        DealStatus? status,
        DealStage? stage,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_dbSet.AsNoTracking(), tenantId, search, status, stage)
            .OrderByDescending(d => d.CreatedAt);
        return RepositoryPaging.ToPagedAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<DealListSummary> GetListSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var horizon30 = now.AddDays(30);
        var horizon60 = now.AddDays(60);
        var horizon90 = now.AddDays(90);

        var open = _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open && d.ExpectedCloseDate != null);

        var forecast30 = await open
            .Where(d => d.ExpectedCloseDate <= horizon30)
            .SumAsync(d => d.Amount * (d.Probability ?? 0) / 100m, cancellationToken);
        var forecast60 = await open
            .Where(d => d.ExpectedCloseDate > horizon30 && d.ExpectedCloseDate <= horizon60)
            .SumAsync(d => d.Amount * (d.Probability ?? 0) / 100m, cancellationToken);
        var forecast90 = await open
            .Where(d => d.ExpectedCloseDate > horizon60 && d.ExpectedCloseDate <= horizon90)
            .SumAsync(d => d.Amount * (d.Probability ?? 0) / 100m, cancellationToken);

        var won = await _dbSet.AsNoTracking().CountAsync(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedWon, cancellationToken);
        var lost = await _dbSet.AsNoTracking().CountAsync(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedLost, cancellationToken);
        var winRate = (won + lost) > 0 ? won * 100.0 / (won + lost) : 0;
        var revenueClosed = await _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedWon)
            .SumAsync(d => d.Amount, cancellationToken);

        return new DealListSummary(forecast30, forecast60, forecast90, winRate, revenueClosed);
    }

    public async Task<DealRevenueKpiAggregates> GetRevenueKpiAggregatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbSet.AsNoTracking().Where(d => d.TenantId == tenantId);

        var wonCount = await baseQuery.CountAsync(d => d.Stage == DealStage.ClosedWon, cancellationToken);
        var lostCount = await baseQuery.CountAsync(d => d.Stage == DealStage.ClosedLost, cancellationToken);
        var revenueClosed = await baseQuery
            .Where(d => d.Stage == DealStage.ClosedWon)
            .SumAsync(d => d.Amount, cancellationToken);
        var lostRevenue = await baseQuery
            .Where(d => d.Stage == DealStage.ClosedLost)
            .SumAsync(d => d.Amount, cancellationToken);
        var openWeighted = await baseQuery
            .Where(d => d.Status == DealStatus.Open)
            .SumAsync(d => d.Amount * (d.Probability ?? 0) / 100m, cancellationToken);

        var cycleQuery = baseQuery.Where(d => d.Stage == DealStage.ClosedWon && d.ClosedAt != null);
        double? avgCycle = await cycleQuery.AnyAsync(cancellationToken)
            ? await cycleQuery.AverageAsync(
                d => (double?)(d.ClosedAt!.Value - d.CreatedAt).TotalDays,
                cancellationToken)
            : null;

        return new DealRevenueKpiAggregates(wonCount, lostCount, revenueClosed, lostRevenue, openWeighted, avgCycle);
    }

    public async Task<DealWinRateCounts> GetWinRateCountsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbSet.AsNoTracking().Where(d => d.TenantId == tenantId);
        var won = await baseQuery.CountAsync(d => d.Stage == DealStage.ClosedWon, cancellationToken);
        var lost = await baseQuery.CountAsync(d => d.Stage == DealStage.ClosedLost, cancellationToken);
        return new DealWinRateCounts(won, lost);
    }

    public async Task<IReadOnlyList<DealForecastHorizonRow>> GetForecastHorizonsAsync(
        Guid tenantId,
        IReadOnlyList<int> horizonDays,
        CancellationToken cancellationToken = default)
    {
        if (horizonDays.Count == 0)
            return Array.Empty<DealForecastHorizonRow>();

        var now = DateTime.UtcNow;
        var open = _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open);

        var rows = new List<DealForecastHorizonRow>(horizonDays.Count);
        foreach (var days in horizonDays.OrderBy(d => d))
        {
            var horizonEnd = now.AddDays(days);
            var bucket = await open
                .Where(d => !d.ExpectedCloseDate.HasValue || d.ExpectedCloseDate <= horizonEnd)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Weighted = g.Sum(d => d.Amount * (d.Probability ?? 0) / 100m),
                    Total = g.Sum(d => d.Amount)
                })
                .FirstOrDefaultAsync(cancellationToken);

            rows.Add(new DealForecastHorizonRow(days, bucket?.Weighted ?? 0m, bucket?.Total ?? 0m));
        }

        return rows;
    }

    public async Task<IReadOnlyList<RepPerformanceAggregate>> GetRepPerformanceAggregatesAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.AssignedToUserId != null)
            .GroupBy(d => d.AssignedToUserId!.Value)
            .Select(g => new RepPerformanceAggregate(
                g.Key,
                g.Where(d => d.Stage == DealStage.ClosedWon
                             && d.ClosedAt >= periodStart
                             && d.ClosedAt <= periodEnd)
                    .Sum(d => d.Amount),
                g.Where(d => d.Status == DealStatus.Open)
                    .Sum(d => d.Amount * (d.Probability ?? 0) / 100m),
                g.Count(d => d.Status == DealStatus.Open),
                g.Count(d => d.Stage == DealStage.ClosedWon),
                g.Count(d => d.Stage == DealStage.ClosedLost)))
            .ToListAsync(cancellationToken);
    }

    public Task<decimal> GetOpenPipelineAmountSumAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open)
            .SumAsync(d => d.Amount, cancellationToken);

    public async Task<decimal> GetWonRevenueMonthlyAverageAsync(
        Guid tenantId,
        int trailingMonths,
        CancellationToken cancellationToken = default)
    {
        if (trailingMonths <= 0)
            return 0m;

        var since = DateTime.UtcNow.AddMonths(-trailingMonths);
        var total = await _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId
                        && d.Stage == DealStage.ClosedWon
                        && d.ClosedAt >= since)
            .SumAsync(d => d.Amount, cancellationToken);

        return total / trailingMonths;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetWonAmountByCustomerAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedWon)
            .GroupBy(d => d.CustomerId)
            .Select(g => new { g.Key, Sum = g.Sum(d => d.Amount) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Key, r => r.Sum);
    }

    public Task<decimal> GetWonAmountForCustomerAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId
                        && d.CustomerId == customerId
                        && d.Stage == DealStage.ClosedWon)
            .SumAsync(d => d.Amount, cancellationToken);

    public async Task<DealJourneyMetrics> GetJourneyDealMetricsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbSet.AsNoTracking().Where(d => d.TenantId == tenantId);
        var openCount = await baseQuery.CountAsync(d => d.Status == DealStatus.Open, cancellationToken);
        var wonCount = await baseQuery.CountAsync(d => d.Stage == DealStage.ClosedWon, cancellationToken);
        var lostCount = await baseQuery.CountAsync(d => d.Stage == DealStage.ClosedLost, cancellationToken);
        var withLead = await PostgresJsonbQuery.CountJsonbKeyAsync(
            _context, "Deals", tenantId, "LeadId", cancellationToken);

        var cycleQuery = baseQuery.Where(d => d.Stage == DealStage.ClosedWon && d.ClosedAt != null);
        double? avgCycle = await cycleQuery.AnyAsync(cancellationToken)
            ? await cycleQuery.AverageAsync(
                d => (double?)(d.ClosedAt!.Value - d.CreatedAt).TotalDays,
                cancellationToken)
            : null;

        return new DealJourneyMetrics(openCount, wonCount + lostCount, wonCount, withLead, avgCycle);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetOpenAssignmentLoadByUserAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId
                        && d.AssignedToUserId != null
                        && d.Status == DealStatus.Open)
            .GroupBy(d => d.AssignedToUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.UserId, r => r.Count);
    }

    private static IQueryable<Deal> ApplyFilters(
        IQueryable<Deal> query,
        Guid tenantId,
        string? search,
        DealStatus? status,
        DealStage? stage)
    {
        query = query.Where(d => d.TenantId == tenantId);
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        if (stage.HasValue)
            query = query.Where(d => d.Stage == stage.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(d => EF.Functions.ILike(d.Title, pattern));
        }
        return query;
    }
}
