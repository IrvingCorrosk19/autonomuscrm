namespace AutonomusCRM.Application.DatabaseIntelligence;

public static class DbOperationJobStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Preview = "Preview";
    public const string Completed = "Completed";
    public const string CompletedWithWarnings = "CompletedWithWarnings";
    public const string Failed = "Failed";
    public const string RolledBack = "RolledBack";
}

public static class DbOperationStages
{
    public const string Analyzing = "Analyzing";
    public const string Preparing = "Preparing";
    public const string Validating = "Validating";
    public const string Transforming = "Transforming";
    public const string Importing = "Importing";
    public const string Completed = "Completed";
}

public static class DbOperationActionType
{
    public const string Filter = "Filter";
    public const string Clean = "Clean";
    public const string Merge = "Merge";
    public const string Enrich = "Enrich";
    public const string Exclude = "Exclude";
    public const string Transform = "Transform";
    public const string Sync = "Sync";
    public const string Import = "Import";
}

public static class DbOperationFilterOperator
{
    public const string Equals = "Equals";
    public const string GreaterThan = "GreaterThan";
    public const string Between = "Between";
    public const string Contains = "Contains";
    public const string IsEmpty = "IsEmpty";
}

public static class DbOperationCleanAction
{
    public const string Trim = "Trim";
    public const string Lowercase = "Lowercase";
    public const string Uppercase = "Uppercase";
    public const string NormalizePhone = "NormalizePhone";
    public const string NormalizeEmail = "NormalizeEmail";
    public const string RemoveSpaces = "RemoveSpaces";
}

public static class DbOperationMergeStrategy
{
    public const string KeepMaster = "KeepMaster";
    public const string KeepNewest = "KeepNewest";
    public const string KeepOldest = "KeepOldest";
}

public static class DbOperationTransformType
{
    public const string SplitFullName = "SplitFullName";
    public const string CombineColumns = "CombineColumns";
    public const string RenameField = "RenameField";
    public const string MapCategory = "MapCategory";
}

public static class DbOperationRowStatus
{
    public const string Active = "Active";
    public const string Filtered = "Filtered";
    public const string Excluded = "Excluded";
    public const string Merged = "Merged";
    public const string Imported = "Imported";
    public const string Error = "Error";
}

public record DbOperationProgress(string Stage, int ProgressPercent, string? Message = null);

public record DbOperationFilterRule(string Field, string Operator, string? Value, string? ValueTo = null);

public record DbOperationCleanRule(string Field, string Action);

public record DbOperationMergeRule(BusinessEntityType EntityType, string MatchField, string Strategy);

public record DbOperationEnrichRule(string Field, string Value);

public record DbOperationExcludeRule(string Reason, string? Field = null, string? Operator = null, string? Value = null);

public record DbOperationTransformRule(
    string TransformType, string SourceField, string? TargetField = null,
    string? SecondField = null, string? Separator = null, Dictionary<string, string>? CategoryMap = null);

public record DbOperationActionPlan(
    bool Filter,
    bool Clean,
    bool Merge,
    bool Enrich,
    bool Exclude,
    bool Transform,
    bool Sync,
    bool Import,
    IReadOnlyList<DbOperationFilterRule> FilterRules,
    IReadOnlyList<DbOperationCleanRule> CleanRules,
    IReadOnlyList<DbOperationMergeRule> MergeRules,
    IReadOnlyList<DbOperationEnrichRule> EnrichRules,
    IReadOnlyList<DbOperationExcludeRule> ExcludeRules,
    IReadOnlyList<DbOperationTransformRule> TransformRules,
    string ConflictPolicy = DbSyncConflictPolicy.SourceWins);

public record DbOperationRowPreview(
    int RowNumber,
    BusinessEntityType EntityType,
    IReadOnlyDictionary<string, string?> Before,
    IReadOnlyDictionary<string, string?> After,
    string? Impact,
    bool Excluded);

public record DbOperationPreviewResultDto(
    Guid JobId,
    int TotalRows,
    int AffectedRows,
    int ExcludedRows,
    int MergedRows,
    IReadOnlyList<DbOperationRowPreview> Samples);

