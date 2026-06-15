using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("Seed:Enabled", true))
            return;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var tenantAccessor = services.GetRequiredService<ICurrentTenantAccessor>();
        tenantAccessor.BypassTenantFilter = true;

        var db = services.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded — ensuring demo role users, QA tenant B, CEO_DEMO.");
            await EnsureDemoRoleUsersAsync(db, logger, cancellationToken);
            await QaTenantSeeder.EnsureQaTenantBAsync(db, logger, cancellationToken);
            await CeoDemoSeeder.EnsureCeoDemoTenantAsync(db, logger, cancellationToken);
            await GlobalManufacturingDemoSeeder.EnsureGlobalManufacturingTenantAsync(db, configuration, logger, cancellationToken);
            return;
        }

        logger.LogInformation("Seeding database with demo data...");

        var tenant = Tenant.Create("AutonomusCRM Demo", "Tenant de demostración local");
        await db.Tenants.AddAsync(tenant, cancellationToken);

        foreach (var demo in DemoRoleUsers.All)
        {
            var user = CreateDemoUser(tenant.Id, demo);
            await db.Users.AddAsync(user, cancellationToken);
        }

        var customers = new[]
        {
            Customer.Create(tenant.Id, "Corporación Alpha", "alpha@corp.com", "+50760000001", "Alpha Corp"),
            Customer.Create(tenant.Id, "Beta Industries", "beta@industries.com", "+50760000002", "Beta SA"),
            Customer.Create(tenant.Id, "Gamma Services", "gamma@services.com", null, "Gamma LLC")
        };
        await db.Customers.AddRangeAsync(customers, cancellationToken);

        var leads = new[]
        {
            Lead.Create(tenant.Id, "Lead Web 1", LeadSource.Website, "lead1@web.com"),
            Lead.Create(tenant.Id, "Referido VIP", LeadSource.Referral, "vip@referral.com"),
            Lead.Create(tenant.Id, "Campaña Email", LeadSource.EmailCampaign, "camp@email.com")
        };
        await db.Leads.AddRangeAsync(leads, cancellationToken);

        var deal = Deal.Create(
            tenant.Id,
            customers[0].Id,
            "Implementación CRM Q1",
            25000m,
            "Deal de demostración");
        await db.Deals.AddAsync(deal, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await QaTenantSeeder.EnsureQaTenantBAsync(db, logger, cancellationToken);
        await CeoDemoSeeder.EnsureCeoDemoTenantAsync(db, logger, cancellationToken);
        await GlobalManufacturingDemoSeeder.EnsureGlobalManufacturingTenantAsync(db, configuration, logger, cancellationToken);

        logger.LogInformation(
            "Seed completed. TenantId={TenantId}. Demo users (password = Rol123!): {Users}",
            tenant.Id,
            string.Join(", ", DemoRoleUsers.All.Select(u => $"{u.Email} ({u.Role})")));
    }

    private static async Task EnsureDemoRoleUsersAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
            return;

        var changed = false;
        foreach (var demo in DemoRoleUsers.All)
        {
            var existing = await db.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == demo.Email, cancellationToken);

            if (existing is null)
            {
                await db.Users.AddAsync(CreateDemoUser(tenant.Id, demo), cancellationToken);
                changed = true;
                logger.LogInformation("Created demo user {Email} with role {Role}", demo.Email, demo.Role);
                continue;
            }

            if (!existing.Roles.Contains(demo.Role))
            {
                existing.AddRole(demo.Role);
                changed = true;
                logger.LogInformation("Added role {Role} to {Email}", demo.Role, demo.Email);
            }
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static User CreateDemoUser(Guid tenantId, DemoRoleUsers.DemoRoleUser demo)
    {
        var user = User.Create(
            tenantId,
            demo.Email,
            BCrypt.Net.BCrypt.HashPassword(DemoRoleUsers.PasswordFor(demo.Role)),
            demo.FirstName,
            demo.LastName);
        user.AddRole(demo.Role);
        return user;
    }
}
