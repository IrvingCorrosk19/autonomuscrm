using Microsoft.AspNetCore.Mvc.Testing;

namespace AutonomusCRM.Tests.Integration;

/// <summary>PostgreSQL (env or Testcontainers) + WebApplicationFactory for E2E/API integration.</summary>
public sealed class PostgresWebApplicationFixture : IAsyncLifetime
{
    private readonly PostgresTestFixture _pg = new();

    public string? SkipReason => _pg.SkipReason;
    public HttpClient? Client { get; private set; }
    public WebApplicationFactory<AutonomusCRM.API.Program>? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _pg.InitializeAsync();
        if (_pg.SkipReason != null)
            return;

        CustomWebApplicationFactory.PostgresConnectionString = _pg.ConnectionString;
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        await _pg.DisposeAsync();
    }
}
