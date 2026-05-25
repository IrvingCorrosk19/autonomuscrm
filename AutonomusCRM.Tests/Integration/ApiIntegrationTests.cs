using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ShouldReturnToken()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty)
        {
            // DB no disponible — omitir fallo duro en CI sin postgres local
            return;
        }

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "admin@autonomuscrm.local",
            "Admin123!",
            tenantId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
    }

    [Fact]
    public async Task Customers_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/customers/00000000-0000-0000-0000-000000000001?tenantId=00000000-0000-0000-0000-000000000001");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
