namespace AutonomusCRM.Application.Integrations;

public class IntegrationOAuthOptions
{
    public const string SectionName = "IntegrationOAuth";
    public string AppBaseUrl { get; set; } = "http://localhost:5000";
    public string? HubSpotClientId { get; set; }
    public string? HubSpotClientSecret { get; set; }
    public string? SalesforceClientId { get; set; }
    public string? SalesforceClientSecret { get; set; }
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }
    public string? MicrosoftClientId { get; set; }
    public string? MicrosoftClientSecret { get; set; }
    public string? MicrosoftTenantId { get; set; } = "common";
}
