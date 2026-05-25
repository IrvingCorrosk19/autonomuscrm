using AutonomusCRM.Application.Auth;
using AutonomusCRM.Infrastructure.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutonomusCRM.Infrastructure.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private const string KeyPrefix = "refresh:";
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private readonly ICacheService _cache;

    public RefreshTokenService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<string> IssueAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var payload = new RefreshTokenPayload(userId, tenantId, DateTime.UtcNow.Add(RefreshLifetime));
        await _cache.SetAsync(KeyPrefix + token, payload, RefreshLifetime, cancellationToken);
        return token;
    }

    public async Task<RefreshTokenInfo?> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var payload = await _cache.GetAsync<RefreshTokenPayload>(KeyPrefix + refreshToken, cancellationToken);
        if (payload is null || payload.ExpiresAt <= DateTime.UtcNow)
            return null;

        return new RefreshTokenInfo(payload.UserId, payload.TenantId, payload.ExpiresAt);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(KeyPrefix + refreshToken, cancellationToken);

    private sealed record RefreshTokenPayload(Guid UserId, Guid TenantId, DateTime ExpiresAt);
}
