using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Tests.Integration;

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
public class ApiIntegrationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public ApiIntegrationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var client = _fixture.Client ?? throw new InvalidOperationException("HttpClient not initialized.");
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task Login_WithSeededAdmin_ShouldReturnToken()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var client = _fixture.Client ?? throw new InvalidOperationException("HttpClient not initialized.");
        var factory = _fixture.Factory ?? throw new InvalidOperationException("Factory not initialized.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstOrDefaultAsync();
        Assert.NotEqual(Guid.Empty, tenantId);

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "admin@autonomuscrm.local",
            "Admin123!",
            tenantId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
    }

    [SkippableFact]
    public async Task Customers_WithoutAuth_ShouldReturnUnauthorized()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var client = _fixture.Client ?? throw new InvalidOperationException("HttpClient not initialized.");
        var response = await client.GetAsync("/api/customers/00000000-0000-0000-0000-000000000001?tenantId=00000000-0000-0000-0000-000000000001");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
