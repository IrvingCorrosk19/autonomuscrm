namespace AutonomusCRM.Application.Tenancy;

public interface ITenantProvisioningService
{
    Task<Guid> ProvisionTenantAsync(string name, string? description, string adminEmail, string adminPassword, CancellationToken cancellationToken = default);
    Task SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task ResumeTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
