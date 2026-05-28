namespace AutonomusCRM.Application.Intelligence;

public static class IntelligenceConstants
{
    public const string SegmentVip = "VIP";
    public const string SegmentGrowth = "Growth";
    public const string SegmentStable = "Stable";
    public const string SegmentAtRisk = "AtRisk";
    public const string SegmentChurned = "Churned";

    public const string FeedbackNps = "NPS";
    public const string FeedbackCsat = "CSAT";
    public const string FeedbackComment = "Comment";

    public const string NpsPromoter = "Promoter";
    public const string NpsPassive = "Passive";
    public const string NpsDetractor = "Detractor";

    public const string UsageLogin = "login";
    public const string UsageSession = "session";
    public const string UsageFeature = "feature";

    public const string TaskInsight = "Intel_CustomerInsight";
    public const string TaskAnomaly = "Intel_Anomaly";
    public const string TaskRecommendation = "Intel_Recommendation";

    public static readonly string[] CrmModules =
        ["Leads", "Deals", "Customers", "Tasks", "Workflows", "Revenue", "Reports"];
}
