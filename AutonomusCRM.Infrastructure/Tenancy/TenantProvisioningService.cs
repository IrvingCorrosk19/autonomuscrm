using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.Tenancy;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Users;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Tenancy;

public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        ILogger<TenantProvisioningService> logger)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<Guid> ProvisionTenantAsync(
        string name,
        string? description,
        string adminEmail,
        string adminPassword,
        CancellationToken cancellationToken = default)
    {
        var previousBypass = _tenantAccessor.BypassTenantFilter;
        _tenantAccessor.BypassTenantFilter = true;
        try
        {
            var tenant = Tenant.Create(name, description);
            var admin = User.Create(
                tenant.Id,
                adminEmail,
                BCrypt.Net.BCrypt.HashPassword(adminPassword),
                "Admin",
                name);
            admin.AddRole("Admin");

            await _db.Tenants.AddAsync(tenant, cancellationToken);
            await _db.Users.AddAsync(admin, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Provisioned tenant {TenantId} {Name}", tenant.Id, name);
            return tenant.Id;
        }
        finally
        {
            _tenantAccessor.BypassTenantFilter = previousBypass;
        }
    }

    public async Task SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var previousBypass = _tenantAccessor.BypassTenantFilter;
        _tenantAccessor.BypassTenantFilter = true;
        try
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
                ?? throw new InvalidOperationException("Tenant not found");
            tenant.Deactivate();
            await _db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _tenantAccessor.BypassTenantFilter = previousBypass;
        }
    }

    public async Task ResumeTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var previousBypass = _tenantAccessor.BypassTenantFilter;
        _tenantAccessor.BypassTenantFilter = true;
        try
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
                ?? throw new InvalidOperationException("Tenant not found");
            tenant.Activate();
            await _db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _tenantAccessor.BypassTenantFilter = previousBypass;
        }
    }
}
