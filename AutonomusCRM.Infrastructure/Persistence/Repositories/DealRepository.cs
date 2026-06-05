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
        var openDeals = await _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open && d.ExpectedCloseDate != null)
            .Select(d => new { d.ExpectedCloseDate, d.Amount, d.Probability })
            .ToListAsync(cancellationToken);

        var forecast30 = openDeals.Where(d => d.ExpectedCloseDate <= now.AddDays(30)).Sum(d => d.Amount * (d.Probability ?? 0) / 100m);
        var forecast60 = openDeals.Where(d => d.ExpectedCloseDate > now.AddDays(30) && d.ExpectedCloseDate <= now.AddDays(60)).Sum(d => d.Amount * (d.Probability ?? 0) / 100m);
        var forecast90 = openDeals.Where(d => d.ExpectedCloseDate > now.AddDays(60) && d.ExpectedCloseDate <= now.AddDays(90)).Sum(d => d.Amount * (d.Probability ?? 0) / 100m);

        var won = await _dbSet.AsNoTracking().CountAsync(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedWon, cancellationToken);
        var lost = await _dbSet.AsNoTracking().CountAsync(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedLost, cancellationToken);
        var winRate = (won + lost) > 0 ? won * 100.0 / (won + lost) : 0;
        var revenueClosed = await _dbSet.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedWon)
            .SumAsync(d => d.Amount, cancellationToken);

        return new DealListSummary(forecast30, forecast60, forecast90, winRate, revenueClosed);
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
