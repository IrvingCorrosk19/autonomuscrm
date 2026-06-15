namespace AutonomusCRM.Application.DataHub;

public enum DataHubIssueSeverity
{
    Error,
    Warning,
    Info
}

public enum DataHubWizardStep
{
    Upload = 1,
    Analyze = 2,
    DetectType = 3,
    Mapping = 4,
    Rules = 5,
    Validate = 6,
    Preview = 7,
    Confirm = 8,
    Import = 9,
    Finish = 10
}

public record DataHubColumnDetectionDto(
    string SourceColumn,
    string DetectedType,
    string? SuggestedTargetField,
    double ConfidencePercent,
    IReadOnlyList<string> SampleValues,
    string? MatchExplanation = null);

public record DataHubAiAnalysisResultDto(
    string SuggestedTargetEntity,
    double OverallConfidencePercent,
    IReadOnlyList<string> DetectedContentTypes,
    IReadOnlyList<DataHubColumnDetectionDto> ColumnDetections,
    IReadOnlyList<string> DetectedIssues,
    IReadOnlyList<string> RecommendedRules,
    IReadOnlyList<DataHubMappingDto> SuggestedMappings,
    string Summary);

public record DataHubCleaningSummaryDto(
    Guid JobId,
    int TotalRows,
    int ValidRows,
    int WarningRows,
    int ErrorRows,
    int DuplicateRows,
    double ValidPercent,
    bool ReadyToImport);

public record DataHubValidationIssueDto(
    int RowNumber,
    string ErrorCode,
    string Message,
    string? FieldName,
    DataHubIssueSeverity Severity,
    bool IsRetryable,
    bool AutoFixable);

public record DataHubExtendedValidationResultDto(
    Guid JobId,
    DataHubCleaningSummaryDto Summary,
    IReadOnlyList<DataHubValidationIssueDto> Issues,
    bool ReadyToImport);

public record DataHubAutoFixResultDto(
    int RowsFixed,
    int WarningsResolved,
    int DuplicatesMerged,
    IReadOnlyList<string> ActionsApplied);

public record DataHubVisualRuleDto(
    Guid? Id,
    string Name,
    string TargetField,
    string ConditionField,
    string ConditionOperator,
    string ConditionValue,
    string ActionType,
    string? ActionValue,
    bool IsActive,
    int Priority = 0,
    int Version = 1);

public record DataHubRuleSetVersionDto(
    string TargetEntity,
    int Version,
    DateTime SavedAt,
    int RuleCount);

public record DataHubJobMetricsDto(
    Guid JobId,
    string Status,
    double ProgressPercent,
    int RowsPerMinute,
    TimeSpan? EstimatedRemaining,
    DateTime? StartedAt,
    string? UserEmail);

public record DataHubQualityScoreDto(
    int Score,
    string Grade,
    int TotalIssues,
    int CriticalIssues,
    int WarningIssues,
    IReadOnlyList<DataHubQualityIssueDto> TopIssues);

public record DataHubTemplateSummaryDto(
    Guid Id,
    string Name,
    string TargetEntity,
    int MappingCount,
    DateTime UpdatedAt,
    int ActiveVersion = 1,
    int LatestVersion = 1);

public record DataHubTemplateVersionDto(
    Guid Id,
    Guid TemplateId,
    int VersionNumber,
    bool IsActive,
    int MappingCount,
    string? ChangeSummary,
    Guid? CreatedByUserId,
    DateTime CreatedAt);

public record DataHubTemplateVersionCompareDto(
    int VersionA,
    int VersionB,
    IReadOnlyList<string> AddedMappings,
    IReadOnlyList<string> RemovedMappings,
    IReadOnlyList<string> ChangedMappings);

public enum DataHubScheduleFrequency
{
    Once,
    Daily,
    Weekly,
    Monthly
}

public record DataHubScheduledImportDto(
    Guid Id,
    string Name,
    string Source,
    string SourceEntity,
    string Frequency,
    string ImportMode,
    string LoadMode,
    bool IsEnabled,
    DateTime? NextRunAt,
    DateTime? LastRunAt,
    DateTime CreatedAt);

public record DataHubScheduledImportRunDto(
    Guid Id,
    Guid ScheduleId,
    Guid? JobId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int DurationMs,
    string? ErrorSummary);

public record DataHubSmartMatchResult(
    string SourceColumn,
    string? TargetField,
    string DetectedType,
    double ConfidencePercent,
    string Explanation);

public record DataHubMatchColumnsRequest(
    IReadOnlyList<string> Columns,
    IReadOnlyList<Dictionary<string, string?>> SampleRows);

public interface IDataHubIntelligenceService
{
    DataHubAiAnalysisResultDto AnalyzeFile(
        string fileName,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> sampleRows,
        string? preferredEntity = null);

