using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Graph;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceGraphIntegrationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceGraphIntegrationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GraphBuild_PersistsAndReturnsViaApi()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupAsync();

        using var scope = factory.Services.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IDbBusinessGraphBuilder>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        var graph = builder.Build(GraphSyntheticDatasets.SmbDataset());
        var jobId = Guid.NewGuid();
        var snapshotId = await db.DbCatalogSnapshots
            .Where(s => s.ConnectionProfileId == connectionId)
            .Select(s => s.Id)
            .FirstAsync();

        db.DbBusinessGraphJobs.Add(new DbBusinessGraphJob
        {
            Id = jobId,
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            CreatedByUserId = Guid.NewGuid(),
            Status = DbBusinessGraphJobStatus.Completed,
            NodeCount = graph.Nodes.Count,
            EdgeCount = graph.Edges.Count
        });
        db.DbBusinessGraphSnapshots.Add(new DbBusinessGraphSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            GraphJobId = jobId,
            GraphJson = JsonSerializer.Serialize(graph, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        });
        await db.SaveChangesAsync();

        var apiGraph = await client.GetFromJsonAsync<DbBusinessGraphDto>(
            $"/api/db-intelligence/graph/{connectionId}?tenantId={tenantId}");
        Assert.NotNull(apiGraph);
        Assert.NotEmpty(apiGraph!.Nodes);
        Assert.NotEmpty(apiGraph.Edges);

        var nodes = await client.GetFromJsonAsync<List<DbBusinessGraphNodeDto>>(
            $"/api/db-intelligence/graph/{connectionId}/nodes?tenantId={tenantId}");
        Assert.NotNull(nodes);
        Assert.NotEmpty(nodes!);

        var summary = await client.GetFromJsonAsync<DbBusinessGraphSummaryDto>(
            $"/api/db-intelligence/graph/{connectionId}/summary?tenantId={tenantId}");
        Assert.NotNull(summary);
        Assert.True(summary!.NodeCount > 0);
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantCannotReadGraph()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupAsync();
        await SeedGraphAsync(factory, tenantId, connectionId);
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync(
            $"/api/db-intelligence/graph/{connectionId}?tenantId={otherTenantId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task ApiResponse_DoesNotExposeSecrets()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupAsync();
        await SeedGraphAsync(factory, tenantId, connectionId);
        var resp = await client.GetAsync(
            $"/api/db-intelligence/graph/{connectionId}?tenantId={tenantId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Panama2020$", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportGraph_ReturnsPngContent()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId) = await SetupAsync();
        await SeedGraphAsync(factory, tenantId, connectionId);
        var resp = await client.PostAsync(
            $"/api/db-intelligence/graph/{connectionId}/export?tenantId={tenantId}&format=png", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 50);
    }

    [Fact]
    public async Task KnowledgeGraphPersistence_EdgesStoredAfterBuild()
    {
        RequirePostgres();
        var (_, factory, tenantId, _) = await SetupAsync();
        using var scope = factory.Services.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IDbBusinessGraphBuilder>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        var graph = builder.Build(GraphSyntheticDatasets.SmbDataset());
        var paymentEdge = graph.Edges.First(e => e.EdgeType == DbBusinessGraphEdgeTypes.GeneratedPayment);

        db.BusinessKnowledgeGraphEdges.Add(
            AutonomusCRM.Application.EnterpriseAI.BusinessKnowledgeGraphEdge.Link(
                tenantId,
                DbBusinessGraphNodeTypes.DipBusinessEntity,
                paymentEdge.FromNodeId,
                DbBusinessGraphNodeTypes.DipBusinessEntity,
                paymentEdge.ToNodeId,
                paymentEdge.EdgeType,
                paymentEdge.ConfidencePercent / 100m));
        await db.SaveChangesAsync();

        var stored = await db.BusinessKnowledgeGraphEdges
            .Where(e => e.TenantId == tenantId &&
                        e.RelationType == DbBusinessGraphEdgeTypes.GeneratedPayment &&
                        e.SourceId == paymentEdge.FromNodeId)
            .FirstOrDefaultAsync();
        Assert.NotNull(stored);
        Assert.Equal(DbBusinessGraphNodeTypes.DipBusinessEntity, stored!.SourceType);
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
            Name = "Graph Test",
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
        await db.SaveChangesAsync();
        return (client, factory, tenantId, connectionId);
    }

    private static async Task SeedGraphAsync(CustomWebApplicationFactory factory, Guid tenantId, Guid connectionId)
    {
        using var scope = factory.Services.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IDbBusinessGraphBuilder>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        var snapshotId = await db.DbCatalogSnapshots
            .Where(s => s.ConnectionProfileId == connectionId)
            .Select(s => s.Id)
            .FirstAsync();

        var graph = builder.Build(GraphSyntheticDatasets.SmbDataset());
        var jobId = Guid.NewGuid();
        db.DbBusinessGraphJobs.Add(new DbBusinessGraphJob
        {
            Id = jobId,
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            CreatedByUserId = Guid.NewGuid(),
            Status = DbBusinessGraphJobStatus.Completed
        });
        db.DbBusinessGraphSnapshots.Add(new DbBusinessGraphSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            GraphJobId = jobId,
            GraphJson = JsonSerializer.Serialize(graph, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Graph Tenant B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
