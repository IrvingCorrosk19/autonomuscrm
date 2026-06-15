using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubSignalR")]
public class DataHubProgressHubTests : IAsyncLifetime
{
    private readonly PostgresWebApplicationFixture _fixture;
    private readonly List<HubConnection> _connections = [];

    public DataHubProgressHubTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var c in _connections)
            await c.DisposeAsync();
    }

    [SkippableFact]
    public async Task Admin_CanSubscribeToAnyJobInTenant()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (tenantId, adminJob, managerJob) = await SeedJobsAsync();
        var adminConn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));

        await adminConn.InvokeAsync("SubscribeJob", managerJob, tenantId);
    }

    [SkippableFact]
    public async Task Manager_CanSubscribeToOwnJob()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (tenantId, _, managerJob) = await SeedJobsAsync();
        var managerConn = await ConnectAsync(await LoginAsync("manager@autonomuscrm.local", "Manager123!", tenantId));

        await managerConn.InvokeAsync("SubscribeJob", managerJob, tenantId);
    }

    [SkippableFact]
    public async Task Manager_CannotSubscribeToOtherUsersJob()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (tenantId, adminJob, _) = await SeedJobsAsync();
        var managerConn = await ConnectAsync(await LoginAsync("manager@autonomuscrm.local", "Manager123!", tenantId));

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            managerConn.InvokeAsync("SubscribeJob", adminJob, tenantId));
        Assert.Contains("access denied", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task CrossTenant_SubscribeJob_Rejected()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (tenantId, adminJob, _) = await SeedJobsAsync();
        var otherTenantId = await EnsureSecondTenantAsync(tenantId);
        var adminConn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            adminConn.InvokeAsync("SubscribeJob", adminJob, otherTenantId));
        Assert.Contains("Cross-tenant", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task CrossTenant_SubscribeTenant_Rejected()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = await GetTenantIdAsync();
        var otherTenantId = await EnsureSecondTenantAsync(tenantId);
        var adminConn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            adminConn.InvokeAsync("SubscribeTenant", otherTenantId));
        Assert.Contains("Cross-tenant", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task GuidTampering_SubscribeJob_RejectedWhenTenantMismatch()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (tenantId, adminJob, _) = await SeedJobsAsync();
        var adminConn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        var tamperedTenant = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            adminConn.InvokeAsync("SubscribeJob", adminJob, tamperedTenant));
        Assert.Contains("Cross-tenant", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Manager_CannotHijackTenantWideSubscription()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = await GetTenantIdAsync();
        var managerConn = await ConnectAsync(await LoginAsync("manager@autonomuscrm.local", "Manager123!", tenantId));

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            managerConn.InvokeAsync("SubscribeTenant", tenantId));
        Assert.Contains("Admin or Owner", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Admin_CanSubscribeTenantGroup()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = await GetTenantIdAsync();
        var adminConn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));

        await adminConn.InvokeAsync("SubscribeTenant", tenantId);
    }

    [SkippableFact]
    public async Task OwnerRole_CanSubscribeTenantGroup()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        var tenantId = await GetTenantIdAsync();
        var ownerEmail = $"owner-{Guid.NewGuid():N}@test.local";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = AutonomusCRM.Domain.Users.User.Create(
                tenantId, ownerEmail, BCrypt.Net.BCrypt.HashPassword("Owner123!"), "Owner", "Test");
            user.AddRole("Owner");
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var ownerConn = await ConnectAsync(await LoginAsync(ownerEmail, "Owner123!", tenantId));
        await ownerConn.InvokeAsync("SubscribeTenant", tenantId);
    }

    [SkippableFact]
    public async Task JobGroup_ReceivesProgressUpdate()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        var (tenantId, adminJob, _) = await SeedJobsAsync();
        var adminConn = await ConnectAsync(await LoginAsync("admin@autonomuscrm.local", "Admin123!", tenantId));
        var tcs = new TaskCompletionSource<DataHubProgressUpdateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        adminConn.On<DataHubProgressUpdateDto>("ProgressUpdate", update =>
        {
            if (update.JobId == adminJob)
                tcs.TrySetResult(update);
        });

        await adminConn.InvokeAsync("SubscribeJob", adminJob, tenantId);

        using var scope = factory.Services.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<IDataHubProgressNotifier>();
        await notifier.NotifyProgressAsync(new DataHubProgressUpdateDto(
            adminJob, tenantId, "Testing", 50, 100, 50, 50, 50, 0, 0, 0, 0, 120, null));

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(tcs.Task, completed);
        Assert.Equal(adminJob, (await tcs.Task).JobId);
    }

    private async Task<HubConnection> ConnectAsync(string token)
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/datahub"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();
        await connection.StartAsync();
        _connections.Add(connection);
        return connection;
    }

    private async Task<string> LoginAsync(string email, string password, Guid tenantId)
    {
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password, tenantId));
        if (login.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Login failed for {email}: {login.StatusCode}");
        return (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
    }

    private async Task<Guid> GetTenantIdAsync()
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Tenants.AsNoTracking().OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstAsync();
    }

    private async Task<(Guid TenantId, Guid AdminJobId, Guid ManagerJobId)> SeedJobsAsync()
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstAsync();
        var adminUserId = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Email == "admin@autonomuscrm.local" && u.TenantId == tenantId)
            .Select(u => u.Id).FirstAsync();
        var managerUserId = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Email == "manager@autonomuscrm.local" && u.TenantId == tenantId)
            .Select(u => u.Id).FirstAsync();

        var adminJob = Guid.NewGuid();
        var managerJob = Guid.NewGuid();
        db.DataHubImportJobs.AddRange(
            new DataHubImportJob
            {
                Id = adminJob, TenantId = tenantId, CreatedByUserId = adminUserId,
                FileName = "admin-job.csv", TargetEntity = "Lead",
                Status = DataHubJobStatus.ReadyToImport.ToString(), CreatedAt = DateTime.UtcNow
            },
            new DataHubImportJob
            {
                Id = managerJob, TenantId = tenantId, CreatedByUserId = managerUserId,
                FileName = "manager-job.csv", TargetEntity = "Lead",
                Status = DataHubJobStatus.ReadyToImport.ToString(), CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();
        return (tenantId, adminJob, managerJob);
    }

    private async Task<Guid> EnsureSecondTenantAsync(Guid existingTenantId)
    {
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var other = await db.Tenants.AsNoTracking()
            .Where(t => t.Id != existingTenantId).Select(t => t.Id).FirstOrDefaultAsync();
        if (other != Guid.Empty) return other;

        var tenant = AutonomusCRM.Domain.Tenants.Tenant.Create("SignalR Other Tenant", "isolation");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }
}
