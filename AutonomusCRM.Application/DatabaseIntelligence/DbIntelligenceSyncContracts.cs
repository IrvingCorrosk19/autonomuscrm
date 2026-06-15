namespace AutonomusCRM.Application.DatabaseIntelligence;

public static class DbSyncMode
{
    public const string Full = "Full";
    public const string Delta = "Delta";
}

public static class DbSyncJobStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string CompletedWithWarnings = "CompletedWithWarnings";
    public const string Failed = "Failed";
    public const string RolledBack = "RolledBack";
}

public static class DbSyncStages
{
    public const string ReadingSource = "ReadingSource";
    public const string BuildingStaging = "BuildingStaging";
    public const string Validating = "Validating";
    public const string Importing = "Importing";
    public const string Completed = "Completed";
}

public static class DbSyncConflictPolicy
{
    public const string SourceWins = "SourceWins";
    public const string CrmWins = "CrmWins";
    public const string ManualReview = "ManualReview";
}

public static class DbSyncScheduleFrequency
{
    public const string Once = "Once";
    public const string Hourly = "Hourly";
    public const string Daily = "Daily";
    public const string Weekly = "Weekly";
}

public static class DbSyncStagingStatus
{
    public const string Pending = "Pending";
    public const string Valid = "Valid";
    public const string Invalid = "Invalid";
    public const string Imported = "Imported";
    public const string Conflict = "Conflict";
    public const string Skipped = "Skipped";
}

public record DbSyncProgress(
    string Stage,
    int ProgressPercent,
    string? Message = null);

public record DbSyncJobDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    string SyncMode,
    string Status,
    string Stage,
    int ProgressPercent,
    string ConflictPolicy,
    int TotalRows,
    int ImportedRows,
    int UpdatedRows,
    int SkippedRows,
    int ErrorRows,
    int DurationMs,
    string? ErrorMessage,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);

public record DbSyncHistoryItemDto(
    Guid Id,
    string SyncMode,
    string Status,
    int TotalRows,
    int ImportedRows,
    int ErrorRows,
    int DurationMs,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record DbSyncScheduleDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    string Name,
    string SyncMode,
    string Frequency,
    string ConflictPolicy,
    bool IsEnabled,
    DateTime? NextRunAt,
    DateTime? LastRunAt,
    DateTime CreatedAtUtc);

public record DbSyncRollbackResultDto(
    Guid JobId,
    int DeletedEntities,
    int RestoredEntities,
    string Status);

public record StartDbSyncRequest(
    Guid ConnectionId,
    string ConflictPolicy = DbSyncConflictPolicy.SourceWins);

public record ScheduleDbSyncRequest(
    Guid ConnectionId,
    string Name,
    string SyncMode,
    string Frequency,
    string ConflictPolicy = DbSyncConflictPolicy.SourceWins,
    DateTime? RunOnceAt = null);

public record DbSyncExecutionInput
{
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid JobId { get; set; }
    public string SyncMode { get; set; } = DbSyncMode.Full;
    public string ConflictPolicy { get; set; } = DbSyncConflictPolicy.SourceWins;
    public DateTime? WatermarkUtc { get; set; }
    public List<DbSyncMappingContext> Mappings { get; set; } = [];
    public List<DbSyncExtractedRow> ExtractedRows { get; set; } = [];
}

public record DbSyncMappingContext(
    Guid MappingId,
    string SchemaName,
    string TableName,
    BusinessEntityType EntityType,
    string Status);

public record DbSyncExtractedRow(
    BusinessEntityType EntityType,
    string SchemaName,
    string TableName,
    int RowNumber,
    Dictionary<string, string?> Data,
    DateTime? ModifiedAtUtc);

public record DbSyncLoadResult(
    int Created,
    int Updated,
    int Skipped,
    int Errors,
    Guid? EntityId,
    string? Error,
    DbSyncRollbackSnapshot? Snapshot);

