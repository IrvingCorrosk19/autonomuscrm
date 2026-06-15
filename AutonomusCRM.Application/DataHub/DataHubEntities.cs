namespace AutonomusCRM.Application.DataHub;

public class DataHubImportJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileFormat { get; set; } = "Csv";
    public string TargetEntity { get; set; } = "Customer";
    public string Status { get; set; } = DataHubJobStatus.Uploaded.ToString();
    public string LoadMode { get; set; } = DataHubLoadMode.InsertOnly.ToString();
    public long FileSizeBytes { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }
    public int SkippedRows { get; set; }
    public int CreatedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public string? StoredFilePath { get; set; }
    public string? DetectedEncoding { get; set; }
    public string? DetectedDelimiter { get; set; }
    public List<string> DetectedColumns { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsDryRun { get; set; }
    public bool RollbackAvailable { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DataHubImportBatch> Batches { get; set; } = new List<DataHubImportBatch>();
    public ICollection<DataHubImportRow> Rows { get; set; } = new List<DataHubImportRow>();
    public ICollection<DataHubImportError> Errors { get; set; } = new List<DataHubImportError>();
    public ICollection<DataHubImportMapping> Mappings { get; set; } = new List<DataHubImportMapping>();
    public ICollection<DataHubImportLog> Logs { get; set; } = new List<DataHubImportLog>();
}

public class DataHubImportBatch
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public int BatchNumber { get; set; }
    public int RowCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public DataHubImportJob? Job { get; set; }
}

public class DataHubImportRow
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public int RowNumber { get; set; }
    public int? BatchNumber { get; set; }
    public string Status { get; set; } = DataHubRowStatus.Pending.ToString();
    public Dictionary<string, string?> RawData { get; set; } = new();
    public Dictionary<string, string?> TransformedData { get; set; } = new();
    public Guid? CreatedEntityId { get; set; }
    public string? EntityType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DataHubImportJob? Job { get; set; }
}

public class DataHubImportError
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public int RowNumber { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? RawValue { get; set; }
    public bool IsRetryable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DataHubImportJob? Job { get; set; }
}

public class DataHubImportMapping
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? TransformRule { get; set; }
    public int SortOrder { get; set; }

    public DataHubImportJob? Job { get; set; }
}

public class DataHubImportTemplate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = "Customer";
    public string FileFormat { get; set; } = "Csv";
    public List<DataHubTemplateMapping> Mappings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }
    public int ActiveVersion { get; set; } = 1;
    public int LatestVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DataHubTemplateVersion
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid TenantId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsActive { get; set; }
    public List<DataHubTemplateMapping> Mappings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }
    public string? ChangeSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DataHubScheduledImport
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceEntity { get; set; } = string.Empty;
    public string Frequency { get; set; } = DataHubScheduleFrequency.Daily.ToString();
    public string ImportMode { get; set; } = "Full";
    public string LoadMode { get; set; } = "Upsert";
    public bool IsEnabled { get; set; } = true;
    public bool IsRunning { get; set; }
    public DateTime? RunningLeaseUntil { get; set; }
    public Guid? ActiveRunId { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? RunOnceAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DataHubScheduledImportRun
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExecutedByUserId { get; set; }
    public Guid? JobId { get; set; }
    public string Status { get; set; } = "Running";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorSummary { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

public class DataHubTemplateMapping
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? TransformRule { get; set; }
}

public class DataHubTransformationRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = "Customer";
    public string TargetField { get; set; } = string.Empty;
    public string TransformType { get; set; } = DataHubTransformType.Trim.ToString();
    public Dictionary<string, string> Parameters { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DataHubValidationRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = "Customer";
    public string TargetField { get; set; } = string.Empty;
    public string ValidationType { get; set; } = DataHubValidationType.Required.ToString();
    public Dictionary<string, string> Parameters { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DataHubRollbackSnapshot
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public int? RowNumber { get; set; }
    public int? BatchNumber { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = "Created";
    public Dictionary<string, object?> PreviousState { get; set; } = new();
    public bool RolledBack { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DataHubImportJob? Job { get; set; }
}

public class DataHubImportLog
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DataHubImportJob? Job { get; set; }
}
