using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Collection("PostgresWebIntegration")]
[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceUnderstandPageTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DbIntelligenceUnderstandPageTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UnderstandPage_ManagerCanLoad()
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

        var page = await client.GetAsync("/DatabaseIntelligence/Understand");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Understand your business", html, StringComparison.OrdinalIgnoreCase);
    }
}
