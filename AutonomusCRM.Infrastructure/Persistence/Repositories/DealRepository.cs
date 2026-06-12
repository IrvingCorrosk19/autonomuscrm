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

        var cycleSamples = await baseQuery
            .Where(d => d.Stage == DealStage.ClosedWon && d.ClosedAt != null)
            .Select(d => new { d.CreatedAt, d.ClosedAt })
            .ToListAsync(cancellationToken);

        double? avgCycle = cycleSamples.Count > 0
            ? cycleSamples
                .Select(d => (d.ClosedAt!.Value - d.CreatedAt).TotalDays)
                .Where(days => days >= 0)
                .DefaultIfEmpty()
                .Average()
            : null;

        return new DealRevenueKpiAggregates(wonCount, lostCount, revenueClosed, lostRevenue, openWeighted, avgCycle);
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
