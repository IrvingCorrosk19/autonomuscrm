using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Health;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceDataHealthIntegrationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceDataHealthIntegrationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FullScan_PersistsFindingsViaEngineAndApi()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId, snapshotId) = await SetupAsync();

        using var scope = factory.Services.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IDataHealthEngine>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        var scan = engine.Scan(DataHealthSyntheticDatasets.MixedDataset());
        var job = new DataHealthJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            CreatedByUserId = Guid.NewGuid(),
            Status = DataHealthJobStatus.Completed,
            GlobalScore = scan.GlobalScore,
            FindingsCount = scan.Findings.Count,
            CriticalFindings = scan.Findings.Count(f => f.Severity == DataHealthFindingSeverity.Critical),
            CompletedAtUtc = DateTime.UtcNow
        };
        db.DataHealthJobs.Add(job);
        foreach (var f in scan.Findings)
        {
            db.DataHealthFindings.Add(new DataHealthFinding
            {
                Id = f.Id,
                TenantId = tenantId,
                ConnectionProfileId = connectionId,
                SnapshotId = snapshotId,
                HealthJobId = job.Id,
                EntityType = f.EntityType,
                Severity = f.Severity,
                Category = f.Category,
                Title = f.Title,
                Explanation = f.Explanation,
                BusinessImpact = f.BusinessImpact,
                Evidence = f.Evidence,
                Recommendation = f.Recommendation,
                SchemaName = f.SchemaName,
                TableName = f.TableName,
                AffectedCount = f.AffectedCount
            });
        }
        foreach (var s in scan.Scores)
        {
            db.DataHealthScores.Add(new DataHealthScore
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConnectionProfileId = connectionId,
                SnapshotId = snapshotId,
                HealthJobId = job.Id,
                EntityType = s.EntityType,
                Score = s.Score,
                CompletenessScore = s.CompletenessScore,
                ValidityScore = s.ValidityScore,
                ConsistencyScore = s.ConsistencyScore,
                DuplicateScore = s.DuplicateScore
            });
        }
        await db.SaveChangesAsync();

        var latest = await client.GetFromJsonAsync<DataHealthResultDto>(
            $"/api/db-intelligence/health/latest?tenantId={tenantId}&connectionId={connectionId}");
        Assert.NotNull(latest);
        Assert.NotEmpty(latest!.Findings);
        Assert.True(latest.Job.GlobalScore >= 0);
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantCannotReadFindings()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId, _) = await SetupAsync();
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync(
            $"/api/db-intelligence/health/findings?tenantId={otherTenantId}&connectionId={connectionId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task ApiResponse_DoesNotExposeSecrets()
    {
        RequirePostgres();
        var (client, _, tenantId, connectionId, _) = await SetupAsync();
        var resp = await client.GetAsync(
            $"/api/db-intelligence/health/findings?tenantId={tenantId}&connectionId={connectionId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Panama2020$", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHealthJob_ReturnsJobMetadata()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId, snapshotId) = await SetupAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var job = new DataHealthJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            CreatedByUserId = Guid.NewGuid(),
            Status = DataHealthJobStatus.Completed,
            GlobalScore = 72
        };
        db.DataHealthJobs.Add(job);
        await db.SaveChangesAsync();

        var dto = await client.GetFromJsonAsync<DataHealthJobDto>(
            $"/api/db-intelligence/health/{job.Id}?tenantId={tenantId}");
        Assert.NotNull(dto);
        Assert.Equal(72, dto!.GlobalScore);
    }

    private void RequirePostgres()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, Guid ConnectionId, Guid SnapshotId)> SetupAsync()
    {
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand("admin@autonomuscrm.local", "Admin123!", tenantId));
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var connectionId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        db.DbConnectionProfiles.Add(new DbConnectionProfile
        {
            Id = connectionId,
            TenantId = tenantId,
            Name = "Health Test",
            EngineType = DbEngineType.PostgreSQL,
            Host = "127.0.0.1",
            Port = 5432,
            DatabaseName = "autonomuscrm",
            Username = "postgres",
            UsernameMasked = "p***s",
            EncryptedConnectionBlob = [1, 2, 3],
            CreatedByUserId = Guid.NewGuid()
        });
        db.DbCatalogSnapshots.Add(new DbCatalogSnapshot
        {
            Id = snapshotId,
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SchemaVersion = 1
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
            Status = DbBusinessMappingStatus.Inferred
        });
        await db.SaveChangesAsync();
        return (client, factory, tenantId, connectionId, snapshotId);
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Health Tenant B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
