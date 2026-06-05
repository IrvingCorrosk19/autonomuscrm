using AutonomusCRM.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

internal static class RepositoryPaging
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (normalizedPage, normalizedSize);
    }

    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (p, size) = Normalize(page, pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((p - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);
        return new PagedResult<T>(items, total, p, size);
    }
}
