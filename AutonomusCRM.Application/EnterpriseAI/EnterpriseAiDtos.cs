namespace AutonomusCRM.Application.EnterpriseAI;

public record MlModelVersionDto(
    Guid Id,
    string ModelType,
    string VersionTag,
    string Status,
    int TrainingSampleCount,
    DateTime TrainedAt,
    Dictionary<string, object> Metrics);

public record MlTrainResultDto(
    string ModelType,
    string VersionTag,
    bool Success,
    int SampleCount,
    Dictionary<string, object> Metrics,
    string? Message);

public record ChurnMlPredictionDto(
    Guid CustomerId,
    string CustomerName,
    int ChurnProbabilityPercent,
    bool UsedMlModel,
    string ModelVersion,
    IReadOnlyList<string> Factors);

public record ExpansionMlPredictionDto(
    Guid CustomerId,
    string CustomerName,
    int ExpansionProbabilityPercent,
    string OpportunityType,
    bool UsedMlModel,
    IReadOnlyList<string> Signals);

public record RevenueMlHorizonDto(
    int HorizonDays,
    decimal PredictedRevenue,
    double ConfidencePercent,
    bool UsedMlModel);

public record RevenueMlForecastDto(
    IReadOnlyList<RevenueMlHorizonDto> Horizons,
    string ModelVersion);

public record MlPipelineStatusDto(
    string DatasetType,
    int SampleCount,
    string? ActiveModelVersion,
    DateTime? LastPipelineRun,
    string Status);

public record ModelDriftDto(
    string ModelType,
    double DriftScorePercent,
    bool AlertTriggered,
    DateTime MeasuredAt);

public record AiEvaluationMetricsDto(
    string ModelType,
    double Precision,
    double Recall,
    double F1Score,
    decimal EstimatedRoiPercent,
    decimal ChurnImpactReduction,
    decimal RevenueImpactEstimate);

public record KnowledgeGraphNodeDto(string NodeType, Guid NodeId, string Label, decimal Centrality);
public record KnowledgeGraphEdgeDto(string SourceType, Guid SourceId, string TargetType, Guid TargetId, string Relation, decimal Weight);
public record KnowledgeGraphDto(IReadOnlyList<KnowledgeGraphNodeDto> Nodes, IReadOnlyList<KnowledgeGraphEdgeDto> Edges);

public record OptimizationResultDto(
    string Area,
    int ItemsOptimized,
    string Summary);

public record ExecutiveAiAnalyticsDto(
    IReadOnlyList<MlModelVersionDto> ActiveModels,
    IReadOnlyList<AiEvaluationMetricsDto> ModelPerformance,
    IReadOnlyList<ModelDriftDto> DriftReports,
    IReadOnlyList<MlPipelineStatusDto> Pipelines,
    decimal AiRoiPercent,
    decimal ChurnReductionPercent,
    decimal RevenueLiftPercent,
    int DecisionsLast30Days,
    double AverageModelPrecision,
    KnowledgeGraphSummaryDto KnowledgeGraph);

public record KnowledgeGraphSummaryDto(int NodeCount, int EdgeCount, int CustomerNodes, int DealNodes);

public record AiGovernanceReportDto(
    IReadOnlyList<ModelAuditEntryDto> ModelAudits,
    IReadOnlyList<DecisionExplainabilityDto> RecentExplanations,
    int TotalModels,
    int ActiveModels,
    int DriftAlerts);

public record ModelAuditEntryDto(string ModelType, string VersionTag, DateTime TrainedAt, Dictionary<string, object> Metrics, string Status);
public record DecisionExplainabilityDto(Guid AuditId, string DecisionType, string Action, int Score, string Reason, Dictionary<string, object> Evidence, string? ModelVersion);

public record SelfLearningCycleResultDto(
    int OutcomesProcessed,
    int WeightsUpdated,
    int ModelsRecalibrated,
    string Summary);
