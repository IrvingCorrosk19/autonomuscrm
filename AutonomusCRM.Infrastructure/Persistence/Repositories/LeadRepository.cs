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
        var total = await query.CountAsync(cancellationToken);
        var qualified = await query.CountAsync(l => l.Status == LeadStatus.Qualified, cancellationToken);
        var newly = await query.CountAsync(l => l.Status == LeadStatus.New, cancellationToken);
        var highScore = await query.CountAsync(l => l.Score != null && l.Score > 70, cancellationToken);
        var scored = query.Where(l => l.Score != null);
        double? avgScore = await scored.AnyAsync(cancellationToken)
            ? await scored.AverageAsync(l => (double)l.Score!, cancellationToken)
            : null;
        return new LeadListSummary(total, qualified, newly, highScore, avgScore);
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
