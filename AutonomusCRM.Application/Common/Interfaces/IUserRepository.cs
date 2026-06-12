using AutonomusCRM.Application.Common;
using AutonomusCRM.Domain.Users;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PagedResult<User>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<UserListSummary> GetListSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> CountActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActiveUserSummary>> GetActiveUserSummariesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record ActiveUserSummary(Guid Id, string Email);

public sealed record UserListSummary(
    int TotalCount,
    int ActiveCount,
    int MfaCount,
    int WithRolesCount);

