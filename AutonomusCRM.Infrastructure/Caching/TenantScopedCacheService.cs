using AutonomusCRM.Application.Common.Tenancy;

namespace AutonomusCRM.Infrastructure.Caching;

/// <summary>
/// Prefija todas las claves de cache con tenant:{id}: para evitar leaks cross-tenant.
/// </summary>
public sealed class TenantScopedCacheService : ICacheService
{
    private readonly ICacheService _inner;
    private readonly ICurrentTenantAccessor _tenant;

    public TenantScopedCacheService(ICacheService inner, ICurrentTenantAccessor tenant)
    {
        _inner = inner;
        _tenant = tenant;
    }

    private string ScopeKey(string key)
    {
        if (_tenant.BypassTenantFilter || !_tenant.TenantId.HasValue)
            return $"system:{key}";
        return $"tenant:{_tenant.TenantId:N}:{key}";
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        => _inner.GetAsync<T>(ScopeKey(key), cancellationToken);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        => _inner.SetAsync(ScopeKey(key), value, expiration, cancellationToken);

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _inner.RemoveAsync(ScopeKey(key), cancellationToken);

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var scoped = _tenant.TenantId.HasValue && !_tenant.BypassTenantFilter
            ? $"tenant:{_tenant.TenantId:N}:{pattern}"
            : pattern;
        return _inner.RemoveByPatternAsync(scoped, cancellationToken);
    }
}
