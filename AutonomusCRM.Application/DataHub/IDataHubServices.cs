namespace AutonomusCRM.Application.DataHub;

public record DataHubJobSummaryDto(
    Guid Id,
    string FileName,
    string TargetEntity,
    string Status,
    string LoadMode,
    int TotalRows,
    int ProcessedRows,
    int SuccessRows,
    int FailedRows,
    int SkippedRows,
    int CreatedRecords,
    int UpdatedRecords,
    double ProgressPercent,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool RollbackAvailable,
    string? ErrorSummary);

public record DataHubJobDetailDto(
    DataHubJobSummaryDto Summary,
    IReadOnlyList<string> DetectedColumns,
    IReadOnlyList<DataHubMappingDto> Mappings,
    IReadOnlyList<DataHubLogDto> RecentLogs,
    IReadOnlyList<DataHubErrorDto> RecentErrors,
    IReadOnlyList<DataHubRowPreviewDto> PreviewRows);

public record DataHubMappingDto(
    Guid? Id,
    string SourceColumn,
    string TargetField,
    bool IsRequired,
    string? DefaultValue,
    string? TransformRule);

public record DataHubLogDto(DateTime At, string Level, string Message);

public record DataHubErrorDto(int RowNumber, string ErrorCode, string Message, string? FieldName, bool IsRetryable);

public record DataHubRowPreviewDto(int RowNumber, string Status, Dictionary<string, string?> Data);

public record DataHubUploadResultDto(Guid JobId, string Status, IReadOnlyList<string> DetectedColumns, int PreviewRowCount);

public record DataHubValidationResultDto(
    Guid JobId,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<DataHubErrorDto> Errors,
    bool ReadyToImport);

public record DataHubImportResultDto(
    Guid JobId,
    string Status,
    int TotalRows,
    int CreatedRecords,
    int UpdatedRecords,
    int FailedRows,
    int SkippedRows,
    IReadOnlyList<DataHubErrorDto> Errors);

public record DataHubExtractChunk(
    IReadOnlyList<string> Columns,
    IReadOnlyList<Dictionary<string, string?>> Rows,
    int StartRowNumber,
    string Encoding,
    string? Delimiter,
    bool IsFirstChunk);

public record DataHubBulkInsertResult(int RowsInserted, TimeSpan Duration);

public enum DataHubProcessingMode
{
    InProcess,
    RabbitMQ
}

public record DataHubImportJobMessage(Guid JobId, Guid TenantId, int Attempt = 0);

public record DataHubScaleMetricsDto(
    int RowCount,
    long PeakMemoryBytes,
    double RowsPerSecond,
    TimeSpan Duration,
    string Operation);

public record DataHubExportRequestDto(
    string EntityType,
    string Format,
    Dictionary<string, string>? Filters);

public record DataHubQualityIssueDto(
    string EntityType,
    Guid EntityId,
    string IssueCode,
    string Message,
    string Severity,
    Dictionary<string, string>? SuggestedActions);

public record DataHubFieldDefinition(string Name, string Label, bool IsRequired, string DataType, int? MaxLength);

public record DataHubAutoMapResult(IReadOnlyList<DataHubMappingDto> Mappings, int MatchedColumns, int UnmappedColumns);

