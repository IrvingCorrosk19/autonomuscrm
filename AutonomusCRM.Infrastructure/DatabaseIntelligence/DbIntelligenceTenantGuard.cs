using AutonomusCRM.Application.DatabaseIntelligence;
using Microsoft.AspNetCore.Http;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence;

public sealed class DbIntelligenceTenantGuard : IDbIntelligenceTenantGuard
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public DbIntelligenceTenantGuard(IHttpContextAccessor? httpContextAccessor = null)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? GetCurrentTenantId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user == null) return null;
        var claim = user.FindFirst("TenantId")?.Value ?? user.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var tid) ? tid : null;
    }

    public bool IsSameTenant(Guid requestedTenantId)
    {
        if (requestedTenantId == Guid.Empty) return false;
        var current = GetCurrentTenantId();
        if (current == null) return false;
        return current.Value == requestedTenantId;
    }

    public void EnsureSameTenant(Guid requestedTenantId)
    {
        if (!IsSameTenant(requestedTenantId))
            throw new DbIntelligenceTenantAccessException("Cross-tenant access denied.");
    }

    public bool IsAdminOrOwner()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true &&
               (user.IsInRole("Admin") || user.IsInRole("Owner"));
    }
}
