using AutonomusCRM.Application.Integrations;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class IntegrationsModel : PageModel
{
    private readonly IIntegrationHubService _hub;
    private readonly IIntegrationOAuthService _oauth;
    private readonly IIntegrationHealthService _health;
    private readonly IServiceProvider _sp;

    public IntegrationsModel(IIntegrationHubService hub, IIntegrationOAuthService oauth, IIntegrationHealthService health, IServiceProvider sp)
    {
        _hub = hub;
        _oauth = oauth;
        _health = health;
        _sp = sp;
    }

    public IReadOnlyList<TenantIntegrationConnection> Connections { get; set; } = Array.Empty<TenantIntegrationConnection>();
    public IntegrationHealthDashboardDto? HealthCenter { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public Guid TenantId { get; set; }

    public static readonly string[] Providers =
    {
        IntegrationProviders.HubSpot,
        IntegrationProviders.Salesforce,
        IntegrationProviders.Gmail,
        IntegrationProviders.Outlook,
        IntegrationProviders.Stripe
    };

    public async Task OnGetAsync(string? message = null, string? error = null)
    {
        Message = message;
        Error = error;
        TenantId = await this.GetTenantIdForPageAsync(_sp);
        Connections = await _hub.ListConnectionsAsync(TenantId);
        HealthCenter = await _health.GetDashboardAsync(TenantId);
    }

    public async Task<IActionResult> OnGetOAuthAsync(string provider)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp);
        var url = _oauth.GetAuthorizationUrl(TenantId, provider);
        if (url == null)
            return RedirectToPage(new { error = $"OAuth no configurado para {provider}. Usa conexión manual." });
        return Redirect(url);
    }

    public async Task<IActionResult> OnPostConnectAsync(string provider, string? accessToken, string? refreshToken, string? instanceUrl, string? apiKey)
    {
        try
        {
            TenantId = await this.GetTenantIdForPageAsync(_sp);
            var settings = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(apiKey)) settings["apiKey"] = apiKey;
            await _hub.ConnectAsync(TenantId, new ConnectIntegrationRequest(
                provider, accessToken, refreshToken, instanceUrl, settings.Count > 0 ? settings : null));
            return RedirectToPage(new { message = $"{provider} conectado." });
        }
        catch (Exception ex) { return RedirectToPage(new { error = ex.Message }); }
    }

    public async Task<IActionResult> OnPostSyncAsync(string provider)
    {
        try
        {
            TenantId = await this.GetTenantIdForPageAsync(_sp);
            var result = await _hub.SyncProviderAsync(TenantId, provider);
            return RedirectToPage(new { message = $"{provider}: pull={result.Pulled} push={result.Pushed} errors={result.Errors}" });
        }
        catch (Exception ex) { return RedirectToPage(new { error = ex.Message }); }
    }

    public bool IsOAuthReady(string provider) => _oauth.IsOAuthConfigured(provider);

    public TenantIntegrationConnection? GetConnection(string provider)
        => Connections.FirstOrDefault(c => c.Provider == provider);
}
