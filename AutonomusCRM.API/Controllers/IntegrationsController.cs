using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationHubService _hub;
    private readonly IIntegrationTokenRefreshService _tokenRefresh;
    private readonly ISyncConflictService _conflicts;
    private readonly IIntegrationOAuthService _oauth;
    private readonly ITenantContext _tenant;

    public IntegrationsController(
        IIntegrationHubService hub,
        IIntegrationTokenRefreshService tokenRefresh,
        ISyncConflictService conflicts,
        IIntegrationOAuthService oauth,
        ITenantContext tenant)
    {
        _hub = hub;
        _tokenRefresh = tokenRefresh;
        _conflicts = conflicts;
        _oauth = oauth;
        _tenant = tenant;
    }

    [HttpGet("oauth/status")]
    public IActionResult OAuthStatus()
    {
        var providers = new[] { IntegrationProviders.HubSpot, IntegrationProviders.Salesforce, IntegrationProviders.Gmail, IntegrationProviders.Outlook };
        return Ok(providers.Select(p => new { provider = p, configured = _oauth.IsOAuthConfigured(p) }));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _hub.ListConnectionsAsync(tenantId, cancellationToken));
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectIntegrationRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        await _hub.ConnectAsync(tenantId, request, cancellationToken);
        return Accepted();
    }

    [HttpPost("sync/{provider}")]
    public async Task<IActionResult> SyncProvider(string provider, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _hub.SyncProviderAsync(tenantId, provider, cancellationToken));
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAll(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _hub.SyncAllAsync(tenantId, cancellationToken));
    }

    [HttpPost("tokens/refresh")]
    public async Task<IActionResult> RefreshTokens(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var count = await _tokenRefresh.RefreshExpiringTokensAsync(tenantId, cancellationToken);
        return Ok(new { refreshed = count });
    }

    [HttpGet("sync/{provider}/conflicts")]
    public async Task<IActionResult> Conflicts(string provider, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _conflicts.DetectConflictsAsync(tenantId, provider, cancellationToken));
    }
}
