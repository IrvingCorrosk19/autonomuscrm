using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceInsightIntegrationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceInsightIntegrationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GenerateInsights_PersistsViaEngineInput()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupAsync();
        await SeedDiscoveryChainAsync(factory, tenantId, connectionId);

        using var scope = factory.Services.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IDbIntelligenceInsightEngine>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        var input = InsightSyntheticDatasets.DemoDataset();
        input.TenantId = tenantId;
        input.ConnectionProfileId = connectionId;
        var insights = engine.Generate(input);
        Assert.True(insights.Count >= 8);

        var job = new DbIntelligenceInsightJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = input.SnapshotId,
            CreatedByUserId = Guid.NewGuid(),
            Status = DbIntelligenceInsightJobStatus.Completed,
            Stage = DbIntelligenceInsightStages.Completed,
            ProgressPercent = 100,
            InsightCount = insights.Count,
            CompletedAtUtc = DateTime.UtcNow
        };
        db.DbIntelligenceInsightJobs.Add(job);
        foreach (var dto in insights)
        {
            db.DbIntelligenceInsights.Add(new DbIntelligenceInsight
            {
                Id = dto.Id,
                JobId = job.Id,
                TenantId = tenantId,
                ConnectionProfileId = connectionId,
                Type = dto.Type,
                Category = dto.Category,
                Title = dto.Title,
                Summary = dto.Summary,
                EvidenceJson = "[]",
                ExplainabilityJson = "[]",
                SuggestedAction = dto.SuggestedAction,
                ImpactScore = dto.ImpactScore,
                EffortScore = dto.EffortScore,
                ConfidencePercent = dto.ConfidencePercent,
                SemanticMatchScore = dto.SemanticMatchScore,
                PriorityScore = dto.PriorityScore
            });
        }
        await db.SaveChangesAsync();

        var listed = await client.GetFromJsonAsync<List<DbIntelligenceInsightDto>>(
            $"/api/db-intelligence/insights/{connectionId}?tenantId={tenantId}");
        Assert.NotNull(listed);
        Assert.True(listed!.Count >= 8);
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantCannotListInsights()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupAsync();
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync(
            $"/api/db-intelligence/insights/{connectionId}?tenantId={otherTenantId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task ApiSecurity_DoesNotExposeSecrets()
    {
        RequirePostgres();
        var (client, _, tenantId, connectionId) = await SetupAsync();
        var resp = await client.GetAsync(
            $"/api/db-intelligence/insights/{connectionId}?tenantId={tenantId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Panama2020$", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LatestInsights_ReturnsNotFoundWhenEmpty()
    {
        RequirePostgres();
        var (client, _, tenantId, connectionId) = await SetupAsync();
        var resp = await client.GetAsync(
            $"/api/db-intelligence/insights/{connectionId}/latest?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    private void RequirePostgres()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, Guid ConnectionId)> SetupAsync()
    {
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand("admin@autonomuscrm.local", "Admin123!", tenantId));
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var connectionId = Guid.NewGuid();
        db.DbConnectionProfiles.Add(new DbConnectionProfile
        {
            Id = connectionId,
            TenantId = tenantId,
            Name = "Insights Test",
            EngineType = DbEngineType.PostgreSQL,
            Host = "127.0.0.1",
            Port = 5432,
            DatabaseName = "autonomuscrm",
            Username = "postgres",
            UsernameMasked = "p***s",
            EncryptedConnectionBlob = [1, 2, 3],
            CreatedByUserId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();
        return (client, factory, tenantId, connectionId);
    }

    private static async Task SeedDiscoveryChainAsync(
        CustomWebApplicationFactory factory, Guid tenantId, Guid connectionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        var snapshotId = Guid.NewGuid();
        db.DbCatalogSnapshots.Add(new DbCatalogSnapshot
        {
            Id = snapshotId,
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            TableCount = 1,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.DbTableBusinessMappings.Add(new DbTableBusinessMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            SchemaName = "public",
            TableName = "customers",
            InferredEntityType = BusinessEntityType.Customer,
            ConfidencePercent = 90,
            Status = DbBusinessMappingStatus.Confirmed
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = AutonomusCRM.Domain.Tenants.Tenant.Create("DIP Insights Tenant B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceInsightsPageTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceInsightsPageTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task InsightsPage_ManagerCanLoad()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);

        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand("manager@autonomuscrm.local", "Manager123!", tenantId));
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var page = await client.GetAsync("/DatabaseIntelligence/Insights");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("AI Insights", html, StringComparison.OrdinalIgnoreCase);
    }
}
