namespace AutonomusCRM.Application.Integrations;

public interface IIntegrationOAuthService
{
    bool IsOAuthConfigured(string provider);
    string? GetAuthorizationUrl(Guid tenantId, string provider);
    Task<OAuthCallbackResult> HandleCallbackAsync(Guid tenantId, string provider, string code, CancellationToken cancellationToken = default);
}

public record OAuthCallbackResult(bool Success, string? Error, string Provider);
