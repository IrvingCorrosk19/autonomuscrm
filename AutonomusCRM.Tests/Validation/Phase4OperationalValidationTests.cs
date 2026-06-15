using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Tests.Validation;

/// <summary>
/// ABOS Phase 4 — operational validation against live WebApplicationFactory + PostgreSQL (CI services).
/// </summary>
[Collection("PostgresWebIntegration")]
[Trait("Category", "Phase4Validation")]
public sealed class Phase4OperationalValidationTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public Phase4OperationalValidationTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    private HttpClient RequireClient()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        return _fixture.Client ?? throw new InvalidOperationException("HttpClient not initialized.");
    }

    [SkippableFact]
    public async Task Phase4_Health_And_Ready_ReturnHealthy()
    {
        var client = RequireClient();
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        var body = await health.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);

        var ready = await client.GetAsync("/health/ready");
        var readyBody = await ready.Content.ReadAsStringAsync();
        Assert.True(ready.StatusCode is HttpStatusCode.OK,
            $"ready status {ready.StatusCode}: {readyBody}");
        Assert.Contains("Healthy", readyBody, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Phase4_Login_And_LlmHealth()
    {
        var client = RequireClient();
        var factory = _fixture.Factory ?? throw new InvalidOperationException("Factory not initialized.");
        var tenantId = await IntegrationTestTenantHelper.ResolveAdminTenantIdAsync(factory);
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginCommand(IntegrationTestTenantHelper.AdminEmail, IntegrationTestTenantHelper.AdminPassword, tenantId));
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var authed = factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var llmHealth = await authed.GetAsync("/api/ai/llm/health");
        llmHealth.EnsureSuccessStatusCode();

        var smoke = await authed.PostAsync("/api/ai/llm/smoke?provider=openai", null);
        smoke.EnsureSuccessStatusCode();
        var smokeBody = await smoke.Content.ReadAsStringAsync();
        Assert.True(
            smokeBody.Contains("NotConfigured", StringComparison.OrdinalIgnoreCase)
            || smokeBody.Contains("Configured", StringComparison.OrdinalIgnoreCase)
            || smokeBody.Contains("Success", StringComparison.OrdinalIgnoreCase)
            || smokeBody.Contains("BlockedNoLiveOptIn", StringComparison.OrdinalIgnoreCase),
            smokeBody);
    }

    [SkippableFact]
    public async Task Phase4_Customer360_And_RevenueOs()
    {
        var client = await CreateAuthedClientAsync();
        var tenantId = await GetTenantIdAsync();

        var search = await client.GetAsync("/api/data/customer360");
        search.EnsureSuccessStatusCode();
        var results = await search.Content.ReadFromJsonAsync<List<Customer360Dto>>();
        Assert.NotNull(results);
        Assert.NotEmpty(results);

        var detail = await client.GetAsync($"/api/data/customer360/{results[0].CustomerId}");
        detail.EnsureSuccessStatusCode();

        var revenue = await client.GetAsync($"/api/revenue/os-dashboard?tenantId={tenantId}");
        revenue.EnsureSuccessStatusCode();

        var forecast = await client.GetAsync($"/api/revenue/forecast?tenantId={tenantId}");
        forecast.EnsureSuccessStatusCode();
    }

    [SkippableFact]
    public async Task Phase4_Memory_Graph_Reasoning_Chain()
    {
        var client = await CreateAuthedClientAsync();

        var memory = await client.GetAsync("/api/business-memory?take=10");
        memory.EnsureSuccessStatusCode();

        var semantic = await client.GetAsync("/api/memory/search?q=Demo");
        semantic.EnsureSuccessStatusCode();

        var build = await client.PostAsync("/api/graph/build", null);
        build.EnsureSuccessStatusCode();

        var foundation = await client.GetAsync("/api/reasoning/foundation?scenario=default");
        foundation.EnsureSuccessStatusCode();

        var leak = await client.GetAsync("/api/reasoning/revenue/leak");
        leak.EnsureSuccessStatusCode();
    }

    [SkippableFact]
    public async Task Phase4_Integrations_SendGrid_HubSpot_DocumentBlocked()
    {
        var client = await CreateAuthedClientAsync();

        foreach (var provider in new[] { "SendGrid", "HubSpot" })
        {
            var smoke = await client.PostAsync($"/api/integrations/smoke/{provider}", null);
            smoke.EnsureSuccessStatusCode();
            var dto = await smoke.Content.ReadFromJsonAsync<SmokeTestResultDto>();
            Assert.NotNull(dto);
            Assert.True(dto.RequiresCredentials || dto.Message.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase),
                $"{provider}: {dto.Message}");
        }

        var health = await client.GetAsync("/api/integrations/health");
        health.EnsureSuccessStatusCode();
    }

    [SkippableFact]
    public async Task Phase4_DemoScenarios_Reasoning_On_SeededCustomer()
    {
        var client = await CreateAuthedClientAsync();
        var search = await client.GetAsync("/api/data/customer360");
        search.EnsureSuccessStatusCode();
        var results = await search.Content.ReadFromJsonAsync<List<Customer360Dto>>();
        Assert.NotNull(results);
        var customerId = results[0].CustomerId;

        var risk = await client.GetAsync($"/api/reasoning/customer/{customerId}/risk");
        risk.EnsureSuccessStatusCode();

        var renewal = await client.GetAsync($"/api/reasoning/customer/{customerId}/renewal");
        renewal.EnsureSuccessStatusCode();

        var winLoss = await client.GetAsync($"/api/revenue/win-loss?tenantId={await GetTenantIdAsync()}");
        winLoss.EnsureSuccessStatusCode();
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException("Factory not initialized.");
        var tenantId = await IntegrationTestTenantHelper.ResolveAdminTenantIdAsync(factory);
        var client = _fixture.Client ?? throw new InvalidOperationException("HttpClient not initialized.");
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginCommand(IntegrationTestTenantHelper.AdminEmail, IntegrationTestTenantHelper.AdminPassword, tenantId));
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;

        var authed = factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return authed;
    }

    private async Task<Guid> GetTenantIdAsync()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException("Factory not initialized.");
        return await IntegrationTestTenantHelper.ResolveAdminTenantIdAsync(factory);
    }
}