public interface IDbSyncOrchestrator
{
    Task<DbSyncJobDto> StartFullSyncAsync(
        Guid tenantId, Guid userId, Guid connectionId, string conflictPolicy,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<DbSyncJobDto> StartDeltaSyncAsync(
        Guid tenantId, Guid userId, Guid connectionId, string conflictPolicy,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<DbSyncJobDto?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbSyncHistoryItemDto>> GetHistoryAsync(
        Guid tenantId, Guid? connectionId, int take, CancellationToken cancellationToken = default);

    Task<DbSyncRollbackResultDto> RollbackJobAsync(
        Guid tenantId, Guid userId, Guid jobId,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task ProcessPendingJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public interface IDbSyncPipeline
{
    Task ExecuteAsync(DbSyncExecutionInput input, IProgress<DbSyncProgress>? progress, CancellationToken cancellationToken = default);
}

public interface IDbSyncExtractService
{
    Task<IReadOnlyList<DbSyncExtractedRow>> ExtractAsync(
        Guid tenantId, Guid connectionId, IReadOnlyList<DbSyncMappingContext> mappings,
        string syncMode, DateTime? watermarkUtc, CancellationToken cancellationToken = default);
}

public interface IDbSyncStagingService
{
    Task StageRowsAsync(Guid tenantId, Guid jobId, IReadOnlyList<DbSyncExtractedRow> rows, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DbSyncStagingRow>> GetPendingRowsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
}

public interface IDbSyncLoadService
{
    Task<DbSyncLoadResult> LoadRowAsync(
        Guid tenantId, Guid jobId, DbSyncStagingRow row, string conflictPolicy,
        CancellationToken cancellationToken = default);
}

public interface IDbSyncRollbackService
{
    DbSyncRollbackSnapshot CreateSnapshot(
        Guid jobId, Guid tenantId, int rowNumber, string entityType, Guid entityId, string action,
        Dictionary<string, object?>? previousState = null);

    Task<DbSyncRollbackResultDto> ExecuteRollbackAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
}

public interface IDbSyncConflictResolver
{
    DbSyncConflictDecision Resolve(
        string conflictPolicy, bool existsInCrm, bool sourceIsNewer, bool hasConflict);
}

public record DbSyncConflictDecision(string Action, string Reason);

public interface IDbSyncScheduleService
{
    Task<DbSyncScheduleDto> CreateScheduleAsync(
        Guid tenantId, Guid userId, ScheduleDbSyncRequest request,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbSyncScheduleDto>> ListSchedulesAsync(Guid tenantId, Guid? connectionId, CancellationToken cancellationToken = default);

    Task ProcessDueSchedulesAsync(CancellationToken cancellationToken = default);
}

public interface IDbSyncDispatcher
{
    Task EnqueueSyncJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
}

public interface IDbIntelligenceSyncProgressNotifier
{
    Task NotifySyncStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default);
    Task NotifySyncProgressAsync(Guid tenantId, Guid jobId, DbSyncProgress progress, CancellationToken cancellationToken = default);
    Task NotifySyncCompletedAsync(Guid tenantId, Guid jobId, int imported, int errors, CancellationToken cancellationToken = default);
    Task NotifySyncFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default);
}

public class DbSyncJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string SyncMode { get; set; } = DbSyncMode.Full;
    public string Status { get; set; } = DbSyncJobStatus.Pending;
    public string Stage { get; set; } = DbSyncStages.ReadingSource;
    public int ProgressPercent { get; set; }
    public string ConflictPolicy { get; set; } = DbSyncConflictPolicy.SourceWins;
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int SkippedRows { get; set; }
    public int ErrorRows { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? WatermarkBeforeUtc { get; set; }
    public DateTime? WatermarkAfterUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class DbSyncStagingRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid JobId { get; set; }
    public int RowNumber { get; set; }
    public BusinessEntityType EntityType { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = DbSyncStagingStatus.Pending;
    public string? ValidationError { get; set; }
    public Guid? CreatedEntityId { get; set; }
    public DateTime? SourceModifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DbSyncRollbackSnapshot
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public int RowNumber { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PreviousStateJson { get; set; } = "{}";
    public bool RolledBack { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DbSyncSchedule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SyncMode { get; set; } = DbSyncMode.Full;
    public string Frequency { get; set; } = DbSyncScheduleFrequency.Daily;
    public string ConflictPolicy { get; set; } = DbSyncConflictPolicy.SourceWins;
    public bool IsEnabled { get; set; } = true;
    public bool IsRunning { get; set; }
    public DateTime? RunningLeaseUntil { get; set; }
    public Guid? ActiveRunId { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? RunOnceAt { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DbSyncWatermark
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public BusinessEntityType EntityType { get; set; }
    public DateTime LastSyncedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