public interface IDataHubRepository
{
    Task<DataHubImportJob?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubImportJob?> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubImportJob>> ListJobsAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
    Task AddJobAsync(DataHubImportJob job, CancellationToken cancellationToken = default);
    Task UpdateJobAsync(DataHubImportJob job, CancellationToken cancellationToken = default);
    Task AddRowsAsync(IEnumerable<DataHubImportRow> rows, CancellationToken cancellationToken = default);
    Task UpdateRowsAsync(IEnumerable<DataHubImportRow> rows, CancellationToken cancellationToken = default);
    Task AddErrorsAsync(IEnumerable<DataHubImportError> errors, CancellationToken cancellationToken = default);
    Task AddLogAsync(DataHubImportLog log, CancellationToken cancellationToken = default);
    Task AddMappingsAsync(IEnumerable<DataHubImportMapping> mappings, CancellationToken cancellationToken = default);
    Task ReplaceMappingsAsync(Guid tenantId, Guid jobId, IEnumerable<DataHubImportMapping> mappings, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubImportRow>> GetRowsAsync(Guid tenantId, Guid jobId, int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubImportError>> GetErrorsAsync(Guid tenantId, Guid jobId, int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubImportMapping>> GetMappingsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubImportLog>> GetLogsAsync(Guid tenantId, Guid jobId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubImportJob>> GetPendingJobsAsync(int take, CancellationToken cancellationToken = default);
    Task AddRollbackSnapshotsAsync(IEnumerable<DataHubRollbackSnapshot> snapshots, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubRollbackSnapshot>> GetRollbackSnapshotsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubTransformationRule>> GetTransformationRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubValidationRule>> GetValidationRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default);
    Task SaveTransformationRuleAsync(DataHubTransformationRule rule, CancellationToken cancellationToken = default);
    Task SaveValidationRuleAsync(DataHubValidationRule rule, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubValidationRule>> GetAllValidationRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default);
    Task ReplaceValidationRulesAsync(Guid tenantId, string targetEntity, IEnumerable<DataHubValidationRule> rules, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubImportTemplate>> GetTemplatesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task SaveTemplateAsync(DataHubImportTemplate template, CancellationToken cancellationToken = default);
    Task<int> IncrementTemplateLatestVersionAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<int> BulkInsertRowsCopyAsync(IReadOnlyList<DataHubImportRow> rows, CancellationToken cancellationToken = default);
    Task<int> CountActiveJobsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubScheduledImport>> GetScheduledImportsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<DataHubScheduledImport?> GetScheduledImportAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubScheduledImport>> GetDueScheduledImportsAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task<DataHubScheduledImport?> TryClaimScheduledImportAsync(Guid scheduleId, Guid runId, DateTime leaseUntil, CancellationToken cancellationToken = default);
    Task ReleaseScheduledImportLeaseAsync(Guid scheduleId, Guid runId, CancellationToken cancellationToken = default);
    Task<int> RecoverExpiredScheduledImportLeasesAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task SaveScheduledImportAsync(DataHubScheduledImport schedule, CancellationToken cancellationToken = default);
    Task UpdateScheduledImportAsync(DataHubScheduledImport schedule, CancellationToken cancellationToken = default);
    Task DeleteScheduledImportAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default);
    Task AddScheduledImportRunAsync(DataHubScheduledImportRun run, CancellationToken cancellationToken = default);
    Task UpdateScheduledImportRunAsync(DataHubScheduledImportRun run, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubScheduledImportRun>> GetScheduledImportRunsAsync(Guid tenantId, Guid scheduleId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubTemplateVersion>> GetTemplateVersionsAsync(Guid tenantId, Guid templateId, CancellationToken cancellationToken = default);
    Task AddTemplateVersionAsync(DataHubTemplateVersion version, CancellationToken cancellationToken = default);
    Task UpdateTemplateVersionAsync(DataHubTemplateVersion version, CancellationToken cancellationToken = default);
    Task DeactivateTemplateVersionsAsync(Guid tenantId, Guid templateId, CancellationToken cancellationToken = default);
    Task<bool> TryAcquireJobProcessingLockAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task ReleaseJobProcessingLockAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task DeleteStagingRowsForJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}

public interface IDataHubOrchestrator
{
    Task<DataHubUploadResultDto> UploadAsync(Guid tenantId, Guid userId, Stream fileStream, string fileName, string targetEntity, string loadMode, bool dryRun, CancellationToken cancellationToken = default);
    Task<DataHubAiAnalysisResultDto> AnalyzeWithAiAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubAutoMapResult> AutoMapAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task SaveMappingsAsync(Guid tenantId, Guid jobId, IReadOnlyList<DataHubMappingDto> mappings, CancellationToken cancellationToken = default);
    Task<DataHubValidationResultDto> ValidateAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubExtendedValidationResultDto> ValidateExtendedAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubCleaningSummaryDto> GetCleaningSummaryAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubAutoFixResultDto> AutoFixAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubImportResultDto> StartImportAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task CancelJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubImportResultDto> RetryFailedRowsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task RecoverOrphanJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task RollbackJobAsync(Guid tenantId, Guid jobId, int? batchNumber = null, int? rowNumber = null, CancellationToken cancellationToken = default);
    Task<DataHubDuplicateScanResultDto> ScanDuplicatesAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task UpdateStagingRowsAsync(Guid tenantId, Guid jobId, IReadOnlyList<DataHubStagingRowUpdateDto> updates, CancellationToken cancellationToken = default);
    Task<DataHubImportSummaryDto> GetImportSummaryAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<DataHubTemplateSummaryDto> SaveTemplateFromJobAsync(Guid tenantId, Guid jobId, string templateName, CancellationToken cancellationToken = default);
    Task<DataHubJobMetricsDto> GetJobMetricsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public interface IDataHubExtractService
{
    Task<(List<string> Columns, List<Dictionary<string, string?>> Rows, string Encoding, string? Delimiter)> ExtractAsync(
        Stream stream, string fileName, CancellationToken cancellationToken = default);

    IAsyncEnumerable<DataHubExtractChunk> ExtractInChunksAsync(
        Stream stream, string fileName, int chunkSize = DataHubConstants.ExtractChunkSize,
        CancellationToken cancellationToken = default);
}

public interface IDataHubTransformService
{
    Dictionary<string, string?> TransformRow(Dictionary<string, string?> raw, IReadOnlyList<DataHubImportMapping> mappings, IReadOnlyList<DataHubTransformationRule> rules);
    string ApplyTransform(string value, string transformType, IReadOnlyDictionary<string, string>? parameters = null);
}

public interface IDataHubValidateService
{
    Task<IReadOnlyList<DataHubImportError>> ValidateRowAsync(Guid tenantId, string targetEntity, int rowNumber, Dictionary<string, string?> data, IReadOnlyList<DataHubValidationRule> rules, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubQualityIssueDto>> ScanQualityAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record DataHubLoadRowResult(
    int Created,
    int Updated,
    int Skipped,
    Guid? EntityId,
    string? Error,
    DataHubRollbackSnapshot? RollbackSnapshot);

public interface IDataHubLoadService
{
    Task<DataHubLoadRowResult> LoadRowAsync(
        Guid tenantId, string targetEntity, string loadMode, Dictionary<string, string?> data,
        bool dryRun, int rowNumber = 0, int? batchNumber = null,
        CancellationToken cancellationToken = default);
}

public interface IDataHubExportService
{
    Task ExportToStreamAsync(Guid tenantId, string entityType, string format, Stream output, Dictionary<string, string>? filters, CancellationToken cancellationToken = default);
    Task ExportErrorsToStreamAsync(Guid tenantId, Guid jobId, string format, Stream output, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Dictionary<string, string?>> StreamEntityRowsAsync(Guid tenantId, string entityType, CancellationToken cancellationToken = default);
}

public interface IDataHubImportDispatcher
{
    Task EnqueueImportJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
}

public interface IDataHubSecurityService
{
    (bool Ok, string? Error) ValidateUpload(string fileName, long fileSize, string? contentType);
    string SanitizeCellValue(string? value);
}

public interface IDataHubFieldCatalog
{
    IReadOnlyList<DataHubFieldDefinition> GetFields(string targetEntity);
    DataHubAutoMapResult SuggestMappings(string targetEntity, IReadOnlyList<string> sourceColumns);
}

public interface IDataHubMigrationService
{
    Task<IReadOnlyList<DataHubMigrationSourceDto>> ListSourcesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    IReadOnlyList<DataHubMigrationEntityDto> ListEntities(string source);
    Task<DataHubMigrationConnectionStatusDto> GetConnectionStatusAsync(Guid tenantId, string source, CancellationToken cancellationToken = default);
    Task<DataHubMigrationStartResultDto> StartMigrationAsync(DataHubMigrationRequestDto request, CancellationToken cancellationToken = default);
    Task<DataHubMigrationQualityReportDto> ValidateMigrationQualityAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task TryCompleteMigrationSyncAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
}

public interface IMigrationSyncCompleter
{
    Task TryCompleteMigrationSyncAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
}
