namespace AutonomusCRM.Application.EnterpriseAuth;

public interface IScimUserService
{
    Task<ScimUserResponse> CreateUserAsync(Guid tenantId, ScimUserRequest request, CancellationToken cancellationToken = default);
    Task<ScimUserResponse?> GetUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<ScimUserResponse> UpdateUserAsync(Guid tenantId, Guid userId, ScimUserRequest request, CancellationToken cancellationToken = default);
    Task DeactivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}

public record ScimUserRequest(
    string UserName,
    bool Active,
    string? GivenName,
    string? FamilyName,
    IReadOnlyList<string>? Roles);

public record ScimUserResponse(
    Guid Id,
    string UserName,
    bool Active,
    string? GivenName,
    string? FamilyName,
    IReadOnlyList<string> Roles);
