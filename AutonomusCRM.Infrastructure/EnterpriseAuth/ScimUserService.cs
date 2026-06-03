using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAuth;
using AutonomusCRM.Domain.Users;

namespace AutonomusCRM.Infrastructure.EnterpriseAuth;

public sealed class ScimUserService : IScimUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public ScimUserService(IUserRepository users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<ScimUserResponse> CreateUserAsync(Guid tenantId, ScimUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByEmailAsync(tenantId, request.UserName, cancellationToken);
        if (existing != null)
            return Map(existing);

        var tempPassword = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
        var user = User.Create(tenantId, request.UserName, tempPassword, request.GivenName, request.FamilyName);
        foreach (var role in request.Roles ?? Array.Empty<string>())
            user.AddRole(role);
        if (!request.Active)
            user.Deactivate();

        await _users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<ScimUserResponse?> GetUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId) return null;
        return Map(user);
    }

    public async Task<ScimUserResponse> UpdateUserAsync(Guid tenantId, Guid userId, ScimUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");
        if (user.TenantId != tenantId) throw new UnauthorizedAccessException();

        user.UpdateInfo(request.GivenName, request.FamilyName, request.UserName);
        if (request.Active) user.Activate(); else user.Deactivate();

        await _users.UpdateAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task DeactivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");
        if (user.TenantId != tenantId) throw new UnauthorizedAccessException();
        user.Deactivate();
        await _users.UpdateAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private static ScimUserResponse Map(User user) => new(
        user.Id, user.Email, user.IsActive, user.FirstName, user.LastName, user.Roles);
}
