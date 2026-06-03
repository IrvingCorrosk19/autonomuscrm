using Microsoft.AspNetCore.Mvc.Testing;

namespace AutonomusCRM.Tests.Integration;

/// <summary>PostgreSQL (Testcontainers) + WebApplicationFactory para E2E sin skip silencioso.</summary>
public sealed class PostgresWebApplicationFixture : IAsyncLifetime
{
    private readonly PostgresTestFixture _pg = new();

    public string? SkipReason => _pg.SkipReason;
    public HttpClient? Client { get; private set; }
    private WebApplicationFactory<AutonomusCRM.API.Program>? _factory;

    public async Task InitializeAsync()
    {
        await _pg.InitializeAsync();
        if (_pg.SkipReason != null)
            return;

        CustomWebApplicationFactory.PostgresConnectionString = _pg.ConnectionString;
        _factory = new CustomWebApplicationFactory();
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        await _pg.DisposeAsync();
    }
}
