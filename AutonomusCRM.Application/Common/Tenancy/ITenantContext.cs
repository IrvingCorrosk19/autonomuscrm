namespace AutonomusCRM.Application.Common.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool TryGetTenantId(out Guid tenantId);
    void EnsureTenantMatches(Guid requestedTenantId);
}
