namespace AutonomusCRM.Application.EnterpriseAI;

public static class EnterpriseAiConstants
{
    public const string ModelChurn = "churn";
    public const string ModelExpansion = "expansion";
    public const string ModelRevenue = "revenue";
    public const string ModelNba = "nba";
    public const string ModelRenewal = "renewal";

    public static readonly string[] AllModelTypes = [ModelChurn, ModelExpansion, ModelRevenue, ModelNba, ModelRenewal];
    public static readonly string[] DatasetTypes = [ModelChurn, ModelExpansion, ModelRevenue, ModelRenewal, "nps", "csat", "engagement"];

    public const string ModelStatusTraining = "Training";
    public const string ModelStatusActive = "Active";
    public const string ModelStatusArchived = "Archived";

    public const string PipelineCompleted = "Completed";
    public const string PipelineFailed = "Failed";

    public const string GraphCustomer = "Customer";
    public const string GraphDeal = "Deal";
    public const string GraphChurnRisk = "ChurnRisk";
    public const string GraphExpansion = "ExpansionOpportunity";
    public const string GraphRevenue = "RevenueSignal";

    public const int MinTrainingSamples = 25;
    public const double DriftThresholdPercent = 15.0;
}
