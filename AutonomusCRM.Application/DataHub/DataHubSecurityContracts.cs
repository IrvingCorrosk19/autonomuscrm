namespace AutonomusCRM.Application.DataHub;

public sealed class DataHubSecurityOptions
{
    public const string SectionName = "DataHub:Security";

    public long MaxFileBytes { get; set; } = DataHubConstants.MaxFileBytes;
    public int MaxImportsPerHour { get; set; } = 30;
    public int MaxExportsPerHour { get; set; } = 60;
    public int MaxConcurrentJobs { get; set; } = 5;
    public bool RequireMalwareScan { get; set; } = true;
    public bool EncryptStorage { get; set; } = true;
    public string ActiveEncryptionKeyId { get; set; } = "v1";
    public Dictionary<string, string> EncryptionKeys { get; set; } = new();
    public string? ClamAvHost { get; set; }
    public int ClamAvPort { get; set; } = 3310;
    public int ClamAvTimeoutSeconds { get; set; } = 30;
}

public record DataHubMalwareScanResult(bool IsClean, string? ThreatName, string Scanner);

public record DataHubForensicAuditEntry(
    Guid TenantId,
    string Action,
    Guid? UserId = null,
    Guid? JobId = null,
    string? FileName = null,
    long? FileSizeBytes = null,
    string? FileHashSha256 = null,
    string? IpAddress = null,
    string? UserAgent = null,
    Dictionary<string, object>? Details = null);

public interface IDataHubTenantGuard
{
    bool IsSameTenant(Guid requestedTenantId);
    void EnsureSameTenant(Guid requestedTenantId);
    Guid? GetCurrentTenantId();
}

public interface IDataHubForensicAuditService
{
    Task RecordAsync(DataHubForensicAuditEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubForensicAudit>> GetRecentAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default);
    Task<int> CountActionsAsync(Guid tenantId, string action, TimeSpan window, CancellationToken cancellationToken = default);
}

public interface IDataHubMalwareScanner
{
    Task<DataHubMalwareScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}

public interface IDataHubSecurityQuotaService
{
    Task EnsureUploadAllowedAsync(Guid tenantId, long fileSizeBytes, CancellationToken cancellationToken = default);
    Task EnsureExportAllowedAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task EnsureImportStartAllowedAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IDataHubRequestContext
{
    string? ClientIp { get; }
    string? UserAgent { get; }
}

public class DataHubForensicAudit
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? JobId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? FileHashSha256 { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class DataHubForensicActions
{
    public const string Upload = "Upload";
    public const string MalwareBlocked = "MalwareBlocked";
    public const string ImportStart = "ImportStart";
    public const string ImportComplete = "ImportComplete";
    public const string Export = "Export";
    public const string ExportErrors = "ExportErrors";
    public const string Rollback = "Rollback";
    public const string Cancel = "Cancel";
    public const string TemplateSave = "TemplateSave";
    public const string RulesSave = "RulesSave";
    public const string QuotaBlocked = "QuotaBlocked";
}

public class DataHubTenantAccessException : UnauthorizedAccessException
{
    public DataHubTenantAccessException(string message) : base(message) { }
}

public class DataHubSecurityQuotaException : InvalidOperationException
{
    public DataHubSecurityQuotaException(string message) : base(message) { }
}

public class DataHubMalwareDetectedException : InvalidOperationException
{
    public DataHubMalwareDetectedException(string threatName)
        : base($"Malware detected: {threatName}") { }
}
