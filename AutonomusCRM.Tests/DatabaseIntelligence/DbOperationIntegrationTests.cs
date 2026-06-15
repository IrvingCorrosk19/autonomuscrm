using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Operations;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbOperationIntegrationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbOperationIntegrationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Preview_ReturnsAffectedRows()
    {
        RequirePostgres();
        var (factory, tenantId, _, jobId) = await SeedOperationJobAsync();
        await LoadRowsAsync(factory, tenantId, jobId, OperationSyntheticDatasets.DuplicateCustomers());

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDbOperationService>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;

        var plan = OperationSyntheticDatasets.CleanMergeExcludeTransformImportPlan() with { Exclude = false, Transform = false };
        var preview = await service.PreviewAsync(tenantId, jobId, plan);
        Assert.True(preview.MergedRows >= 1);
        Assert.NotEmpty(preview.Samples);
    }

    [Fact]
    public async Task Execute_ImportCreatesCustomer()
    {
        RequirePostgres();
        var email = $"op-import-{Guid.NewGuid():N}@example.com";
        var rows = OperationSyntheticDatasets.ImportReadySet();
        rows[0].Data["email"] = email;

        var (factory, tenantId, _, jobId) = await SeedOperationJobAsync();
        await LoadRowsAsync(factory, tenantId, jobId, rows);

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDbOperationService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;

        var result = await service.ExecuteAsync(
            tenantId, Guid.NewGuid(), jobId, OperationSyntheticDatasets.ImportOnlyPlan(), null, null);
        Assert.Equal(DbOperationJobStatus.Completed, result.Status);
        Assert.True(result.ImportedRows > 0);
        Assert.True(await db.Customers.AnyAsync(c => c.TenantId == tenantId && c.Email == email));
    }

    [Fact]
    public async Task Rollback_RemovesImportedCustomer()
    {
        RequirePostgres();
        var email = $"op-rollback-{Guid.NewGuid():N}@example.com";
        var rows = OperationSyntheticDatasets.ImportReadySet();
        rows[0].Data["email"] = email;

        var (factory, tenantId, _, jobId) = await SeedOperationJobAsync();
        await LoadRowsAsync(factory, tenantId, jobId, rows);

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDbOperationService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;

        await service.ExecuteAsync(tenantId, Guid.NewGuid(), jobId, OperationSyntheticDatasets.ImportOnlyPlan(), null, null);
        Assert.True(await db.Customers.AnyAsync(c => c.Email == email));

        var rollback = await service.RollbackAsync(tenantId, Guid.NewGuid(), jobId, null, null);
        Assert.True(rollback.DeletedEntities > 0);
        Assert.False(await db.Customers.AnyAsync(c => c.Email == email));
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantCannotReadJob()
    {
        RequirePostgres();
        var (client, factory, tenantId, _, jobId) = await SetupClientAsync();
        var otherTenantId = await EnsureSecondTenantAsync(factory, tenantId);
        var forbidden = await client.GetAsync($"/api/db-intelligence/operations/{jobId}?tenantId={otherTenantId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Api_PreviewViaHttp()
    {
        RequirePostgres();
        var (client, factory, tenantId, _, jobId) = await SetupClientAsync();
        await LoadRowsAsync(factory, tenantId, jobId, OperationSyntheticDatasets.FilterAmountSet());

        var plan = OperationSyntheticDatasets.FilterPlan(1000);
        var resp = await client.PostAsJsonAsync(
            $"/api/db-intelligence/operations/{jobId}/preview?tenantId={tenantId}", plan);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var preview = await resp.Content.ReadFromJsonAsync<DbOperationPreviewResultDto>();
        Assert.NotNull(preview);
        Assert.Equal(1, preview!.ExcludedRows);
    }

    [Fact]
    public async Task ApiSecurity_DoesNotExposeSecrets()
    {
        RequirePostgres();
        var (client, _, tenantId, _, jobId) = await SetupClientAsync();
        var resp = await client.GetAsync($"/api/db-intelligence/operations/{jobId}?tenantId={tenantId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Panama2020$", body);
    }

    private void RequirePostgres()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);
    }

    private async Task<(CustomWebApplicationFactory Factory, Guid TenantId, Guid ConnectionId, Guid JobId)> SeedOperationJobAsync()
    {
        var (_, factory, tenantId, connectionId, jobId) = await SetupClientAsync();
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
            Name = "Operations Test",
            EngineType = DbEngineType.PostgreSQL,
            Host = "127.0.0.1",
            Port = 5432,
            DatabaseName = "autonomuscrm",
            Username = "postgres",
            UsernameMasked = "p***s",
            EncryptedConnectionBlob = [1, 2, 3],
            CreatedByUserId = Guid.NewGuid(),
            IsActive = true
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
        db.DbOperationJobs.Add(new DbOperationJob
        {
            Id = jobId,
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            CreatedByUserId = Guid.NewGuid(),
            Status = DbOperationJobStatus.Pending,
            Stage = DbOperationStages.Validating,
            TotalRows = 0
        });
        await db.SaveChangesAsync();
        return (client, factory, tenantId, connectionId, jobId);
    }

    private static async Task LoadRowsAsync(
        CustomWebApplicationFactory factory, Guid tenantId, Guid jobId, IReadOnlyList<DbOperationRowContext> rows)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDbOperationService>() as DbOperationService
            ?? throw new InvalidOperationException("DbOperationService not registered.");
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;
        await service.LoadSyntheticRowsAsync(tenantId, jobId, rows);
    }

    private static async Task<Guid> EnsureSecondTenantAsync(CustomWebApplicationFactory factory, Guid existingTenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Operations Tenant B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbOperationPageTests
{
    private readonly PostgresWebApplicationFixture _fixture;
    public DbOperationPageTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task OperatePage_ManagerCanLoad()
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

        var page = await client.GetAsync("/DatabaseIntelligence/Operate");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Operations Center", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Filter Studio", html, StringComparison.Ordinal);
        Assert.Contains("Clean Studio", html, StringComparison.Ordinal);
        Assert.Contains("Merge Studio", html, StringComparison.Ordinal);
        Assert.Contains("Enrichment Studio", html, StringComparison.Ordinal);
        Assert.Contains("Exclusion Studio", html, StringComparison.Ordinal);
        Assert.Contains("Transformation Studio", html, StringComparison.Ordinal);
        Assert.Contains("Start a session above", html, StringComparison.Ordinal);
        Assert.Contains("db-intelligence-operate.js", html, StringComparison.Ordinal);
        Assert.Contains("signalr.min.js", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BuildDefaultPlan", html, StringComparison.Ordinal);
    }
}

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbOperationHubTests : IAsyncLifetime
{
    private readonly PostgresWebApplicationFixture _fixture;
    private readonly List<HubConnection> _connections = [];
    public DbOperationHubTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;
    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { foreach (var c in _connections) await c.DisposeAsync(); }

    [Fact]
    public async Task Admin_CanSubscribeToOperationJob()
    {
        if (_fixture.SkipReason != null) throw new InvalidOperationException(_fixture.SkipReason);
        var (tenantId, jobId) = await SeedJobAsync();
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        await conn.InvokeAsync("SubscribeOperationJob", jobId, tenantId);
    }

    [Fact]
    public async Task CrossTenant_SubscribeOperationJob_Rejected()
    {
        if (_fixture.SkipReason != null) throw new InvalidOperationException(_fixture.SkipReason);
        var (tenantId, jobId) = await SeedJobAsync();
        var other = await EnsureSecondTenantAsync(tenantId);
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        var ex = await Assert.ThrowsAsync<HubException>(() => conn.InvokeAsync("SubscribeOperationJob", jobId, other));
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
                Id = Guid.NewGuid(), TenantId = tenantId, Name = "Operation Hub", EngineType = DbEngineType.PostgreSQL,
                Host = "127.0.0.1", Port = 5432, DatabaseName = "autonomuscrm", Username = "postgres",
                UsernameMasked = "p***s", EncryptedConnectionBlob = [1, 2, 3], CreatedByUserId = Guid.NewGuid(), IsActive = true,
                LastTestSucceeded = false
            };
            db.DbConnectionProfiles.Add(connection);
            await db.SaveChangesAsync();
        }
        var job = new DbOperationJob
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ConnectionProfileId = connection.Id,
            CreatedByUserId = Guid.NewGuid(), Status = DbOperationJobStatus.Pending
        };
        db.DbOperationJobs.Add(job);
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
        var tenant = Tenant.Create("DIP Operation Hub B", "Isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
