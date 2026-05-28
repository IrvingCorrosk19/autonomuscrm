using System.Security.Claims;
using AutonomusCRM.Application.Common.Tenancy;

namespace AutonomusCRM.API.Infrastructure;

public sealed class TenantContext : ITenantContext
{
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public TenantContext(ICurrentTenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    public Guid? TenantId => _tenantAccessor.TenantId;

    public bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = default;
        if (_tenantAccessor.TenantId is not { } value)
            return false;
        tenantId = value;
        return true;
    }

    public void EnsureTenantMatches(Guid requestedTenantId)
    {
        if (!TryGetTenantId(out var userTenant) || userTenant != requestedTenantId)
            throw new UnauthorizedAccessException("TenantId no autorizado para este usuario.");
    }
}