public record DbOperationResultDto(
    Guid JobId,
    Guid TenantId,
    Guid ConnectionProfileId,
    string Status,
    string Stage,
    int ProgressPercent,
    int CorrectedRows,
    int MergedRows,
    int ExcludedRows,
    int TransformedRows,
    int ImportedRows,
    int ErrorRows,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record DbOperationJobDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    string Status,
    string Stage,
    int ProgressPercent,
    int TotalRows,
    string? PlanJson,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record StartDbOperationRequest(
    Guid ConnectionId,
    DbOperationActionPlan Plan);

public record StartDbOperationSessionRequest(Guid ConnectionId);

public record DbOperationRollbackResultDto(
    Guid JobId,
    int DeletedEntities,
    int RestoredEntities,
    string Status);

public sealed class DbOperationRowContext
{
    public int RowNumber { get; set; }
    public BusinessEntityType EntityType { get; set; }
    public string SchemaName { get; set; } = "public";
    public string TableName { get; set; } = string.Empty;
    public Dictionary<string, string?> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Status { get; set; } = DbOperationRowStatus.Active;
    public string? ExclusionReason { get; set; }
    public DateTime? SourceModifiedAtUtc { get; set; }
}

public sealed class DbOperationExecutionResult
{
    public int Corrected { get; set; }
    public int Merged { get; set; }
    public int Excluded { get; set; }
    public int Transformed { get; set; }
    public int Filtered { get; set; }
    public int Imported { get; set; }
    public int Errors { get; set; }
    public List<DbOperationRowContext> Rows { get; set; } = [];
}

public interface IDbOperationEngine
{
    DbOperationExecutionResult ApplyPreview(DbOperationActionPlan plan, IReadOnlyList<DbOperationRowContext> rows);
    DbOperationPreviewResultDto BuildPreview(Guid jobId, DbOperationActionPlan plan, IReadOnlyList<DbOperationRowContext> rows, int sampleSize = 25);
}

public interface IDbOperationService
{
    Task<DbOperationJobDto> StartSessionAsync(
        Guid tenantId, Guid userId, Guid connectionId,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<DbOperationPreviewResultDto> PreviewAsync(
        Guid tenantId, Guid jobId, DbOperationActionPlan plan, CancellationToken cancellationToken = default);

    Task<DbOperationResultDto> ExecuteAsync(
        Guid tenantId, Guid userId, Guid jobId, DbOperationActionPlan plan,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<DbOperationResultDto?> GetResultAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    Task<DbOperationJobDto?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    Task<DbOperationRollbackResultDto> RollbackAsync(
        Guid tenantId, Guid userId, Guid jobId,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
}

public interface IDbOperationProgressNotifier
{
    Task NotifyOperationStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default);
    Task NotifyOperationProgressAsync(Guid tenantId, Guid jobId, DbOperationProgress progress, CancellationToken cancellationToken = default);
    Task NotifyOperationCompletedAsync(Guid tenantId, Guid jobId, DbOperationResultDto result, CancellationToken cancellationToken = default);
    Task NotifyOperationFailedAsync(Guid tenantId, Guid jobId, string error, CancellationToken cancellationToken = default);
}

public class DbOperationJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = DbOperationJobStatus.Pending;
    public string Stage { get; set; } = DbOperationStages.Analyzing;
    public int ProgressPercent { get; set; }
    public int TotalRows { get; set; }
    public int CorrectedRows { get; set; }
    public int MergedRows { get; set; }
    public int ExcludedRows { get; set; }
    public int TransformedRows { get; set; }
    public int ImportedRows { get; set; }
    public int ErrorRows { get; set; }
    public string? PlanJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class DbOperationStagingRow
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public int RowNumber { get; set; }
    public BusinessEntityType EntityType { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string OriginalPayloadJson { get; set; } = "{}";
    public string Status { get; set; } = DbOperationRowStatus.Active;
    public string? ExclusionReason { get; set; }
    public Guid? CreatedEntityId { get; set; }
    public DateTime? SourceModifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DbOperationRollbackSnapshot
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public int RowNumber { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = "Created";
    public string PreviousStateJson { get; set; } = "{}";
    public bool RolledBack { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
