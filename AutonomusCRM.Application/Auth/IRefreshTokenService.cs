namespace AutonomusCRM.Application.Auth;

public interface IRefreshTokenService
{
    Task<string> IssueAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<RefreshTokenInfo?> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public record RefreshTokenInfo(Guid UserId, Guid TenantId, DateTime ExpiresAt);
