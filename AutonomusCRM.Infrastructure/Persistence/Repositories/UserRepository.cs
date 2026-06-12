using AutonomusCRM.Application.Common;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<Domain.Users.User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Domain.Users.User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Domain.Users.User>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(u => u.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public Task<PagedResult<Domain.Users.User>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_dbSet.AsNoTracking(), tenantId, search)
            .OrderBy(u => u.Email);
        return RepositoryPaging.ToPagedAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().CountAsync(u => u.TenantId == tenantId, cancellationToken);
    }

    public async Task<UserListSummary> GetListSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var summary = await _dbSet.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new UserListSummary(
                g.Count(),
                g.Count(u => u.IsActive),
                g.Count(u => u.MfaEnabled),
                g.Count(u => u.Roles.Any())))
            .FirstOrDefaultAsync(cancellationToken);

        return summary ?? new UserListSummary(0, 0, 0, 0);
    }

    public Task<int> CountActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.IsActive, cancellationToken);

    public async Task<IReadOnlyList<ActiveUserSummary>> GetActiveUserSummariesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.Email)
            .Select(u => new ActiveUserSummary(u.Id, u.Email))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Domain.Users.User> ApplyFilters(
        IQueryable<Domain.Users.User> query,
        Guid tenantId,
        string? search)
    {
        query = query.Where(u => u.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern) ||
                (u.FirstName != null && EF.Functions.ILike(u.FirstName, pattern)) ||
                (u.LastName != null && EF.Functions.ILike(u.LastName, pattern)) ||
                u.Roles.Any(r => EF.Functions.ILike(r, pattern)));
        }
        return query;
    }
}
