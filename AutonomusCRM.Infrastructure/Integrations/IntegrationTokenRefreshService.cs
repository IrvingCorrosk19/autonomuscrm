using System.Text.Json;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class IntegrationTokenRefreshService : IIntegrationTokenRefreshService
{
    private readonly ITenantIntegrationRepository _repo;
    private readonly IntegrationOAuthOptions _options;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<IntegrationTokenRefreshService> _logger;

    public IntegrationTokenRefreshService(
        ITenantIntegrationRepository repo,
        IOptions<IntegrationOAuthOptions> options,
        IHttpClientFactory http,
        ILogger<IntegrationTokenRefreshService> logger)
    {
        _repo = repo;
        _options = options.Value;
        _http = http;
        _logger = logger;
    }

    public async Task<int> RefreshExpiringTokensAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var provider in new[] { IntegrationProviders.HubSpot, IntegrationProviders.Salesforce, IntegrationProviders.Gmail, IntegrationProviders.Outlook })
        {
            if (await RefreshProviderAsync(tenantId, provider, cancellationToken))
                count++;
        }

        return count;
    }

    public async Task<bool> RefreshProviderAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default)
    {
        var conn = await _repo.GetAsync(tenantId, provider, cancellationToken);
        if (conn == null || string.IsNullOrWhiteSpace(conn.RefreshToken)) return false;

        try
        {
            var (access, refresh, instance) = await RefreshTokenAsync(provider, conn.RefreshToken!, cancellationToken);
            conn.Configure(access, refresh ?? conn.RefreshToken, instance ?? conn.InstanceUrl, conn.Settings);
            conn.Settings["tokenRefreshedAt"] = DateTime.UtcNow.ToString("O");
            await _repo.UpsertAsync(conn, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed {Provider} tenant {TenantId}", provider, tenantId);
            return false;
        }
    }

    private async Task<(string? access, string? refresh, string? instance)> RefreshTokenAsync(
        string provider, string refreshToken, CancellationToken ct)
    {
        var client = _http.CreateClient("OAuth");
        return provider switch
        {
            IntegrationProviders.HubSpot => await RefreshHubSpotAsync(client, refreshToken, ct),
            IntegrationProviders.Salesforce => await RefreshSalesforceAsync(client, refreshToken, ct),
            IntegrationProviders.Gmail => await RefreshGoogleAsync(client, refreshToken, ct),
            IntegrationProviders.Outlook => await RefreshMicrosoftAsync(client, refreshToken, ct),
            _ => throw new InvalidOperationException($"Refresh not supported for {provider}")
        };
    }

    private async Task<(string?, string?, string?)> RefreshHubSpotAsync(HttpClient client, string refresh, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.HubSpotClientId!,
            ["client_secret"] = _options.HubSpotClientSecret!,
            ["refresh_token"] = refresh
        };
        using var res = await client.PostAsync("https://api.hubapi.com/oauth/v1/token", new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(),
            doc.RootElement.TryGetProperty("refresh_token", out var r) ? r.GetString() : refresh, null);
    }

    private async Task<(string?, string?, string?)> RefreshSalesforceAsync(HttpClient client, string refresh, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.SalesforceClientId!,
            ["client_secret"] = _options.SalesforceClientSecret!,
            ["refresh_token"] = refresh
        };
        using var res = await client.PostAsync("https://login.salesforce.com/services/oauth2/token", new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(), refresh, null);
    }

    private async Task<(string?, string?, string?)> RefreshGoogleAsync(HttpClient client, string refresh, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.GoogleClientId!,
            ["client_secret"] = _options.GoogleClientSecret!,
            ["refresh_token"] = refresh
        };
        using var res = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(), refresh, null);
    }

    private async Task<(string?, string?, string?)> RefreshMicrosoftAsync(HttpClient client, string refresh, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.MicrosoftClientId!,
            ["client_secret"] = _options.MicrosoftClientSecret!,
            ["refresh_token"] = refresh,
            ["scope"] = "https://graph.microsoft.com/Mail.Read offline_access"
        };
        var url = $"https://login.microsoftonline.com/{_options.MicrosoftTenantId}/oauth2/v2.0/token";
        using var res = await client.PostAsync(url, new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(), refresh, null);
    }
}
