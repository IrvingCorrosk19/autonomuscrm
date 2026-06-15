namespace AutonomusCRM.Application.DatabaseIntelligence;

public static class DataHealthJobStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string CompletedWithWarnings = "CompletedWithWarnings";
    public const string Failed = "Failed";
}

public static class DataHealthScanMode
{
    public const string Full = "Full";
    public const string Incremental = "Incremental";
}

public static class DataHealthStages
{
    public const string ScanningCustomers = "ScanningCustomers";
    public const string ScanningInvoices = "ScanningInvoices";
    public const string ScanningPayments = "ScanningPayments";
    public const string ScanningProducts = "ScanningProducts";
    public const string CalculatingScore = "CalculatingScore";
    public const string Completed = "Completed";
}

public static class DataHealthFindingSeverity
{
    public const string Critical = "Critical";
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";
}

public static class DataHealthFindingCategory
{
    public const string IncompleteData = "IncompleteData";
    public const string InvalidFormat = "InvalidFormat";
    public const string Duplicate = "Duplicate";
    public const string Orphan = "Orphan";
    public const string BrokenRelationship = "BrokenRelationship";
    public const string BusinessInconsistency = "BusinessInconsistency";
}

public static class DataHealthScoreBand
{
    public static string Label(int score) => score switch
    {
        >= 90 => "Excellent",
        >= 75 => "Good",
        >= 50 => "Fair",
        _ => "Critical"
    };
}

public record DataHealthProgress(
    string Stage,
    int ProgressPercent,
    string? EntityType = null,
    string? Message = null);

public record DataHealthScoreDto(
    BusinessEntityType EntityType,
    int Score,
    string Band,
    int CompletenessScore,
    int ValidityScore,
    int ConsistencyScore,
    int DuplicateScore);

public record DataHealthFindingDto(
    Guid Id,
    BusinessEntityType? EntityType,
    string Severity,
    string Category,
    string Title,
    string Explanation,
    string BusinessImpact,
    string Evidence,
    string Recommendation,
    string? SchemaName,
    string? TableName,
    int AffectedCount);

public record DataHealthJobDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    Guid SnapshotId,
    string Status,
    string ScanMode,
    string Stage,
    int ProgressPercent,
    int GlobalScore,
    string GlobalBand,
    int FindingsCount,
    int CriticalFindings,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);

public record DataHealthResultDto(
    DataHealthJobDto Job,
    IReadOnlyList<DataHealthScoreDto> Scores,
    IReadOnlyList<DataHealthFindingDto> Findings);

public record RunDataHealthRequest(
    Guid ConnectionId,
    string ScanMode = DataHealthScanMode.Full);

public sealed class DataHealthTableContext
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public BusinessEntityType EntityType { get; set; }
    public IReadOnlyList<DataHealthColumnContext> Columns { get; set; } = Array.Empty<DataHealthColumnContext>();
    public IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, string?>>();
}

public sealed class DataHealthColumnContext
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
}

public sealed class DataHealthRelationshipContext
{
    public string FromSchema { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToSchema { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
    public BusinessEntityType FromEntity { get; set; }
    public BusinessEntityType ToEntity { get; set; }
}

public sealed class DataHealthScanInput
{
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public string ScanMode { get; set; } = DataHealthScanMode.Full;
    public List<DataHealthTableContext> Tables { get; set; } = [];
    public List<DataHealthRelationshipContext> Relationships { get; set; } = [];
}

public sealed class DataHealthScanResult
{
    public List<DataHealthFindingDto> Findings { get; set; } = [];
    public List<DataHealthScoreDto> Scores { get; set; } = [];
    public int GlobalScore { get; set; }
}

public interface IDataHealthEngine
{
    DataHealthScanResult Scan(
        DataHealthScanInput input,
        IProgress<DataHealthProgress>? progress = null);
}

public interface IDataHealthService
{
    Task<DataHealthResultDto> RunHealthScanAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string scanMode,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DataHealthJobDto?> GetHealthJobAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    Task<DataHealthResultDto?> GetLatestHealthResultAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DataHealthFindingDto>> ListFindingsAsync(
        Guid tenantId, Guid? connectionId = null, string? severity = null,
        CancellationToken cancellationToken = default);
}

public interface IDbIntelligenceHealthProgressNotifier
{
    Task NotifyHealthScanStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default);
    Task NotifyHealthScanProgressAsync(Guid tenantId, Guid jobId, DataHealthProgress progress, CancellationToken cancellationToken = default);
    Task NotifyHealthScanCompletedAsync(Guid tenantId, Guid jobId, int globalScore, int findingsCount, CancellationToken cancellationToken = default);
    Task NotifyHealthScanFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default);
}

public class DataHealthJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = DataHealthJobStatus.Pending;
    public string ScanMode { get; set; } = DataHealthScanMode.Full;
    public string Stage { get; set; } = DataHealthStages.ScanningCustomers;
    public int ProgressPercent { get; set; }
    public int GlobalScore { get; set; }
    public int FindingsCount { get; set; }
    public int CriticalFindings { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class DataHealthFinding
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid HealthJobId { get; set; }
    public BusinessEntityType? EntityType { get; set; }
    public string Severity { get; set; } = DataHealthFindingSeverity.Medium;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string BusinessImpact { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? SchemaName { get; set; }
    public string? TableName { get; set; }
    public int AffectedCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DataHealthScore
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid HealthJobId { get; set; }
    public BusinessEntityType EntityType { get; set; }
    public int Score { get; set; }
    public int CompletenessScore { get; set; }
    public int ValidityScore { get; set; }
    public int ConsistencyScore { get; set; }
    public int DuplicateScore { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
