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
using Xunit;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceConnectionApiTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceConnectionApiTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task CreateConnection_ManagerRole_Forbidden()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "manager@autonomuscrm.local", "Manager123!", tenantId));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync(
            $"/api/db-intelligence/connections?tenantId={tenantId}",
            BuildValidPostgresRequest("Manager Attempt"));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [SkippableFact]
    public async Task CreateConnection_RequiresAdminOrOwner()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId) = await LoginAsAdminAsync();
        var body = BuildValidPostgresRequest("Admin Created");
        var ok = await client.PostAsJsonAsync($"/api/db-intelligence/connections?tenantId={tenantId}", body);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var dto = await ok.Content.ReadFromJsonAsync<DbConnectionProfileDto>();
        Assert.NotNull(dto);
        Assert.DoesNotContain("Panama", JsonSerializer.Serialize(dto));
        Assert.DoesNotContain(body.Password, JsonSerializer.Serialize(dto));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var entity = await db.DbConnectionProfiles.AsNoTracking().FirstAsync(p => p.Id == dto!.Id);
        Assert.NotEmpty(entity.EncryptedConnectionBlob);
        Assert.DoesNotContain(body.Password, System.Text.Encoding.UTF8.GetString(entity.EncryptedConnectionBlob));
        Assert.Equal("DBIV"u8.ToArray(), entity.EncryptedConnectionBlob.AsSpan(0, 4).ToArray());
    }

    [SkippableFact]
    public async Task TenantIsolation_OtherTenantCannotListConnections()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId) = await LoginAsAdminAsync();
        var create = await client.PostAsJsonAsync(
            $"/api/db-intelligence/connections?tenantId={tenantId}",
            BuildValidPostgresRequest("Tenant A Connection"));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync($"/api/db-intelligence/connections?tenantId={otherTenantId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [SkippableFact]
    public async Task TestConnection_UsesConnectorFactory_InvalidCredentialsSafeError()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var (_, _, tenantId, token) = await LoginInternalAsync(client, _fixture.Factory ?? throw new InvalidOperationException());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync("/api/db-intelligence/connections/test", new TestDbConnectionRequest(
            DbEngineType.PostgreSQL,
            "127.0.0.1",
            5432,
            "autonomuscrm",
            "postgres",
            "definitely-wrong-password",
            true));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<DbConnectionTestResultDto>();
        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.DoesNotContain("definitely-wrong-password", result.Message);
    }

    [SkippableFact]
    public async Task PostgreSQL_TestConnection_RealLocalDatabase_Passes()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var (_, _, tenantId, token) = await LoginInternalAsync(client, _fixture.Factory ?? throw new InvalidOperationException());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync("/api/db-intelligence/connections/test", new TestDbConnectionRequest(
            DbEngineType.PostgreSQL,
            "127.0.0.1",
            5432,
            "autonomuscrm",
            "postgres",
            "Panama2020$",
            true));

        Skip.If(resp.StatusCode != HttpStatusCode.OK, "Local PostgreSQL not reachable with expected credentials.");
        var result = await resp.Content.ReadFromJsonAsync<DbConnectionTestResultDto>();
        Assert.NotNull(result);
        Assert.True(result!.Success, result.Message);
    }

    [SkippableFact]
    public async Task CreateAndTest_RegistersForensicAudit()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId) = await LoginAsAdminAsync();
        var create = await client.PostAsJsonAsync(
            $"/api/db-intelligence/connections?tenantId={tenantId}",
            BuildValidPostgresRequest($"Audit Test {Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var audits = await db.DbIntelligenceForensicAudits
            .Where(a => a.TenantId == tenantId && a.Action == DbIntelligenceForensicActions.ConnectionCreated)
            .ToListAsync();
        Assert.NotEmpty(audits);
    }

    [Fact]
    public void ConnectorFactory_ResolvesAllEngines()
    {
        var factory = new AutonomusCRM.Infrastructure.DatabaseIntelligence.DbConnectorFactory();
        foreach (DbEngineType engine in Enum.GetValues<DbEngineType>())
        {
            var connector = factory.Create(engine);
            Assert.Equal(engine, connector.EngineType);
            Assert.True(connector.SupportsReadOnlyMode);
        }
    }

    private static CreateDbConnectionProfileRequest BuildValidPostgresRequest(string name) => new(
        name,
        DbEngineType.PostgreSQL,
        "127.0.0.1",
        5432,
        "autonomuscrm",
        "postgres",
        "Panama2020$",
        true);

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId)> LoginAsAdminAsync()
    {
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        var (_, _, tenantId, token) = await LoginInternalAsync(client, factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, factory, tenantId);
    }

    private static async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, string Token)> LoginInternalAsync(
        HttpClient client, CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "admin@autonomuscrm.local", "Admin123!", tenantId));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        return (client, factory, tenantId, token);
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;

        var tenant = Tenant.Create("DIP Other Tenant", "Database Intelligence isolation test");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
