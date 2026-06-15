using System.Net;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.Integration;

/// <summary>Stable tenant + admin login for integration and demo-path tests.</summary>
public static class IntegrationTestTenantHelper
{
    public const string AdminEmail = "admin@autonomuscrm.local";
    public const string AdminPassword = "Admin123!";

    public static async Task<Guid> ResolveAdminTenantIdAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (await HasAdminAndCustomersAsync(db, TenantIds.CeoDemo, cancellationToken))
            return TenantIds.CeoDemo;

        var tenantWithData = await (
            from u in db.Users.AsNoTracking()
            where u.Email == AdminEmail
            join c in db.Customers.AsNoTracking() on u.TenantId equals c.TenantId
            group c by u.TenantId into g
            orderby g.Count() descending, g.Key
            select g.Key).FirstOrDefaultAsync(cancellationToken);
        if (tenantWithData != Guid.Empty)
            return tenantWithData;

        if (await db.Users.AsNoTracking().AnyAsync(
                u => u.TenantId == TenantIds.CeoDemo && u.Email == AdminEmail,
                cancellationToken))
            return TenantIds.CeoDemo;

        var fromAdmin = await db.Users.AsNoTracking()
            .Where(u => u.Email == AdminEmail)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        if (fromAdmin != Guid.Empty)
            return fromAdmin;

        return await db.Tenants.AsNoTracking()
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.Id)
            .FirstAsync(cancellationToken);
    }

    private static async Task<bool> HasAdminAndCustomersAsync(
        ApplicationDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(
                u => u.TenantId == tenantId && u.Email == AdminEmail,
                cancellationToken))
            return false;

        return await db.Customers.AsNoTracking().AnyAsync(c => c.TenantId == tenantId, cancellationToken);
    }

    public static async Task<Guid> ResolveAdminTenantIdAsync(
        CustomWebApplicationFactory factory,
        CancellationToken cancellationToken = default)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await ResolveAdminTenantIdAsync(db, cancellationToken);
    }

    public static async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, string Token)> LoginAdminAsync(
        HttpClient client,
        CustomWebApplicationFactory factory,
        CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveAdminTenantIdAsync(factory, cancellationToken);
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(AdminEmail, AdminPassword, tenantId),
            cancellationToken);
        if (login.StatusCode != HttpStatusCode.OK)
        {
            var body = await login.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Admin login failed ({login.StatusCode}) for tenant {tenantId}: {body}");
        }

        var token = (await login.Content.ReadFromJsonAsync<LoginResult>(cancellationToken))!.AccessToken;
        return (client, factory, tenantId, token);
    }
}
