using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Users;
using AutonomusCRM.Infrastructure.DatabaseIntelligence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Persistence.Seed;

/// <summary>
/// Enterprise sales demo tenant — Global Manufacturing Group.
/// Gated by <c>Seed:GlobalManufacturing:Enabled</c> (default false) to avoid heavy startup in tests.
/// </summary>
public static class GlobalManufacturingDemoSeeder
{
    public const string TenantName = GlobalManufacturingDemoTargets.TenantName;

    public static async Task EnsureGlobalManufacturingTenantAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Seed:GlobalManufacturing:Enabled", false))
            return;

        var lite = configuration.GetValue("Seed:GlobalManufacturing:LiteMode", false);
        var targetCustomers = lite ? GlobalManufacturingDemoTargets.LiteCustomers : GlobalManufacturingDemoTargets.Customers;

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == TenantIds.GlobalManufacturing, cancellationToken);
        if (tenant is null)
        {
            tenant = Tenant.CreateWithId(
                TenantIds.GlobalManufacturing,
                TenantName,
                GlobalManufacturingDemoTargets.TenantDescription);
            tenant.UpdateSetting("Demo:Profile", "GlobalManufacturing");
            tenant.UpdateSetting("Demo:Industry", "Manufacturing");
            tenant.UpdateSetting("Plan", "Enterprise");
            await db.Tenants.AddAsync(tenant, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created demo tenant {TenantName} ({TenantId})", TenantName, TenantIds.GlobalManufacturing);
        }

        await EnsureDemoUsersAsync(db, logger, cancellationToken);

        var existingCustomers = await db.Customers.CountAsync(c => c.TenantId == TenantIds.GlobalManufacturing, cancellationToken);
        if (existingCustomers >= targetCustomers)
        {
            await EnsureDashboardSignalsAsync(db, logger, cancellationToken);
            await EnsurePostgreSqlConnectionProfileAsync(db, configuration, logger, cancellationToken);
            return;
        }

        logger.LogInformation(
            "Seeding {TenantName} CRM dataset ({Customers} customers, lite={Lite})…",
            TenantName, targetCustomers, lite);

        await GlobalManufacturingBulkSql.BulkInsertCustomersAsync(db, TenantIds.GlobalManufacturing, targetCustomers, cancellationToken);

        var leadCount = lite ? GlobalManufacturingDemoTargets.LiteLeads : GlobalManufacturingDemoTargets.Leads;
        await GlobalManufacturingBulkSql.BulkInsertLeadsAsync(db, TenantIds.GlobalManufacturing, leadCount, cancellationToken);

        var dealCount = lite ? GlobalManufacturingDemoTargets.LiteDeals : GlobalManufacturingDemoTargets.Deals;
        await GlobalManufacturingBulkSql.BulkInsertDealsAsync(db, TenantIds.GlobalManufacturing, dealCount, cancellationToken);

        var taskCount = lite ? 100 : GlobalManufacturingDemoTargets.Tasks;
        await GlobalManufacturingBulkSql.BulkInsertTasksAsync(
            db, TenantIds.GlobalManufacturing, OperationalConstants.SystemWorkflowId, taskCount, cancellationToken);

        var productEvents = lite ? 200 : GlobalManufacturingDemoTargets.ProductEvents;
        await GlobalManufacturingBulkSql.BulkInsertProductEventsAsync(
            db, TenantIds.GlobalManufacturing, productEvents, cancellationToken);

        await EnsureDashboardSignalsAsync(db, logger, cancellationToken);
        await EnsurePostgreSqlConnectionProfileAsync(db, configuration, logger, cancellationToken);

        logger.LogInformation("{TenantName} CRM seed complete.", TenantName);
    }

    private static async Task EnsureDashboardSignalsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.AiDecisionAudits.AnyAsync(a => a.TenantId == TenantIds.GlobalManufacturing, cancellationToken))
            return;

        var customers = await db.Customers
            .Where(c => c.TenantId == TenantIds.GlobalManufacturing)
            .OrderBy(c => c.CreatedAt)
            .Take(40)
            .ToListAsync(cancellationToken);
        if (customers.Count == 0)
            return;

        var deals = await db.Deals
            .Where(d => d.TenantId == TenantIds.GlobalManufacturing)
            .OrderByDescending(d => d.Amount)
            .Take(20)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < 12; i++)
        {
            var customer = customers[i % customers.Count];
            var evidence = new Dictionary<string, object> { ["outcomeFabric.revenueImpact"] = 25_000m + i * 4_500m };
            await db.AiDecisionAudits.AddAsync(
                AiDecisionAudit.Create(
                    TenantIds.GlobalManufacturing,
                    i % 2 == 0 ? "Renewal" : "Expansion",
                    i % 2 == 0 ? "RenewContract" : "UpsellModule",
                    78 + i,
                    $"GMG executive signal {i + 1}",
                    evidence,
                    customer.Id,
                    agentName: "Revenue Agent"),
                cancellationToken);
        }

        for (var i = 0; i < Math.Min(20, customers.Count); i++)
        {
            var customer = customers[i];
            await db.CustomerAnalyticsSnapshots.AddAsync(
                CustomerAnalyticsSnapshot.Create(
                    TenantIds.GlobalManufacturing,
                    customer.Id,
                    DateTime.UtcNow.AddDays(-i),
                    healthScore: 55 + i,
                    churnRiskScore: 20 + (i % 10),
                    npsScore: 7 + (i % 3),
                    csatScore: 3.8m + (i % 5) * 0.1m,
                    revenueAmount: 80_000m + i * 12_000m,
                    expansionScore: 60 + i,
                    IntelligenceConstants.SegmentGrowth,
                    engagementScore: 50 + i,
                    adoptionScore: 45 + i,
                    activeUsers: 5 + i % 8),
                cancellationToken);
        }

        var adminUser = await db.Users
            .Where(u => u.TenantId == TenantIds.GlobalManufacturing && u.Roles.Contains("Admin"))
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (adminUser == Guid.Empty)
            adminUser = await db.Users.Where(u => u.TenantId == TenantIds.GlobalManufacturing).Select(u => u.Id).FirstAsync(cancellationToken);

        await db.SalesQuotas.AddAsync(
            SalesQuota.Create(
                TenantIds.GlobalManufacturing,
                adminUser,
                QuotaPeriodTypes.Quarterly,
                DateTime.UtcNow.AddMonths(-1),
                DateTime.UtcNow.AddMonths(2),
                2_500_000m),
            cancellationToken);

        foreach (var deal in deals.Take(8))
        {
            await db.CustomerContracts.AddAsync(
                CustomerContract.Create(
                    TenantIds.GlobalManufacturing,
                    deal.CustomerId,
                    deal.Id,
                    DateTime.UtcNow.AddMonths(-8),
                    deal.Amount * 1.2m,
                    12),
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{TenantName}: executive/revenue dashboard signals seeded", TenantName);
    }

    private static async Task EnsurePostgreSqlConnectionProfileAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string profileName = "GMG ERP — PostgreSQL (Primary Demo)";
        if (await db.DbConnectionProfiles.AnyAsync(
                p => p.TenantId == TenantIds.GlobalManufacturing && p.Name == profileName,
                cancellationToken))
            return;

        var host = configuration["Demo:Manufacturing:PostgreSql:Host"] ?? "127.0.0.1";
        var port = configuration.GetValue("Demo:Manufacturing:PostgreSql:Port", 5432);
        var database = configuration["Demo:Manufacturing:PostgreSql:Database"] ?? "autonomuscrm";
        var username = configuration["Demo:Manufacturing:PostgreSql:Username"] ?? "postgres";
        var password = configuration["Demo:Manufacturing:PostgreSql:Password"] ?? "Panama2020$";

        var adminUser = await db.Users
            .Where(u => u.TenantId == TenantIds.GlobalManufacturing && u.Roles.Contains("Admin"))
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (adminUser == Guid.Empty)
            adminUser = await db.Users.Where(u => u.TenantId == TenantIds.GlobalManufacturing).Select(u => u.Id).FirstAsync(cancellationToken);

        var vault = new DbIntelligenceConnectionVault(
            Microsoft.Extensions.Options.Options.Create(new DbIntelligenceSecurityOptions
            {
                ActiveEncryptionKeyId = configuration["DatabaseIntelligence:Security:ActiveEncryptionKeyId"] ?? "v1",
                EncryptionKeys = configuration.GetSection("DatabaseIntelligence:Security:EncryptionKeys")
                    .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>
                    {
                        ["v1"] = "QXV0b25vbXVzQ1JNLURhdGFIdWItQUVTMjU2LUtleSEh"
                    }
            }));

        var entity = new DbConnectionProfile
        {
            Id = Guid.NewGuid(),
            TenantId = TenantIds.GlobalManufacturing,
            Name = profileName,
            EngineType = DbEngineType.PostgreSQL,
            Host = host,
            Port = port,
            DatabaseName = database,
            Username = username,
            UsernameMasked = DbIntelligenceMasking.MaskUsername(username),
            EncryptedConnectionBlob = vault.Encrypt(new DbConnectionSecrets(password)),
            IsReadOnly = true,
            IsActive = true,
            CreatedByUserId = adminUser,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            LastTestedAtUtc = DateTime.UtcNow,
            LastTestSucceeded = true
        };

        await db.DbConnectionProfiles.AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{TenantName}: registered DIP profile {Profile}", TenantName, profileName);
    }

    private static async Task EnsureDemoUsersAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var demo in DemoRoleUsers.All)
        {
            if (await db.Users.AnyAsync(u => u.TenantId == TenantIds.GlobalManufacturing && u.Email == demo.Email, cancellationToken))
                continue;

            var user = User.Create(
                TenantIds.GlobalManufacturing,
                demo.Email,
                BCrypt.Net.BCrypt.HashPassword(DemoRoleUsers.PasswordFor(demo.Role)),
                demo.FirstName,
                demo.LastName);
            user.AddRole(demo.Role);
            await db.Users.AddAsync(user, cancellationToken);
            logger.LogInformation("{TenantName} user {Email} ({Role})", TenantName, demo.Email, demo.Role);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
