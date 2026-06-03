namespace AutonomusCRM.Application.SemanticMemory;

public static class SemanticMemoryConstants
{
    public const string SourceObservation = "Observation";
    public const string SourceDecision = "Decision";
    public const string SourceOutcome = "Outcome";
    public const string SourceLearning = "Learning";
    public const string SourceCustomerInsight = "CustomerInsight";
    public const string SourceRevenueInsight = "RevenueInsight";
    public const string SourceEpisode = "Episode";

    public const int ConsolidationMinClusterSize = 10;
    public const int DefaultSearchTake = 20;
    public const int DefaultSimilarTake = 15;
}
