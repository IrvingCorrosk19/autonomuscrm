namespace AutonomusCRM.Application.DatabaseIntelligence;

public static class DbIntelligenceInsightJobStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class DbIntelligenceInsightStages
{
    public const string AnalyzingCatalog = "AnalyzingCatalog";
    public const string EvaluatingHealth = "EvaluatingHealth";
    public const string GeneratingInsights = "GeneratingInsights";
    public const string EnrichingSemantic = "EnrichingSemantic";
    public const string Completed = "Completed";
}

public static class DbIntelligenceInsightType
{
    public const string CriticalTable = "CriticalTable";
    public const string UnusedData = "UnusedData";
    public const string MigrationOpportunity = "MigrationOpportunity";
    public const string QualityRisk = "QualityRisk";
    public const string UnmappedEntity = "UnmappedEntity";
}

public static class DbIntelligenceInsightCategory
{
    public const string Risk = "Risk";
    public const string Opportunity = "Opportunity";
    public const string Recommendation = "Recommendation";
}

public static class DbIntelligenceSemanticSourceType
{
    public const string DipInsight = "DipDatabaseInsight";
}

public record DbIntelligenceInsightProgress(
    string Stage,
    int ProgressPercent,
    string? Message = null);

public record DbIntelligenceInsightDto(
    Guid Id,
    string Type,
    string Category,
    string Title,
    string Summary,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ExplainabilityReasons,
    string SuggestedAction,
    int ImpactScore,
    int EffortScore,
    int ConfidencePercent,
    int SemanticMatchScore,
    int PriorityScore,
    BusinessEntityType? EntityType,
    string? SchemaName,
    string? TableName,
    DateTime CreatedAtUtc);

public record DbIntelligenceInsightJobDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    Guid SnapshotId,
    string Status,
    string Stage,
    int ProgressPercent,
    int InsightCount,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);

public record DbIntelligenceInsightResultDto(
    DbIntelligenceInsightJobDto Job,
    IReadOnlyList<DbIntelligenceInsightDto> Insights);

public record GenerateDbIntelligenceInsightsRequest(
    bool IncludeSemanticEnrichment = true);

public sealed class DbIntelligenceCatalogTableContext
{
    public string SchemaName { get; set; } = "public";
    public string TableName { get; set; } = string.Empty;
    public long EstimatedRowCount { get; set; }
    public int IncomingFkCount { get; set; }
    public int OutgoingFkCount { get; set; }
    public int NullableColumnCount { get; set; }
    public int TotalColumnCount { get; set; }
    public bool HasUpdatedAtColumn { get; set; }
    public bool IsMapped { get; set; }
}

public sealed class DbIntelligenceUnmappedTableContext
{
    public string SchemaName { get; set; } = "public";
    public string TableName { get; set; } = string.Empty;
    public BusinessEntityType? InferredEntityType { get; set; }
    public int ConfidencePercent { get; set; }
    public string Status { get; set; } = DbBusinessMappingStatus.Inferred;
    public long EstimatedRowCount { get; set; }
    public List<string> InferenceReasons { get; set; } = [];
}

public sealed class DbIntelligenceInsightBuildInput
{
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public List<DbBusinessGraphMappingContext> ConfirmedMappings { get; set; } = [];
    public List<DbIntelligenceUnmappedTableContext> UnmappedTables { get; set; } = [];
    public List<DbIntelligenceCatalogTableContext> CatalogTables { get; set; } = [];
    public List<DbBusinessGraphRelationshipContext> Relationships { get; set; } = [];
    public List<DataHealthScoreDto> HealthScores { get; set; } = [];
    public List<DataHealthFindingDto> HealthFindings { get; set; } = [];
    public int GlobalHealthScore { get; set; } = 100;
}

public interface IDbIntelligenceInsightEngine
{
    IReadOnlyList<DbIntelligenceInsightDto> Generate(
        DbIntelligenceInsightBuildInput input,
        IProgress<DbIntelligenceInsightProgress>? progress = null);
}

public interface IDbIntelligenceInsightService
{
    Task<DbIntelligenceInsightResultDto> GenerateInsightsAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        GenerateDbIntelligenceInsightsRequest? request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DbIntelligenceInsightResultDto?> GetLatestInsightsAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<DbIntelligenceInsightJobDto?> GetInsightJobAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbIntelligenceInsightDto>> ListInsightsAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);
}

public class DbIntelligenceInsightJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = DbIntelligenceInsightJobStatus.Pending;
    public string Stage { get; set; } = DbIntelligenceInsightStages.AnalyzingCatalog;
    public int ProgressPercent { get; set; }
    public int InsightCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class DbIntelligenceInsight
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public string ExplainabilityJson { get; set; } = "[]";
    public string SuggestedAction { get; set; } = string.Empty;
    public int ImpactScore { get; set; }
    public int EffortScore { get; set; }
    public int ConfidencePercent { get; set; }
    public int SemanticMatchScore { get; set; }
    public int PriorityScore { get; set; }
    public BusinessEntityType? EntityType { get; set; }
    public string? SchemaName { get; set; }
    public string? TableName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
