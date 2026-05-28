namespace AutonomusCRM.Application.CustomerSuccess;

public static class CustomerSuccessConstants
{
    public const string HealthHealthy = "Healthy";
    public const string HealthWarning = "Warning";
    public const string HealthCritical = "Critical";

    public const string PlaybookOnboarding = "Onboarding";
    public const string PlaybookAdoption = "Adoption";
    public const string PlaybookRescue = "Rescue";
    public const string PlaybookRenewal = "Renewal";
    public const string PlaybookExpansion = "Expansion";
    public const string PlaybookReEngagement = "ReEngagement";

    public const string TaskChurnAlert = "ChurnRisk_Alert";
    public const string TaskRenewal90 = "Renewal_90d";
    public const string TaskRenewal60 = "Renewal_60d";
    public const string TaskRenewal30 = "Renewal_30d";
    public const string TaskExpansion = "Expansion_Opportunity";
    public const string TaskHealthRescue = "Health_Rescue";
    public const string TaskReEngagement = "ReEngagement";

    public const string ChannelEmail = "Email";
    public const string ChannelWhatsApp = "WhatsApp";

    public const string ContractActive = "Active";
    public const string ContractPendingRenewal = "PendingRenewal";
    public const string ContractChurned = "Churned";
}
