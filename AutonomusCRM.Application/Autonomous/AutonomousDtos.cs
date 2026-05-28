namespace AutonomusCRM.Application.Autonomous;

public record AutonomousDecisionDto(
    Guid DecisionId,
    string DecisionType,
    string Action,
    int Score,
    string Reason,
    Dictionary<string, object> Evidence,
    Guid? CustomerId,
    Guid? DealId);

public record NextBestActionDto(
    string EntityType,
    Guid EntityId,
    string EntityName,
    string RecommendedAction,
    string Channel,
    DateTime? DueAt,
    int PriorityScore,
    string Rationale);

public record PredictiveHorizonDto(
    int HorizonDays,
    decimal PredictedRevenue,
    decimal PredictedRenewals,
    int PredictedChurnCount,
    decimal PredictedExpansion);

public record PredictiveRevenueForecastDto(
    IReadOnlyList<PredictiveHorizonDto> Horizons,
    double ConfidencePercent);

public record PlaybookProgressDto(
    Guid StateId,
    Guid CustomerId,
    string PlaybookType,
    string Status,
    int CurrentStep,
    int TotalSteps,
    DateTime? NextActionAt);

public record AgentRunResultDto(
    string AgentName,
    int DecisionsMade,
    int ActionsExecuted,
    int TasksCreated);

public record MlDatasetSummaryDto(
    string DatasetType,
    int SampleCount,
    DateTime? LatestCapture);

public record KnowledgeInsightDto(
    string PatternKey,
    decimal SuccessRate,
    int Occurrences,
    string Recommendation);

public record ExecutiveAiDashboardDto(
    IReadOnlyList<AutonomousDecisionDto> RecentDecisions,
    PredictiveRevenueForecastDto Predictions,
    IReadOnlyList<NextBestActionDto> NextBestActions,
    IReadOnlyList<AgentRunResultDto> AgentActivity,
    IReadOnlyList<KnowledgeInsightDto> TopKnowledge,
    int PendingDecisions,
    int ExecutedToday,
    int AtRiskCustomers,
    int ExpansionReady);
