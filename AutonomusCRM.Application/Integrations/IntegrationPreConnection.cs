namespace AutonomusCRM.Application.Integrations;

public static class IntegrationHealthStates
{
    public const string Connected = "Connected";
    public const string Disconnected = "Disconnected";
    public const string Expired = "Expired";
    public const string Pending = "Pending";
    public const string Misconfigured = "Misconfigured";
    public const string RateLimited = "RateLimited";
    public const string Error = "Error";
    public const string Blocked = "Blocked";
}

public static class IntegrationProviderCatalog
{
    public static readonly string[] All =
    [
        IntegrationProviders.OpenAI,
        IntegrationProviders.AzureOpenAI,
        IntegrationProviders.SendGrid,
        IntegrationProviders.Smtp,
        IntegrationProviders.Twilio,
        IntegrationProviders.WhatsApp,
        IntegrationProviders.Stripe,
        IntegrationProviders.HubSpot,
        IntegrationProviders.Salesforce,
        IntegrationProviders.Gmail,
        IntegrationProviders.Outlook
    ];

    public static bool IsTenantScoped(string provider) =>
        provider is IntegrationProviders.HubSpot or IntegrationProviders.Salesforce
            or IntegrationProviders.Gmail or IntegrationProviders.Outlook
            or IntegrationProviders.Stripe or IntegrationProviders.SendGrid
            or IntegrationProviders.Twilio or IntegrationProviders.WhatsApp
            or IntegrationProviders.OpenAI or IntegrationProviders.AzureOpenAI
            or IntegrationProviders.Smtp;
}

public class IntegrationEndpointsOptions
{
    public const string SectionName = "IntegrationEndpoints";

    public string OpenAiEmbeddingsUrl { get; set; } = "https://api.openai.com/v1/embeddings";
    public string SendGridMailUrl { get; set; } = "https://api.sendgrid.com/v3/mail/send";
    public string HubSpotApiBase { get; set; } = "https://api.hubapi.com";
    public string HubSpotOAuthAuthorize { get; set; } = "https://app.hubspot.com/oauth/authorize";
    public string HubSpotOAuthToken { get; set; } = "https://api.hubapi.com/oauth/v1/token";
    public string SalesforceOAuthAuthorize { get; set; } = "https://login.salesforce.com/services/oauth2/authorize";
    public string SalesforceOAuthToken { get; set; } = "https://login.salesforce.com/services/oauth2/token";
    public string StripeApiBase { get; set; } = "https://api.stripe.com/v1";
    public string GoogleOAuthToken { get; set; } = "https://oauth2.googleapis.com/token";
    public string GoogleOAuthAuthorize { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";
    public string MicrosoftOAuthTokenBase { get; set; } = "https://login.microsoftonline.com";
    public string WhatsAppGraphBase { get; set; } = "https://graph.facebook.com";
}

public class TwilioOptions
{
    public const string SectionName = "Twilio";
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? FromNumber { get; set; }
    public string? WebhookBaseUrl { get; set; }
}

public record IntegrationHealthItemDto(
    string Provider,
    string Status,
    bool IsConfigured,
    bool IsTenantOverride,
    bool CredentialsPresent,
    string ConfigurationSource,
    string? LastSyncStatus,
    DateTime? LastSyncAt,
    string? Detail,
    IReadOnlyList<string> RequiredVariables);

public record IntegrationHealthDashboardDto(
    Guid TenantId,
    IReadOnlyList<IntegrationHealthItemDto> Providers,
    bool SecretEncryptionConfigured,
    string SecretEncryptionBadge);

public record SmokeTestResultDto(
    string Provider,
    string Status,
    string Message,
    bool RequiresCredentials,
    IReadOnlyList<string> RequiredVariables);

public interface IIntegrationHealthService
{
    Task<IntegrationHealthDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IIntegrationSmokeTestService
{
    IReadOnlyList<string> GetSupportedProviders();
    Task<SmokeTestResultDto> RunAsync(string provider, Guid? tenantId = null, CancellationToken cancellationToken = default);
}

public interface ISecretMaskingService
{
    string Mask(string? secret);
    string MaskConnection(TenantIntegrationConnection connection);
}

public interface IIntegrationWebhookAuditor
{
    void LogReceived(string provider, string eventType, Guid? tenantId, bool signatureValid, string? detail = null);
}
