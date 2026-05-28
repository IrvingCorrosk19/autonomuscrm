namespace AutonomusCRM.Application.EnterpriseAI;

public interface IMlModelVersionRepository : Common.Interfaces.IRepository<MlModelVersion>
{
    Task<MlModelVersion?> GetActiveAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default);
    Task<IEnumerable<MlModelVersion>> GetByTypeAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default);
}

public interface IMlPipelineRunRepository : Common.Interfaces.IRepository<MlPipelineRun>
{
    Task<MlPipelineRun?> GetLatestAsync(Guid tenantId, string datasetType, CancellationToken cancellationToken = default);
}

public interface IMlDriftReportRepository : Common.Interfaces.IRepository<MlDriftReport>
{
    Task<IEnumerable<MlDriftReport>> GetRecentAsync(Guid tenantId, int take = 20, CancellationToken cancellationToken = default);
}

public interface IBusinessKnowledgeGraphEdgeRepository : Common.Interfaces.IRepository<BusinessKnowledgeGraphEdge>
{
    Task<IEnumerable<BusinessKnowledgeGraphEdge>> GetByTenantAsync(Guid tenantId, int take = 2000, CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface INbaOutcomeRecordRepository : Common.Interfaces.IRepository<NbaOutcomeRecord>
{
    Task<IEnumerable<NbaOutcomeRecord>> GetRecentAsync(Guid tenantId, int take = 500, CancellationToken cancellationToken = default);
}

public interface IMachineLearningPipelineService
{
    Task<IReadOnlyList<MlPipelineStatusDto>> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<MlTrainResultDto> TrainModelAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MlTrainResultDto>> TrainAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IChurnPredictionModel
{
    Task<IReadOnlyList<ChurnMlPredictionDto>> PredictAsync(Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default);
}

public interface IExpansionPredictionModel
{
    Task<IReadOnlyList<ExpansionMlPredictionDto>> PredictAsync(Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default);
}

public interface IRevenuePredictionModel
{
    Task<RevenueMlForecastDto> ForecastAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface INextBestActionMlScorer
{
    int ScoreAction(string action, string channel, string entityType, Guid tenantId);
    Task RecordOutcomeAsync(Guid tenantId, string entityType, Guid entityId, string action, string channel, bool converted, decimal impact = 0, CancellationToken cancellationToken = default);
}

public interface ISelfLearningEngine
{
    Task<SelfLearningCycleResultDto> RunLearningCycleAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IModelRegistryService
{
    Task<MlModelVersionDto?> GetActiveVersionAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MlModelVersionDto>> ListVersionsAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default);
    Task<bool> RollbackAsync(Guid tenantId, string modelType, string versionTag, CancellationToken cancellationToken = default);
    Task<MlModelVersion> RegisterTrainedModelAsync(Guid tenantId, string modelType, Dictionary<string, object> weights, Dictionary<string, object> metrics, int sampleCount, CancellationToken cancellationToken = default);
}

public interface IMlOpsFoundationService
{
    Task<IReadOnlyList<ModelDriftDto>> DetectDriftAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task MonitorModelsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IAiEvaluationFrameworkService
{
    Task<IReadOnlyList<AiEvaluationMetricsDto>> EvaluateAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<AiEvaluationMetricsDto> EvaluateModelAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default);
}

public interface IBusinessKnowledgeGraphService
{
    Task<int> RebuildGraphAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<KnowledgeGraphDto> GetGraphAsync(Guid tenantId, int maxNodes = 100, CancellationToken cancellationToken = default);
}

public interface IAutonomousOptimizationEngine
{
    Task<IReadOnlyList<OptimizationResultDto>> OptimizeTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IExecutiveAiAnalyticsService
{
    Task<ExecutiveAiAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IAiGovernanceService
{
    Task<AiGovernanceReportDto> GetGovernanceReportAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IEnterpriseAiCycleService
{
    Task RunEnterpriseAiCycleAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
