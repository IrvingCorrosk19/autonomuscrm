using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Persistence.Seed;

/// <summary>
/// Garantiza tenant QA-B con datos distintos para pruebas de aislamiento.
/// </summary>
public static class QaTenantSeeder
{
    public const string TenantBName = "AutonomusFlow QA-B";
    public const string AdminBEmail = "admin-b@qa.autonomusflow.local";

    public static async Task EnsureQaTenantBAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var tenantB = await db.Tenants.FirstOrDefaultAsync(t => t.Id == TenantIds.QaTenantB, cancellationToken);
        if (tenantB is null)
        {
            tenantB = Tenant.CreateWithId(TenantIds.QaTenantB, TenantBName, "Tenant aislado para pruebas multi-tenant Fase 3");
            await db.Tenants.AddAsync(tenantB, cancellationToken);
            logger.LogInformation("Created QA tenant B {TenantId}", TenantIds.QaTenantB);
        }

        if (!await db.Users.AnyAsync(u => u.TenantId == TenantIds.QaTenantB && u.Email == AdminBEmail, cancellationToken))
        {
            var adminB = User.Create(
                TenantIds.QaTenantB,
                AdminBEmail,
                BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                "Admin",
                "QA-B");
            adminB.AddRole("Admin");
            await db.Users.AddAsync(adminB, cancellationToken);
        }

        if (!await db.Customers.AnyAsync(c => c.TenantId == TenantIds.QaTenantB, cancellationToken))
        {
            await db.Customers.AddAsync(
                Customer.Create(TenantIds.QaTenantB, "Cliente EXCLUSIVO QA-B", "exclusive-b@qa.local", null, "QA-B Corp"),
                cancellationToken);
        }

        if (!await db.Leads.AnyAsync(l => l.TenantId == TenantIds.QaTenantB, cancellationToken))
        {
            await db.Leads.AddAsync(
                Lead.Create(TenantIds.QaTenantB, "Lead EXCLUSIVO QA-B", LeadSource.Other, "lead-b@qa.local"),
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
