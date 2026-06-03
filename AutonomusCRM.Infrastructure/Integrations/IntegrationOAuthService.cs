using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class IntegrationOAuthService : IIntegrationOAuthService
{
    private readonly IntegrationOAuthOptions _options;
    private readonly IntegrationEndpointsOptions _endpoints;
    private readonly IIntegrationHubService _hub;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<IntegrationOAuthService> _logger;

    public IntegrationOAuthService(
        IOptions<IntegrationOAuthOptions> options,
        IOptions<IntegrationEndpointsOptions> endpoints,
        IIntegrationHubService hub,
        IHttpClientFactory http,
        ILogger<IntegrationOAuthService> logger)
    {
        _options = options.Value;
        _endpoints = endpoints.Value;
        _hub = hub;
        _http = http;
        _logger = logger;
    }

    public bool IsOAuthConfigured(string provider) => provider switch
    {
        IntegrationProviders.HubSpot => !string.IsNullOrWhiteSpace(_options.HubSpotClientId),
        IntegrationProviders.Salesforce => !string.IsNullOrWhiteSpace(_options.SalesforceClientId),
        IntegrationProviders.Gmail => !string.IsNullOrWhiteSpace(_options.GoogleClientId),
        IntegrationProviders.Outlook => !string.IsNullOrWhiteSpace(_options.MicrosoftClientId),
        _ => false
    };

    public string? GetAuthorizationUrl(Guid tenantId, string provider)
    {
        var redirect = Uri.EscapeDataString($"{_options.AppBaseUrl.TrimEnd('/')}/Integrations/OAuthCallback?provider={provider}&tenantId={tenantId}");
        return provider switch
        {
            IntegrationProviders.HubSpot when IsOAuthConfigured(provider) =>
                $"{_endpoints.HubSpotOAuthAuthorize}?client_id={_options.HubSpotClientId}&redirect_uri={redirect}&scope=crm.objects.contacts.read%20crm.objects.contacts.write",
            IntegrationProviders.Salesforce when IsOAuthConfigured(provider) =>
                $"{_endpoints.SalesforceOAuthAuthorize}?response_type=code&client_id={_options.SalesforceClientId}&redirect_uri={redirect}",
            IntegrationProviders.Gmail when IsOAuthConfigured(provider) =>
                $"{_endpoints.GoogleOAuthAuthorize}?client_id={_options.GoogleClientId}&redirect_uri={redirect}&response_type=code&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/gmail.readonly")}&access_type=offline&prompt=consent",
            IntegrationProviders.Outlook when IsOAuthConfigured(provider) =>
                $"https://login.microsoftonline.com/{_options.MicrosoftTenantId}/oauth2/v2.0/authorize?client_id={_options.MicrosoftClientId}&response_type=code&redirect_uri={redirect}&scope={Uri.EscapeDataString("https://graph.microsoft.com/Mail.Read offline_access")}",
            _ => null
        };
    }

    public async Task<OAuthCallbackResult> HandleCallbackAsync(
        Guid tenantId, string provider, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var redirect = $"{_options.AppBaseUrl.TrimEnd('/')}/Integrations/OAuthCallback?provider={provider}&tenantId={tenantId}";
            var (access, refresh, instance) = await ExchangeTokenAsync(provider, code, redirect, cancellationToken);
            await _hub.ConnectAsync(tenantId, new ConnectIntegrationRequest(provider, access, refresh, instance, null), cancellationToken);
            return new OAuthCallbackResult(true, null, provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed {Provider}", provider);
            return new OAuthCallbackResult(false, ex.Message, provider);
        }
    }

    private async Task<(string? access, string? refresh, string? instance)> ExchangeTokenAsync(
        string provider, string code, string redirectUri, CancellationToken cancellationToken)
    {
        var client = _http.CreateClient("OAuth");
        return provider switch
        {
            IntegrationProviders.HubSpot => await ExchangeHubSpotAsync(client, code, redirectUri, cancellationToken),
            IntegrationProviders.Salesforce => await ExchangeSalesforceAsync(client, code, redirectUri, cancellationToken),
            IntegrationProviders.Gmail => await ExchangeGoogleAsync(client, code, redirectUri, cancellationToken),
            IntegrationProviders.Outlook => await ExchangeMicrosoftAsync(client, code, redirectUri, cancellationToken),
            _ => throw new InvalidOperationException($"OAuth not supported for {provider}")
        };
    }

    private async Task<(string?, string?, string?)> ExchangeHubSpotAsync(HttpClient client, string code, string redirect, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.HubSpotClientId!,
            ["client_secret"] = _options.HubSpotClientSecret!,
            ["redirect_uri"] = redirect,
            ["code"] = code
        };
        using var res = await client.PostAsync(_endpoints.HubSpotOAuthToken, new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(),
            doc.RootElement.TryGetProperty("refresh_token", out var r) ? r.GetString() : null, null);
    }

    private async Task<(string?, string?, string?)> ExchangeSalesforceAsync(HttpClient client, string code, string redirect, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.SalesforceClientId!,
            ["client_secret"] = _options.SalesforceClientSecret!,
            ["redirect_uri"] = redirect,
            ["code"] = code
        };
        using var res = await client.PostAsync(_endpoints.SalesforceOAuthToken, new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(),
            doc.RootElement.TryGetProperty("refresh_token", out var r) ? r.GetString() : null,
            doc.RootElement.GetProperty("instance_url").GetString());
    }

    private async Task<(string?, string?, string?)> ExchangeGoogleAsync(HttpClient client, string code, string redirect, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.GoogleClientId!,
            ["client_secret"] = _options.GoogleClientSecret!,
            ["redirect_uri"] = redirect,
            ["code"] = code
        };
        using var res = await client.PostAsync(_endpoints.GoogleOAuthToken, new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(),
            doc.RootElement.TryGetProperty("refresh_token", out var r) ? r.GetString() : null, null);
    }

    private async Task<(string?, string?, string?)> ExchangeMicrosoftAsync(HttpClient client, string code, string redirect, CancellationToken ct)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.MicrosoftClientId!,
            ["client_secret"] = _options.MicrosoftClientSecret!,
            ["redirect_uri"] = redirect,
            ["code"] = code,
            ["scope"] = "https://graph.microsoft.com/Mail.Read offline_access"
        };
        var url = $"{_endpoints.MicrosoftOAuthTokenBase.TrimEnd('/')}/{_options.MicrosoftTenantId}/oauth2/v2.0/token";
        using var res = await client.PostAsync(url, new FormUrlEncodedContent(body), ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString(),
            doc.RootElement.TryGetProperty("refresh_token", out var r) ? r.GetString() : null, null);
    }
}
