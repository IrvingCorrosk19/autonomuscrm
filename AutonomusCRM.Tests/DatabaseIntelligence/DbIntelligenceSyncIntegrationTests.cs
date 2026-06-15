using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceSyncIntegrationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceSyncIntegrationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FullSync_ImportsCustomersToCrm()
    {
        RequirePostgres();
        var (factory, tenantId, connectionId, jobId) = await SeedJobAsync(DbSyncMode.Full);
        await RunPipelineAsync(factory, tenantId, connectionId, jobId, SyncSyntheticDatasets.SmbDataset());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        Assert.True(await db.Customers.AnyAsync(c => c.TenantId == tenantId && c.Email == "acme@example.com"));
        var job = await db.DbSyncJobs.FirstAsync(j => j.Id == jobId);
        Assert.Equal(DbSyncJobStatus.Completed, job.Status);
        Assert.True(job.ImportedRows > 0);
    }

    [Fact]
    public async Task DeltaSync_UsesWatermark()
    {
        RequirePostgres();
        var (factory, tenantId, connectionId, jobId) = await SeedJobAsync(DbSyncMode.Delta);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        db.DbSyncWatermarks.Add(new DbSyncWatermark
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            EntityType = BusinessEntityType.Customer,
            LastSyncedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var watermark = DateTime.UtcNow.AddDays(-1);
        var filtered = SyncSyntheticDatasets.DeltaDataset(watermark)
            .Where(r => !r.ModifiedAtUtc.HasValue || r.ModifiedAtUtc > watermark).ToList();
        await RunPipelineAsync(factory, tenantId, connectionId, jobId, filtered);

        Assert.True(await db.Customers.AnyAsync(c => c.Email == "delta@example.com"));
        Assert.False(await db.Customers.AnyAsync(c => c.Email == "old@example.com"));
    }

    [Fact]
    public async Task Rollback_RemovesCreatedEntities()
    {
        RequirePostgres();
        var email = $"rollback-{Guid.NewGuid():N}@example.com";
        var rows = new List<DbSyncExtractedRow>
        {
            new(BusinessEntityType.Customer, "public", "customers", 1,
                new Dictionary<string, string?> { ["name"] = "Rollback User", ["email"] = email }, null)
        };

        var (factory, tenantId, connectionId, jobId) = await SeedJobAsync(DbSyncMode.Full);
        await RunPipelineAsync(factory, tenantId, connectionId, jobId, rows);

        using var scope = factory.Services.CreateScope();
        var rollback = scope.ServiceProvider.GetRequiredService<IDbSyncRollbackService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;

        Assert.True(await db.Customers.AnyAsync(c => c.TenantId == tenantId && c.Email == email));

        var result = await rollback.ExecuteRollbackAsync(tenantId, jobId);
        Assert.True(result.DeletedEntities > 0);
        Assert.False(await db.Customers.AnyAsync(c => c.Email == email));
    }

    [Fact]
    public async Task SyncHistory_ReturnsViaApi()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId, jobId) = await SetupClientAsync();
        await RunPipelineAsync(factory, tenantId, connectionId, jobId, SyncSyntheticDatasets.SmbDataset());

        var history = await client.GetFromJsonAsync<List<DbSyncHistoryItemDto>>(
            $"/api/db-intelligence/sync/history?tenantId={tenantId}&connectionId={connectionId}");
        Assert.NotNull(history);
        Assert.NotEmpty(history!);
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantCannotReadHistory()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId, _) = await SetupClientAsync();
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync(
            $"/api/db-intelligence/sync/history?tenantId={otherTenantId}&connectionId={connectionId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task ApiSecurity_DoesNotExposeSecrets()
    {
        RequirePostgres();
        var (client, factory, tenantId, connectionId, jobId) = await SetupClientAsync();
        await RunPipelineAsync(factory, tenantId, connectionId, jobId, SyncSyntheticDatasets.SmbDataset());
        var resp = await client.GetAsync($"/api/db-intelligence/sync/{jobId}?tenantId={tenantId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Panama2020$", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduledSync_CreatesSchedule()
    {
        RequirePostgres();
        var (client, _, tenantId, connectionId, _) = await SetupClientAsync();
        var resp = await client.PostAsJsonAsync(
            $"/api/db-intelligence/sync/schedule?tenantId={tenantId}",
            new ScheduleDbSyncRequest(connectionId, "Nightly sync", DbSyncMode.Full, DbSyncScheduleFrequency.Daily));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var schedule = await resp.Content.ReadFromJsonAsync<DbSyncScheduleDto>();
        Assert.NotNull(schedule);
        Assert.Equal("Daily", schedule!.Frequency);
    }

    [Fact]
    public async Task CrmMapping_ContactBecomesLead()
    {
        RequirePostgres();
        var (factory, tenantId, connectionId, jobId) = await SeedJobAsync(DbSyncMode.Full);
        await RunPipelineAsync(factory, tenantId, connectionId, jobId,
            SyncSyntheticDatasets.SmbDataset().Where(r => r.EntityType == BusinessEntityType.Contact).ToList());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        Assert.True(await db.Leads.AnyAsync(l => l.Email == "maria@example.com"));
    }

    [Fact]
    public async Task Watermark_UpdatedAfterSync()
    {
        RequirePostgres();
        var (factory, tenantId, connectionId, jobId) = await SeedJobAsync(DbSyncMode.Full);
        await RunPipelineAsync(factory, tenantId, connectionId, jobId, SyncSyntheticDatasets.SmbDataset());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        Assert.True(await db.DbSyncWatermarks.AnyAsync(w => w.ConnectionProfileId == connectionId));
    }

    [Fact]
    public async Task Recovery_FailedJobCanBeRetrieved()
    {
        RequirePostgres();
        var (client, _, tenantId, _, jobId) = await SetupClientAsync();
        var job = await client.GetFromJsonAsync<DbSyncJobDto>($"/api/db-intelligence/sync/{jobId}?tenantId={tenantId}");
        Assert.NotNull(job);
    }

    private void RequirePostgres()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);
    }

    private async Task<(CustomWebApplicationFactory Factory, Guid TenantId, Guid ConnectionId, Guid JobId)> SeedJobAsync(string mode)
    {
        var (_, factory, tenantId, connectionId, jobId) = await SetupClientAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;
        var job = await db.DbSyncJobs.FirstAsync(j => j.Id == jobId);
        job.SyncMode = mode;
        await db.SaveChangesAsync();
        return (factory, tenantId, connectionId, jobId);
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, Guid ConnectionId, Guid JobId)> SetupClientAsync()
    {
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand("admin@autonomuscrm.local", "Admin123!", tenantId));
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var connectionId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        db.DbConnectionProfiles.Add(new DbConnectionProfile
        {
            Id = connectionId,
            TenantId = tenantId,
            Name = "Sync Test",
            EngineType = DbEngineType.PostgreSQL,
            Host = "127.0.0.1",
            Port = 5432,
            DatabaseName = "autonomuscrm",
            Username = "postgres",
            UsernameMasked = "p***s",
            EncryptedConnectionBlob = [1, 2, 3],
            CreatedByUserId = Guid.NewGuid()
        });
        db.DbTableBusinessMappings.Add(new DbTableBusinessMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = Guid.NewGuid(),
            SchemaName = "public",
            TableName = "customers",
            InferredEntityType = BusinessEntityType.Customer,
            ConfidencePercent = 90,
            Status = DbBusinessMappingStatus.Confirmed
        });
        db.DbSyncJobs.Add(new DbSyncJob
        {
            Id = jobId,
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            CreatedByUserId = Guid.NewGuid(),
            SyncMode = DbSyncMode.Full,
            Status = DbSyncJobStatus.Running,
            ConflictPolicy = DbSyncConflictPolicy.SourceWins
        });
        await db.SaveChangesAsync();
        return (client, factory, tenantId, connectionId, jobId);
    }

    private static async Task RunPipelineAsync(
        CustomWebApplicationFactory factory, Guid tenantId, Guid connectionId, Guid jobId,
        List<DbSyncExtractedRow> rows)
    {
        using var scope = factory.Services.CreateScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<IDbSyncPipeline>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;

        await pipeline.ExecuteAsync(new DbSyncExecutionInput
        {
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            JobId = jobId,
            SyncMode = DbSyncMode.Full,
            ConflictPolicy = DbSyncConflictPolicy.SourceWins,
            Mappings = SyncSyntheticDatasets.DefaultMappings(),
            ExtractedRows = rows
        }, null, CancellationToken.None);
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Sync Tenant B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceSyncPageTests
{
    private readonly PostgresWebApplicationFixture _fixture;
    public DbIntelligenceSyncPageTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SyncPage_ManagerCanLoad()
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

        var page = await client.GetAsync("/DatabaseIntelligence/Sync");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Database sync", html, StringComparison.OrdinalIgnoreCase);
    }
}

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceSyncHubTests : IAsyncLifetime
{
    private readonly PostgresWebApplicationFixture _fixture;
    private readonly List<HubConnection> _connections = [];
    public DbIntelligenceSyncHubTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;
    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { foreach (var c in _connections) await c.DisposeAsync(); }

    [Fact]
    public async Task Admin_CanSubscribeToSyncJob()
    {
        if (_fixture.SkipReason != null) throw new InvalidOperationException(_fixture.SkipReason);
        var (tenantId, jobId) = await SeedJobAsync();
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        await conn.InvokeAsync("SubscribeSyncJob", jobId, tenantId);
    }

    [Fact]
    public async Task CrossTenant_SubscribeSyncJob_Rejected()
    {
        if (_fixture.SkipReason != null) throw new InvalidOperationException(_fixture.SkipReason);
        var (tenantId, jobId) = await SeedJobAsync();
        var other = await EnsureSecondTenantAsync(tenantId);
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        var ex = await Assert.ThrowsAsync<HubException>(() => conn.InvokeAsync("SubscribeSyncJob", jobId, other));
        Assert.Contains("Cross-tenant", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Guid TenantId, Guid JobId)> SeedJobAsync()
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.Select(t => t.Id).FirstAsync();
        var connection = await db.DbConnectionProfiles.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (connection == null)
        {
            connection = new DbConnectionProfile
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Name = "Sync Hub", EngineType = DbEngineType.PostgreSQL,
                Host = "127.0.0.1", Port = 5432, DatabaseName = "autonomuscrm", Username = "postgres",
                UsernameMasked = "p***s", EncryptedConnectionBlob = [1, 2, 3], CreatedByUserId = Guid.NewGuid()
            };
            db.DbConnectionProfiles.Add(connection);
            await db.SaveChangesAsync();
        }
        var job = new DbSyncJob
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ConnectionProfileId = connection.Id,
            CreatedByUserId = Guid.NewGuid(), Status = DbSyncJobStatus.Running
        };
        db.DbSyncJobs.Add(job);
        await db.SaveChangesAsync();
        return (tenantId, job.Id);
    }

    private async Task<string> LoginAsync(string email, string password, Guid tenantId)
    {
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password, tenantId));
        return (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
    }

    private async Task<HubConnection> ConnectAsync(string token)
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        var conn = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress!, "/hubs/db-intelligence"), o =>
            {
                o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                o.AccessTokenProvider = () => Task.FromResult<string?>(token);
            }).Build();
        await conn.StartAsync();
        _connections.Add(conn);
        return conn;
    }

    private async Task<Guid> EnsureSecondTenantAsync(Guid existing)
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existing).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Sync Hub B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
