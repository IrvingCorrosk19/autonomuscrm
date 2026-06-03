namespace AutonomusCRM.Application.CustomerSuccess;

public class CommunicationOptions
{
    public const string SectionName = "Communications";

    /// <summary>Log | Smtp | SendGrid | Ses</summary>
    public string EmailProvider { get; set; } = "Log";

    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SendGridApiKey { get; set; }
    public string? SesAccessKey { get; set; }
    public string? SesSecretKey { get; set; }
    public string? SesRegion { get; set; } = "us-east-1";
    public string FromAddress { get; set; } = "noreply@autonomusflow.local";
    public string FromName { get; set; } = "AutonomusFlow";

    /// <summary>Log | WhatsAppBusiness</summary>
    public string WhatsAppProvider { get; set; } = "Log";
    public string? WhatsAppAccessToken { get; set; }
    public string? WhatsAppPhoneNumberId { get; set; }
    public string? WhatsAppApiVersion { get; set; } = "v21.0";

    /// <summary>When false, production blocks Log/Simulated providers.</summary>
    public bool AllowSimulation { get; set; } = true;
}
