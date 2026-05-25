using AutonomusCRM.Application.Common.Interfaces;
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
        var db = services.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        logger.LogInformation("Seeding database with demo data...");

        var tenant = Tenant.Create("AutonomusCRM Demo", "Tenant de demostración local");
        await db.Tenants.AddAsync(tenant, cancellationToken);

        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";
        var admin = User.Create(
            tenant.Id,
            configuration["Seed:AdminEmail"] ?? "admin@autonomuscrm.local",
            BCrypt.Net.BCrypt.HashPassword(adminPassword),
            "Admin",
            "Sistema");
        admin.AddRole("Admin");
        admin.AddRole("Manager");
        await db.Users.AddAsync(admin, cancellationToken);

        var sales = User.Create(
            tenant.Id,
            "sales@autonomuscrm.local",
            BCrypt.Net.BCrypt.HashPassword("Sales123!"),
            "Ana",
            "Ventas");
        sales.AddRole("Sales");
        await db.Users.AddAsync(sales, cancellationToken);

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

        logger.LogInformation(
            "Seed completed. TenantId={TenantId}, Admin={Email}, Password={Password}",
            tenant.Id,
            admin.Email,
            adminPassword);
    }
}
