using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
public class DbIntelligenceBusinessDiscoveryIntegrationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceBusinessDiscoveryIntegrationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    private void RequirePostgres()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException($"PostgreSQL required: {_fixture.SkipReason}");
    }

    [Fact]
    public async Task MappingPersistence_AfterBusinessDiscovery()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupCatalogAndConnectionAsync();

        var run = await client.PostAsync(
            $"/api/db-intelligence/business-discovery/{connectionId}/run?tenantId={tenantId}", null);
        var body = await run.Content.ReadAsStringAsync();
        Assert.True(run.StatusCode == HttpStatusCode.OK, $"Expected OK but got {run.StatusCode}: {body}");
        var discovery = await run.Content.ReadFromJsonAsync<BusinessDiscoveryResultDto>();
        Assert.NotNull(discovery);
        Assert.NotEmpty(discovery!.Mappings);

        var mappings = await client.GetFromJsonAsync<List<DbTableBusinessMappingDto>>(
            $"/api/db-intelligence/business-discovery/mappings?tenantId={tenantId}&connectionId={connectionId}");
        Assert.NotNull(mappings);
        Assert.NotEmpty(mappings!);
    }

    [Fact]
    public async Task ConfirmMapping_PersistsStatus()
    {
        RequirePostgres();
        var (client, _, tenantId, connectionId) = await SetupCatalogAndConnectionAsync();
        await client.PostAsync($"/api/db-intelligence/business-discovery/{connectionId}/run?tenantId={tenantId}", null);
        var mappings = await client.GetFromJsonAsync<List<DbTableBusinessMappingDto>>(
            $"/api/db-intelligence/business-discovery/mappings?tenantId={tenantId}&connectionId={connectionId}");
        var target = mappings!.First(m => m.InferredEntityType != BusinessEntityType.Unknown);

        var confirm = await client.PostAsJsonAsync(
            $"/api/db-intelligence/business-discovery/confirm?tenantId={tenantId}",
            new ConfirmBusinessMappingRequest(target.Id, "Confirm"));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var updated = await confirm.Content.ReadFromJsonAsync<DbTableBusinessMappingDto>();
        Assert.Equal(DbBusinessMappingStatus.Confirmed, updated!.Status);
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantCannotReadMappings()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupCatalogAndConnectionAsync();
        await client.PostAsync($"/api/db-intelligence/business-discovery/{connectionId}/run?tenantId={tenantId}", null);
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync(
            $"/api/db-intelligence/business-discovery/mappings?tenantId={otherTenantId}&connectionId={connectionId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task ApiResponse_DoesNotExposeSecrets()
    {
        RequirePostgres();
        var (client, _, tenantId, connectionId) = await SetupCatalogAndConnectionAsync();
        var run = await client.PostAsync(
            $"/api/db-intelligence/business-discovery/{connectionId}/run?tenantId={tenantId}", null);
        var body = await run.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Panama2020$", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBusinessDiscovery_ReturnsLatestResult()
    {
        RequirePostgres();
        var (client, _, tenantId, connectionId) = await SetupCatalogAndConnectionAsync();
        await client.PostAsync($"/api/db-intelligence/business-discovery/{connectionId}/run?tenantId={tenantId}", null);
        var result = await client.GetFromJsonAsync<BusinessDiscoveryResultDto>(
            $"/api/db-intelligence/business-discovery/{connectionId}?tenantId={tenantId}");
        Assert.NotNull(result);
        Assert.Equal(DbDiscoveryJobStatus.Completed, result!.Status);
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, Guid ConnectionId)> SetupCatalogAndConnectionAsync()
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

        var connectionId = await DipIntegrationTestHelpers.EnsureConnectionAsync(client, tenantId);
        await SeedSyntheticCatalogAsync(db, tenantId, connectionId);
        return (client, factory, tenantId, connectionId);
    }

    private static async Task SeedSyntheticCatalogAsync(ApplicationDbContext db, Guid tenantId, Guid connectionId)
    {
        var snapshots = await db.DbCatalogSnapshots
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .ToListAsync();
        if (snapshots.Count > 0)
        {
            var snapIds = snapshots.Select(s => s.Id).ToList();
            db.DbCatalogRelationships.RemoveRange(await db.DbCatalogRelationships.Where(r => snapIds.Contains(r.SnapshotId)).ToListAsync());
            db.DbCatalogColumns.RemoveRange(await db.DbCatalogColumns.Where(c => snapIds.Contains(c.SnapshotId)).ToListAsync());
            db.DbCatalogTables.RemoveRange(await db.DbCatalogTables.Where(t => snapIds.Contains(t.SnapshotId)).ToListAsync());
            db.DbTableBusinessMappings.RemoveRange(await db.DbTableBusinessMappings.Where(m => snapIds.Contains(m.SnapshotId)).ToListAsync());
            db.DbBusinessDiscoveryJobs.RemoveRange(await db.DbBusinessDiscoveryJobs.Where(j => snapIds.Contains(j.SnapshotId)).ToListAsync());
            db.DbCatalogSnapshots.RemoveRange(snapshots);
            await db.SaveChangesAsync();
        }

        var catalog = BusinessDiscoverySyntheticCatalogs.FullRetailSchema();
        var snapshot = new DbCatalogSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SchemaVersion = 1,
            SchemaCount = 1,
            TableCount = catalog.Tables.Count,
            ColumnCount = catalog.Columns.Count,
            RelationshipCount = catalog.Relationships.Count,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.DbCatalogSnapshots.Add(snapshot);

        foreach (var table in catalog.Tables)
        {
            db.DbCatalogTables.Add(new DbCatalogTable
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConnectionProfileId = connectionId,
                SnapshotId = snapshot.Id,
                SchemaName = table.SchemaName,
                ObjectName = table.TableName,
                ObjectType = table.ObjectType
            });
        }

        foreach (var col in catalog.Columns)
        {
            db.DbCatalogColumns.Add(new DbCatalogColumn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConnectionProfileId = connectionId,
                SnapshotId = snapshot.Id,
                SchemaName = col.SchemaName,
                ObjectName = col.TableName,
                ColumnName = col.ColumnName,
                DataType = col.DataType,
                IsPrimaryKey = col.IsPrimaryKey,
                IsForeignKey = col.IsForeignKey,
                Ordinal = 1
            });
        }

        foreach (var rel in catalog.Relationships)
        {
            db.DbCatalogRelationships.Add(new DbCatalogRelationship
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConnectionProfileId = connectionId,
                SnapshotId = snapshot.Id,
                FromSchema = rel.FromSchema,
                FromTable = rel.FromTable,
                FromColumn = rel.FromColumn,
                ToSchema = rel.ToSchema,
                ToTable = rel.ToTable,
                ToColumn = rel.ToColumn,
                Source = DbRelationshipSource.ExplicitForeignKey,
                ConfidencePercent = 100
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Business Tenant B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
