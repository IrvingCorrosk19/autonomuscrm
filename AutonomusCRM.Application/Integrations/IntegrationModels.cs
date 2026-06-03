namespace AutonomusCRM.Application.Integrations;

public static class IntegrationProviders
{
    public const string HubSpot = "HubSpot";
    public const string Salesforce = "Salesforce";
    public const string Gmail = "Gmail";
    public const string Outlook = "Outlook";
    public const string Stripe = "Stripe";
    public const string OpenAI = "OpenAI";
    public const string AzureOpenAI = "AzureOpenAI";
    public const string SendGrid = "SendGrid";
    public const string Smtp = "Smtp";
    public const string Twilio = "Twilio";
    public const string WhatsApp = "WhatsApp";
}

public class TenantIntegrationConnection
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? InstanceUrl { get; private set; }
    public Dictionary<string, string> Settings { get; private set; } = new();
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TenantIntegrationConnection() { }

    public static TenantIntegrationConnection Create(Guid tenantId, string provider)
    {
        return new TenantIntegrationConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Provider = provider,
            IsEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Configure(string? accessToken, string? refreshToken, string? instanceUrl, Dictionary<string, string>? settings)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        InstanceUrl = instanceUrl;
        if (settings != null)
            Settings = new Dictionary<string, string>(settings);
        IsEnabled = !string.IsNullOrWhiteSpace(accessToken) || Settings.Count > 0;
        MarkSync("Configured");
    }

    public void MarkSync(string status)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncStatus = status;
    }
}

public record IntegrationSyncResultDto(
    string Provider,
    int Pulled,
    int Pushed,
    int Errors,
    IReadOnlyList<string> Messages);

public record ConnectIntegrationRequest(
    string Provider,
    string? AccessToken,
    string? RefreshToken,
    string? InstanceUrl,
    Dictionary<string, string>? Settings);
