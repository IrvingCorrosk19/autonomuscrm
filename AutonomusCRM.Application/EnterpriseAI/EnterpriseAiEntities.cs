using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.EnterpriseAI;

public class MlModelVersion : Entity
{
    public Guid TenantId { get; private set; }
    public string ModelType { get; private set; }
    public string VersionTag { get; private set; }
    public string Status { get; private set; }
    public Dictionary<string, object> Weights { get; private set; }
    public Dictionary<string, object> Metrics { get; private set; }
    public int TrainingSampleCount { get; private set; }
    public DateTime TrainedAt { get; private set; }
    public string? Notes { get; private set; }

    private MlModelVersion() : base()
    {
        ModelType = string.Empty;
        VersionTag = string.Empty;
        Status = EnterpriseAiConstants.ModelStatusTraining;
        Weights = new Dictionary<string, object>();
        Metrics = new Dictionary<string, object>();
    }

    public static MlModelVersion Create(
        Guid tenantId, string modelType, string versionTag,
        Dictionary<string, object> weights, Dictionary<string, object> metrics,
        int sampleCount, string? notes = null)
    {
        return new MlModelVersion
        {
            TenantId = tenantId,
            ModelType = modelType,
            VersionTag = versionTag,
            Status = EnterpriseAiConstants.ModelStatusTraining,
            Weights = weights,
            Metrics = metrics,
            TrainingSampleCount = sampleCount,
            TrainedAt = DateTime.UtcNow,
            Notes = notes
        };
    }

    public void Activate()
    {
        Status = EnterpriseAiConstants.ModelStatusActive;
        MarkAsUpdated();
    }

    public void Archive()
    {
        Status = EnterpriseAiConstants.ModelStatusArchived;
        MarkAsUpdated();
    }
}

public class MlPipelineRun : Entity
{
    public Guid TenantId { get; private set; }
    public string DatasetType { get; private set; }
    public string Status { get; private set; }
    public int SamplesProcessed { get; private set; }
    public string? ModelVersionTag { get; private set; }
    public Dictionary<string, object> RunMetrics { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private MlPipelineRun() : base()
    {
        DatasetType = string.Empty;
        Status = EnterpriseAiConstants.PipelineCompleted;
        RunMetrics = new Dictionary<string, object>();
    }

    public static MlPipelineRun Start(Guid tenantId, string datasetType)
    {
        return new MlPipelineRun
        {
            TenantId = tenantId,
            DatasetType = datasetType,
            Status = "Running",
            StartedAt = DateTime.UtcNow
        };
    }

    public void Complete(int samples, string? versionTag, Dictionary<string, object>? metrics = null)
    {
        SamplesProcessed = samples;
        ModelVersionTag = versionTag;
        Status = EnterpriseAiConstants.PipelineCompleted;
        RunMetrics = metrics ?? new Dictionary<string, object>();
        CompletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Fail(string reason)
    {
        Status = EnterpriseAiConstants.PipelineFailed;
        RunMetrics = new Dictionary<string, object> { ["error"] = reason };
        CompletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}

public class MlDriftReport : Entity
{
    public Guid TenantId { get; private set; }
    public string ModelType { get; private set; }
    public double DriftScorePercent { get; private set; }
    public bool AlertTriggered { get; private set; }
    public Dictionary<string, object> Details { get; private set; }
    public DateTime MeasuredAt { get; private set; }

    private MlDriftReport() : base()
    {
        ModelType = string.Empty;
        Details = new Dictionary<string, object>();
    }

    public static MlDriftReport Capture(Guid tenantId, string modelType, double driftPercent, Dictionary<string, object>? details = null)
    {
        return new MlDriftReport
        {
            TenantId = tenantId,
            ModelType = modelType,
            DriftScorePercent = driftPercent,
            AlertTriggered = driftPercent >= EnterpriseAiConstants.DriftThresholdPercent,
            Details = details ?? new Dictionary<string, object>(),
            MeasuredAt = DateTime.UtcNow
        };
    }
}

public class BusinessKnowledgeGraphEdge : Entity
{
    public Guid TenantId { get; private set; }
    public string SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public string TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public string RelationType { get; private set; }
    public decimal Weight { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private BusinessKnowledgeGraphEdge() : base()
    {
        SourceType = string.Empty;
        TargetType = string.Empty;
        RelationType = string.Empty;
        Metadata = new Dictionary<string, object>();
    }

    public static BusinessKnowledgeGraphEdge Link(
        Guid tenantId, string sourceType, Guid sourceId,
        string targetType, Guid targetId, string relation, decimal weight = 1m,
        Dictionary<string, object>? metadata = null)
    {
        return new BusinessKnowledgeGraphEdge
        {
            TenantId = tenantId,
            SourceType = sourceType,
            SourceId = sourceId,
            TargetType = targetType,
            TargetId = targetId,
            RelationType = relation,
            Weight = weight,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}

public class NbaOutcomeRecord : Entity
{
    public Guid TenantId { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string RecommendedAction { get; private set; }
    public string Channel { get; private set; }
    public bool Converted { get; private set; }
    public decimal ImpactScore { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private NbaOutcomeRecord() : base()
    {
        EntityType = string.Empty;
        RecommendedAction = string.Empty;
        Channel = string.Empty;
    }

    public static NbaOutcomeRecord FromAction(
        Guid tenantId, string entityType, Guid entityId,
        string action, string channel, bool converted, decimal impact = 0)
    {
        return new NbaOutcomeRecord
        {
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            RecommendedAction = action,
            Channel = channel,
            Converted = converted,
            ImpactScore = impact,
            RecordedAt = DateTime.UtcNow
        };
    }
}