    IReadOnlyList<DataHubColumnDetectionDto> DetectColumns(
        string targetEntity,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> sampleRows);

    IReadOnlyList<DataHubSmartMatchResult> MatchColumnsV2(
        string targetEntity,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> sampleRows);
}

public interface IDataHubAutoFixService
{
    Task<DataHubAutoFixResultDto> AutoFixJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
}

public interface IDataHubRulesEngineService
{
    IReadOnlyList<DataHubVisualRuleDto> GetDefaultRules(string targetEntity);
    Task<IReadOnlyList<DataHubVisualRuleDto>> GetRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default);
    Task<DataHubRuleSetVersionDto> SaveRulesAsync(Guid tenantId, string targetEntity, IReadOnlyList<DataHubVisualRuleDto> rules, CancellationToken cancellationToken = default);
    Task<DataHubRuleSetVersionDto?> GetRuleSetVersionAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default);
    Dictionary<string, string?> ApplyRules(Dictionary<string, string?> row, IReadOnlyList<DataHubVisualRuleDto> rules);
}

public interface IDataHubJobQueue
{
    void Enqueue(Guid jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}

public record DataHubImportSummaryLinkDto(
    string EntityType,
    Guid EntityId,
    string Label,
    string Url);

public record DataHubImportSummaryDto(
    Guid JobId,
    string TargetEntity,
    string FileName,
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    int Duplicates,
    int QualityScore,
    string QualityGrade,
    TimeSpan? Duration,
    bool RollbackAvailable,
    IReadOnlyList<DataHubImportSummaryLinkDto> CreatedLinks,
    IReadOnlyList<DataHubImportSummaryLinkDto> UpdatedLinks);

public record DataHubProgressUpdateDto(
    Guid JobId,
    Guid TenantId,
    string Status,
    double ProgressPercent,
    int TotalRows,
    int ProcessedRows,
    int PendingRows,
    int SuccessRows,
    int FailedRows,
    int SkippedRows,
    int CreatedRecords,
    int UpdatedRecords,
    int RowsPerMinute,
    string? EstimatedRemaining);

public record DataHubQualityActionResultDto(
    bool Success,
    string Message,
    int AffectedCount);

public interface IDataHubProgressNotifier
{
    Task NotifyProgressAsync(DataHubProgressUpdateDto update, CancellationToken cancellationToken = default);
}

public interface IDataHubQualityActionService
{
    Task<DataHubQualityActionResultDto> MergeCustomersAsync(Guid tenantId, Guid keepId, IReadOnlyList<Guid> mergeIds, CancellationToken cancellationToken = default);
    Task<DataHubQualityActionResultDto> AssignLeadOwnerAsync(Guid tenantId, Guid leadId, Guid userId, CancellationToken cancellationToken = default);
    Task<DataHubQualityActionResultDto> AutoAssignLeadsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<DataHubQualityActionResultDto> MarkForReviewAsync(Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<DataHubQualityActionResultDto> KeepDuplicatesAsync(Guid tenantId, Guid entityId, CancellationToken cancellationToken = default);
}

public record DataHubDuplicateGroupDto(
    string MatchField,
    string MatchKey,
    int PrimaryRowNumber,
    IReadOnlyList<int> RowNumbers,
    Guid? ExistingEntityId,
    string SuggestedAction);

public record DataHubDuplicateScanResultDto(
    Guid JobId,
    int TotalGroups,
    int TotalDuplicateRows,
    IReadOnlyList<DataHubDuplicateGroupDto> Groups);

public record DataHubRollbackResultDto(
    Guid JobId,
    int EntitiesDeleted,
    int EntitiesRestored,
    int SnapshotsProcessed,
    string Scope);

public record DataHubStagingRowUpdateDto(int RowNumber, Dictionary<string, string?> Data);

public interface IDataHubRollbackService
{
    DataHubRollbackSnapshot CreateSnapshot(Guid jobId, Guid tenantId, int rowNumber, int? batchNumber, string entityType, Guid entityId, string action, Dictionary<string, object?>? previousState = null);
    Task<DataHubRollbackResultDto> ExecuteRollbackAsync(Guid tenantId, Guid jobId, int? batchNumber = null, int? rowNumber = null, CancellationToken cancellationToken = default);
}

public interface IDataHubDuplicateEngine
{
    Task<DataHubDuplicateScanResultDto> ScanJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<int> ApplyDuplicatePolicyAsync(Guid tenantId, Guid jobId, string targetEntity, string loadMode, CancellationToken cancellationToken = default);
    IReadOnlyList<DataHubDuplicateMatchField> GetActiveMatchFields(string targetEntity);
}

public interface IDataHubQualityScoreService
{
    Task<DataHubQualityScoreDto> CalculateScoreAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
