using AutonomusCRM.Application.Common;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class LeadRepository : Repository<Lead>, ILeadRepository
{
    public LeadRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Lead>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(l => l.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lead>> GetByStatusAsync(Guid tenantId, LeadStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(l => l.TenantId == tenantId && l.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lead>> GetByAssignedUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(l => l.TenantId == tenantId && l.AssignedToUserId == userId).ToListAsync(cancellationToken);
    }

    public Task<PagedResult<Lead>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        LeadStatus? status,
        LeadSource? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_dbSet.AsNoTracking(), tenantId, search, status, source)
            .OrderByDescending(l => l.CreatedAt);
        return RepositoryPaging.ToPagedAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().CountAsync(l => l.TenantId == tenantId, cancellationToken);
    }

    public async Task<LeadListSummary> GetListSummaryAsync(
        Guid tenantId,
        string? search,
        LeadStatus? status,
        LeadSource? source,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_dbSet.AsNoTracking(), tenantId, search, status, source);
        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Qualified = g.Count(l => l.Status == LeadStatus.Qualified),
                Newly = g.Count(l => l.Status == LeadStatus.New),
                HighScore = g.Count(l => l.Score != null && l.Score > 70),
                AvgScore = g.Where(l => l.Score != null).Average(l => (double?)l.Score)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (summary == null)
            return new LeadListSummary(0, 0, 0, 0, null);

        return new LeadListSummary(summary.Total, summary.Qualified, summary.Newly, summary.HighScore, summary.AvgScore);
    }

    public async Task<LeadConversionStats> GetConversionStatsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var stats = await _dbSet.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Qualified = g.Count(l => l.Status == LeadStatus.Qualified)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats == null || stats.Total == 0)
            return new LeadConversionStats(0, 0, 0);

        return new LeadConversionStats(stats.Total, stats.Qualified, stats.Qualified * 100.0 / stats.Total);
    }

    public async Task<IReadOnlyList<LeadSourceStat>> GetSourceStatsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .GroupBy(l => l.Source)
            .Select(g => new LeadSourceStat(
                g.Key,
                g.Count(),
                g.Count(x => x.Status == LeadStatus.Qualified)))
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Lead> ApplyFilters(
        IQueryable<Lead> query,
        Guid tenantId,
        string? search,
        LeadStatus? status,
        LeadSource? source)
    {
        query = query.Where(l => l.TenantId == tenantId);
        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);
        if (source.HasValue)
            query = query.Where(l => l.Source == source.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.Name, pattern) ||
                (l.Email != null && EF.Functions.ILike(l.Email, pattern)) ||
                (l.Company != null && EF.Functions.ILike(l.Company, pattern)) ||
                (l.Phone != null && l.Phone.Contains(search.Trim())));
        }
        return query;
    }
}
