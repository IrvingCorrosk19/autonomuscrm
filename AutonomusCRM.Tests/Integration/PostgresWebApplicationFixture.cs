using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.Integration;

/// <summary>PostgreSQL (env or Testcontainers) + WebApplicationFactory for E2E/API integration.</summary>
public sealed class PostgresWebApplicationFixture : IAsyncLifetime
{
    private readonly PostgresTestFixture _pg = new();

    public string? SkipReason => _pg.SkipReason;
    public HttpClient? Client { get; private set; }
    public CustomWebApplicationFactory? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _pg.InitializeAsync();
        if (_pg.SkipReason != null)
            return;

        CustomWebApplicationFactory.PostgresConnectionString = _pg.ConnectionString;
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await ResetIntegrationTestStateAsync();
    }

    private async Task ResetIntegrationTestStateAsync()
    {
        if (Factory == null) return;
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        await db.DbConnectionProfiles.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

        var staleStatuses = new[]
        {
            "Parsing", "Importing", "ReadyToImport", "Analyzing", "Validating", "AutoFixing"
        };
        await db.DataHubImportJobs
            .Where(j => staleStatuses.Contains(j.Status))
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, "Failed"));
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        await _pg.DisposeAsync();
    }
}
