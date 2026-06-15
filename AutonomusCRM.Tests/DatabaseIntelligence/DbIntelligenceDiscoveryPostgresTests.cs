using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceDiscoveryPostgresTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceDiscoveryPostgresTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    private void RequirePostgres()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException($"PostgreSQL required for discovery tests: {_fixture.SkipReason}");
    }

    [Fact]
    public async Task PostgreSQL_DiscoverNow_PersistsCatalogWithPkFkAndRowCounts()
    {
        RequirePostgres();
        var (client, factory, tenantId) = await LoginAsAdminAsync();
        var connectionId = await DipIntegrationTestHelpers.EnsureConnectionAsync(client, tenantId);

        using var scope = factory.Services.CreateScope();
        var discovery = scope.ServiceProvider.GetRequiredService<IDbSchemaDiscoveryService>();
        var userId = Guid.NewGuid();
        var result = await discovery.DiscoverNowAsync(tenantId, userId, connectionId, null, null);
        Assert.True(result.Snapshot.TableCount + result.Snapshot.ViewCount > 0);
        Assert.True(result.Snapshot.ColumnCount > 0);

        var catalogResp = await client.GetAsync($"/api/db-intelligence/connections/{connectionId}/catalog?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, catalogResp.StatusCode);
        var snap = await catalogResp.Content.ReadFromJsonAsync<DbCatalogSnapshotDto>();
        Assert.NotNull(snap);

        var tablesResp = await client.GetAsync($"/api/db-intelligence/connections/{connectionId}/catalog/tables?tenantId={tenantId}");
        var tables = await tablesResp.Content.ReadFromJsonAsync<List<DbCatalogTableDto>>();
        Assert.NotNull(tables);
        Assert.NotEmpty(tables!);

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var columns = await db.DbCatalogColumns.Where(c => c.ConnectionProfileId == connectionId).ToListAsync();
        Assert.Contains(columns, c => c.IsPrimaryKey || c.IsForeignKey || c.IsIndexed);
    }

    [Fact]
    public async Task Discovery_ApiResponse_DoesNotExposeSecrets()
    {
        RequirePostgres();
        var (client, _, tenantId) = await LoginAsAdminAsync();
        var connectionId = await DipIntegrationTestHelpers.EnsureConnectionAsync(client, tenantId);
        var discovery = await client.PostAsync($"/api/db-intelligence/connections/{connectionId}/discover?tenantId={tenantId}", null);
        var body = discovery.StatusCode == HttpStatusCode.OK
            ? await discovery.Content.ReadAsStringAsync()
            : "{}";
        Assert.DoesNotContain("Panama2020$", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantCannotReadCatalog()
    {
        RequirePostgres();
        var (client, factory, tenantId) = await LoginAsAdminAsync();
        var connectionId = await DipIntegrationTestHelpers.EnsureConnectionAsync(client, tenantId);
        using var scope = factory.Services.CreateScope();
        var discovery = scope.ServiceProvider.GetRequiredService<IDbSchemaDiscoveryService>();
        await discovery.DiscoverNowAsync(tenantId, Guid.NewGuid(), connectionId, null, null);

        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync($"/api/db-intelligence/connections/{connectionId}/catalog?tenantId={otherTenantId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId)> LoginAsAdminAsync()
    {
        RequirePostgres();
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand("admin@autonomuscrm.local", "Admin123!", tenantId));
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, factory, tenantId);
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Discovery Tenant B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
