using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceProgressHubTests : IAsyncLifetime
{
    private readonly PostgresWebApplicationFixture _fixture;
    private readonly List<HubConnection> _connections = [];

    public DbIntelligenceProgressHubTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var c in _connections)
            await c.DisposeAsync();
    }

    [Fact]
    public async Task Admin_CanSubscribeToDiscoveryJobInTenant()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);

        var (tenantId, jobId) = await SeedDiscoveryJobAsync();
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        await conn.InvokeAsync("SubscribeDiscoveryJob", jobId, tenantId);
    }

    [Fact]
    public async Task CrossTenant_SubscribeDiscoveryJob_Rejected()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);

        var (tenantId, jobId) = await SeedDiscoveryJobAsync();
        var otherTenantId = await EnsureSecondTenantAsync(tenantId);
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        var ex = await Assert.ThrowsAsync<HubException>(() =>
            conn.InvokeAsync("SubscribeDiscoveryJob", jobId, otherTenantId));
        Assert.Contains("Cross-tenant", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_CanSubscribeToBusinessDiscoveryJobInTenant()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);

        var (tenantId, jobId) = await SeedBusinessDiscoveryJobAsync();
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        await conn.InvokeAsync("SubscribeBusinessDiscoveryJob", jobId, tenantId);
    }

    [Fact]
    public async Task CrossTenant_SubscribeBusinessDiscoveryJob_Rejected()
    {
        if (_fixture.SkipReason != null)
            throw new InvalidOperationException(_fixture.SkipReason);

        var (tenantId, jobId) = await SeedBusinessDiscoveryJobAsync();
        var otherTenantId = await EnsureSecondTenantAsync(tenantId);
        var conn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        var ex = await Assert.ThrowsAsync<HubException>(() =>
            conn.InvokeAsync("SubscribeBusinessDiscoveryJob", jobId, otherTenantId));
        Assert.Contains("Cross-tenant", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Guid TenantId, Guid JobId)> SeedBusinessDiscoveryJobAsync()
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
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Business Hub Test",
                EngineType = DbEngineType.PostgreSQL,
                Host = "127.0.0.1",
                Port = 5432,
                DatabaseName = "autonomuscrm",
                Username = "postgres",
                UsernameMasked = "p***s",
                EncryptedConnectionBlob = [1, 2, 3],
                CreatedByUserId = Guid.NewGuid()
            };
            db.DbConnectionProfiles.Add(connection);
        }

        var snapshot = await db.DbCatalogSnapshots.FirstOrDefaultAsync(s => s.ConnectionProfileId == connection.Id);
        if (snapshot == null)
        {
            snapshot = new DbCatalogSnapshot
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConnectionProfileId = connection.Id,
                SchemaVersion = 1,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.DbCatalogSnapshots.Add(snapshot);
        }

        var job = new DbBusinessDiscoveryJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connection.Id,
            SnapshotId = snapshot.Id,
            CreatedByUserId = Guid.NewGuid(),
            Status = DbDiscoveryJobStatus.Running
        };
        db.DbBusinessDiscoveryJobs.Add(job);
        await db.SaveChangesAsync();
        return (tenantId, job.Id);
    }

    private async Task<(Guid TenantId, Guid JobId)> SeedDiscoveryJobAsync()
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
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Hub Test",
                EngineType = DbEngineType.PostgreSQL,
                Host = "127.0.0.1",
                Port = 5432,
                DatabaseName = "autonomuscrm",
                Username = "postgres",
                UsernameMasked = "p***s",
                EncryptedConnectionBlob = [1, 2, 3],
                CreatedByUserId = Guid.NewGuid()
            };
            db.DbConnectionProfiles.Add(connection);
        }

        var job = new DbDiscoveryJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connection.Id,
            CreatedByUserId = Guid.NewGuid(),
            Status = DbDiscoveryJobStatus.Pending
        };
        db.DbDiscoveryJobs.Add(job);
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
            .WithUrl(new Uri(factory.Server.BaseAddress!, "/hubs/db-intelligence"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        await conn.StartAsync();
        _connections.Add(conn);
        return conn;
    }

    private async Task<Guid> EnsureSecondTenantAsync(Guid existingTenantId)
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking().Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;
        var tenant = Tenant.Create("DIP Hub Tenant B", "Hub isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
