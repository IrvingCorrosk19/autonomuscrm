using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Tests.Integration;

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
public sealed class TenantIsolationApiIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public TenantIsolationApiIntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    private void RequirePostgres()
    {
        if (_fixture.SkipReason != null)
            Assert.Fail($"PostgreSQL integration requiere Docker: {_fixture.SkipReason}");
        Assert.NotNull(_fixture.ConnectionString);
    }

    [Fact]
    public async Task Api_JwtTenantA_CannotQuery_Customer_With_TenantB_QueryParam()
    {
        RequirePostgres();
        CustomWebApplicationFactory.PostgresConnectionString = _fixture.ConnectionString;

        var tenantAccessor = new TestTenantAccessor { BypassTenantFilter = true };
        await using (var seedCtx = new AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext(
            new DbContextOptionsBuilder<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>()
                .UseNpgsql(_fixture.ConnectionString!)
                .Options,
            tenantAccessor))
        {
            var tenantA = Tenant.Create("Api-A-" + Guid.NewGuid().ToString("N")[..8], "test");
            var tenantB = Tenant.Create("Api-B-" + Guid.NewGuid().ToString("N")[..8], "test");
            await seedCtx.Tenants.AddRangeAsync(tenantA, tenantB);
            var custB = Customer.Create(tenantB.Id, "Hidden");
            await seedCtx.Customers.AddAsync(custB);
            var hash = BCrypt.Net.BCrypt.HashPassword("Test123!");
            var userA = User.Create(tenantA.Id, $"user-a-{Guid.NewGuid():N}@iso.test", hash);
            userA.AddRole("Admin");
            await seedCtx.Users.AddAsync(userA);
            await seedCtx.SaveChangesAsync();

            await using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var login = await client.PostAsJsonAsync("/api/auth/login",
                new LoginCommand(userA.Email, "Test123!", tenantA.Id));
            login.EnsureSuccessStatusCode();
            var loginBody = await login.Content.ReadFromJsonAsync<LoginResult>();
            Assert.NotNull(loginBody);
            Assert.False(loginBody.RequiresMfa);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);

            var crossTenant = await client.GetAsync(
                $"/api/customers/{custB.Id}?tenantId={tenantB.Id}");
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, crossTenant.StatusCode);
        }
    }
}
