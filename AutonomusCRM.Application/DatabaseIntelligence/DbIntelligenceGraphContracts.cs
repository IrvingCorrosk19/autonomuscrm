namespace AutonomusCRM.Application.DatabaseIntelligence;

public static class DbBusinessGraphJobStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class DbBusinessGraphStages
{
    public const string BuildingGraph = "BuildingGraph";
    public const string CreatingNodes = "CreatingNodes";
    public const string CreatingRelationships = "CreatingRelationships";
    public const string CalculatingMetrics = "CalculatingMetrics";
    public const string Completed = "Completed";
}

public static class DbBusinessGraphEdgeTypes
{
    public const string HasContacts = "HasContacts";
    public const string GeneratedSale = "GeneratedSale";
    public const string GeneratedInvoice = "GeneratedInvoice";
    public const string GeneratedPayment = "GeneratedPayment";
    public const string PurchasedProduct = "PurchasedProduct";
    public const string HasActivity = "HasActivity";
}

public static class DbBusinessGraphExportFormat
{
    public const string Png = "png";
    public const string Pdf = "pdf";
    public const string Snapshot = "snapshot";
}

public static class DbBusinessGraphNodeTypes
{
    public const string DipBusinessEntity = "DipBusinessEntity";
}

public record DbBusinessGraphProgress(
    string Stage,
    int ProgressPercent,
    string? Message = null);

public record DbBusinessGraphSourceDto(
    Guid MappingId,
    string BusinessName,
    int ConfidencePercent,
    string? SchemaName = null,
    string? TableName = null);

public record DbBusinessGraphNodeDto(
    Guid Id,
    BusinessEntityType EntityType,
    string Label,
    int ConfidencePercent,
    IReadOnlyList<DbBusinessGraphSourceDto> Sources,
    int HealthScore,
    string HealthBand,
    string RiskLevel,
    long RecordCount,
    int RelationshipCount,
    int DuplicateCount,
    int OrphanCount,
    IReadOnlyList<DataHealthFindingDto> TopFindings);

public record DbBusinessGraphEdgeDto(
    Guid Id,
    Guid FromNodeId,
    Guid ToNodeId,
    BusinessEntityType FromEntityType,
    BusinessEntityType ToEntityType,
    string EdgeType,
    string BusinessLabel,
    int ConfidencePercent,
    int RelationshipCount);

public record DbBusinessGraphSummaryDto(
    Guid ConnectionProfileId,
    Guid SnapshotId,
    int NodeCount,
    int EdgeCount,
    int GlobalHealthScore,
    string GlobalHealthBand,
    int TotalRecords,
    int TotalDuplicates,
    int TotalOrphans,
    int CriticalFindings,
    string BusinessViewMessage);

public record DbBusinessGraphDto(
    Guid ConnectionProfileId,
    Guid SnapshotId,
    Guid? GraphJobId,
    IReadOnlyList<DbBusinessGraphNodeDto> Nodes,
    IReadOnlyList<DbBusinessGraphEdgeDto> Edges,
    DbBusinessGraphSummaryDto Summary,
    DateTime GeneratedAtUtc);

public record DbBusinessGraphJobDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    Guid SnapshotId,
    string Status,
    string Stage,
    int ProgressPercent,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);

public record BuildDbBusinessGraphRequest(
    bool IncludeProducts = true,
    bool IncludeActivities = true);

public record DbBusinessGraphExportResultDto(
    string Format,
    string ContentType,
    string FileName,
    byte[]? Content,
    string? SnapshotJson);

public sealed class DbBusinessGraphMappingContext
{
    public Guid MappingId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public BusinessEntityType EntityType { get; set; }
    public int ConfidencePercent { get; set; }
    public string Status { get; set; } = DbBusinessMappingStatus.Inferred;
    public long EstimatedRowCount { get; set; }
}

public sealed class DbBusinessGraphRelationshipContext
{
    public string FromSchema { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToSchema { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
    public int ConfidencePercent { get; set; } = 100;
}

public sealed class DbBusinessGraphBuildInput
{
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public bool IncludeProducts { get; set; } = true;
    public bool IncludeActivities { get; set; } = true;
    public List<DbBusinessGraphMappingContext> Mappings { get; set; } = [];
    public List<DbBusinessGraphRelationshipContext> Relationships { get; set; } = [];
    public List<DataHealthScoreDto> HealthScores { get; set; } = [];
    public List<DataHealthFindingDto> HealthFindings { get; set; } = [];
}

public interface IDbBusinessGraphBuilder
{
    DbBusinessGraphDto Build(
        DbBusinessGraphBuildInput input,
        IProgress<DbBusinessGraphProgress>? progress = null);
}

public interface IDbBusinessGraphService
{
    Task<DbBusinessGraphResultDto> BuildGraphAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        BuildDbBusinessGraphRequest? request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DbBusinessGraphDto?> GetGraphAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbBusinessGraphNodeDto>> GetNodesAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbBusinessGraphEdgeDto>> GetEdgesAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<DbBusinessGraphSummaryDto?> GetSummaryAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<DbBusinessGraphJobDto?> GetGraphJobAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<DbBusinessGraphExportResultDto> ExportGraphAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string format,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}

public record DbBusinessGraphResultDto(
    DbBusinessGraphJobDto Job,
    DbBusinessGraphDto Graph);

public interface IDbIntelligenceGraphProgressNotifier
{
    Task NotifyGraphBuildStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default);
    Task NotifyGraphBuildProgressAsync(Guid tenantId, Guid jobId, DbBusinessGraphProgress progress, CancellationToken cancellationToken = default);
    Task NotifyGraphBuildCompletedAsync(Guid tenantId, Guid jobId, int nodeCount, int edgeCount, CancellationToken cancellationToken = default);
    Task NotifyGraphBuildFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default);
}

public class DbBusinessGraphJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = DbBusinessGraphJobStatus.Pending;
    public string Stage { get; set; } = DbBusinessGraphStages.BuildingGraph;
    public int ProgressPercent { get; set; }
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class DbBusinessGraphSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid GraphJobId { get; set; }
    public string GraphJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
