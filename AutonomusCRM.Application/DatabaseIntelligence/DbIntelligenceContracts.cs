namespace AutonomusCRM.Application.DatabaseIntelligence;

public enum DbEngineType
{
    PostgreSQL = 1,
    SqlServer = 2,
    MySQL = 3,
    MariaDB = 4,
    Oracle = 5
}

public sealed class DbIntelligenceSecurityOptions
{
    public const string SectionName = "DatabaseIntelligence:Security";

    public string ActiveEncryptionKeyId { get; set; } = "v1";
    public Dictionary<string, string> EncryptionKeys { get; set; } = new();
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public int MaxConnectionsPerTenant { get; set; } = 10;
}

public record DbConnectionProfileDto(
    Guid Id,
    Guid TenantId,
    string Name,
    DbEngineType EngineType,
    string Host,
    int Port,
    string DatabaseName,
    string UsernameMasked,
    bool IsReadOnly,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastTestedAtUtc,
    bool? LastTestSucceeded,
    string? LastErrorMessage);

public record CreateDbConnectionProfileRequest(
    string Name,
    DbEngineType EngineType,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string Password,
    bool IsReadOnly = true);

public record TestDbConnectionRequest(
    DbEngineType EngineType,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string Password,
    bool IsReadOnly = true);

public record DbConnectionTestResultDto(
    bool Success,
    string Message,
    int LatencyMs,
    DbEngineType EngineType,
    bool ReadOnlyMode);

public record DbConnectionSecrets(string Password);

public interface IDbConnectionProfileService
{
    Task<DbConnectionProfileDto> CreateAsync(
        Guid tenantId,
        Guid userId,
        CreateDbConnectionProfileRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbConnectionProfileDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<DbConnectionProfileDto?> GetAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DbConnectionTestResultDto> TestExistingAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DbConnectionTestResultDto> TestAsync(
        TestDbConnectionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDbConnectorFactory
{
    IDbConnector Create(DbEngineType engineType);
}

public interface IDbConnector
{
    DbEngineType EngineType { get; }
    bool SupportsReadOnlyMode { get; }
    Task<DbConnectionTestResultDto> TestConnectionAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        CancellationToken cancellationToken = default);
}

public record DbConnectionEndpoint(
    string Host,
    int Port,
    string DatabaseName,
    string Username);

public interface IDbConnectionVault
{
    byte[] Encrypt(DbConnectionSecrets secrets);
    DbConnectionSecrets Decrypt(byte[] encryptedBlob);
}

public record DbIntelligenceAuditEntry(
    Guid TenantId,
    string Action,
    Guid? UserId = null,
    Guid? ConnectionProfileId = null,
    DbEngineType? EngineType = null,
    string? HostMasked = null,
    string? DatabaseName = null,
    bool? Success = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? ErrorMessage = null);

public interface IDbIntelligenceAuditService
{
    Task RecordAsync(DbIntelligenceAuditEntry entry, CancellationToken cancellationToken = default);
}

public interface IDbIntelligenceTenantGuard
{
    Guid? GetCurrentTenantId();
    bool IsSameTenant(Guid requestedTenantId);
    void EnsureSameTenant(Guid requestedTenantId);
    bool IsAdminOrOwner();
}

public class DbConnectionProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DbEngineType EngineType { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string UsernameMasked { get; set; } = string.Empty;
    public byte[] EncryptedConnectionBlob { get; set; } = Array.Empty<byte>();
    public bool IsReadOnly { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastTestedAtUtc { get; set; }
    public bool? LastTestSucceeded { get; set; }
    public string? LastErrorMessage { get; set; }
}

public class DbDiscoveryJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = DbDiscoveryJobStatus.Pending;
    public int ProgressPercent { get; set; }
    public int TablesDiscovered { get; set; }
    public int ViewsDiscovered { get; set; }
    public int ColumnsDiscovered { get; set; }
    public int RelationshipsDiscovered { get; set; }
    public Guid? CatalogSnapshotId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? LogsJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class DbCatalogSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid? DiscoveryJobId { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public int SchemaCount { get; set; }
    public int TableCount { get; set; }
    public int ViewCount { get; set; }
    public int ColumnCount { get; set; }
    public int IndexCount { get; set; }
    public int RelationshipCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DbIntelligenceForensicAudit
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ConnectionProfileId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EngineType { get; set; }
    public string? HostMasked { get; set; }
    public string? DatabaseName { get; set; }
    public bool? Success { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class DbIntelligenceForensicActions
{
    public const string ConnectionCreated = "ConnectionCreated";
    public const string ConnectionTested = "ConnectionTested";
    public const string ConnectionTestFailed = "ConnectionTestFailed";
    public const string ConnectionDeleted = "ConnectionDeleted";
    public const string DiscoveryStarted = "DiscoveryStarted";
    public const string DiscoveryCompleted = "DiscoveryCompleted";
    public const string DiscoveryFailed = "DiscoveryFailed";
    public const string BusinessDiscoveryStarted = "BusinessDiscoveryStarted";
    public const string BusinessDiscoveryCompleted = "BusinessDiscoveryCompleted";
    public const string BusinessDiscoveryFailed = "BusinessDiscoveryFailed";
    public const string BusinessMappingConfirmed = "BusinessMappingConfirmed";
    public const string HealthScanStarted = "HealthScanStarted";
    public const string HealthScanCompleted = "HealthScanCompleted";
    public const string HealthScanFailed = "HealthScanFailed";
    public const string GraphBuildStarted = "GraphBuildStarted";
    public const string GraphBuildCompleted = "GraphBuildCompleted";
    public const string GraphBuildFailed = "GraphBuildFailed";
    public const string GraphExported = "GraphExported";
    public const string SyncFullStarted = "SyncFullStarted";
    public const string SyncDeltaStarted = "SyncDeltaStarted";
    public const string SyncCompleted = "SyncCompleted";
    public const string SyncFailed = "SyncFailed";
    public const string SyncRolledBack = "SyncRolledBack";
    public const string SyncScheduleCreated = "SyncScheduleCreated";
    public const string InsightsGenerationStarted = "InsightsGenerationStarted";
    public const string InsightsGenerationCompleted = "InsightsGenerationCompleted";
    public const string InsightsGenerationFailed = "InsightsGenerationFailed";
    public const string OperationStarted = "OperationStarted";
    public const string OperationExecuteStarted = "OperationExecuteStarted";
    public const string OperationExecuteCompleted = "OperationExecuteCompleted";
    public const string OperationRolledBack = "OperationRolledBack";
}

public class DbIntelligenceTenantAccessException : UnauthorizedAccessException
{
    public DbIntelligenceTenantAccessException(string message) : base(message) { }
}

public class DbIntelligenceValidationException : InvalidOperationException
{
    public DbIntelligenceValidationException(string message) : base(message) { }
}

public class DbIntelligenceQuotaException : InvalidOperationException
{
    public DbIntelligenceQuotaException(string message) : base(message) { }
}

public static class DbIntelligenceMasking
{
    public static string MaskUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return "***";
        var trimmed = username.Trim();
        if (trimmed.Length <= 2) return new string('*', trimmed.Length);
        return $"{trimmed[0]}{new string('*', Math.Min(trimmed.Length - 2, 6))}{trimmed[^1]}";
    }

    public static string MaskHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return "***";
        var trimmed = host.Trim();
        if (trimmed.Length <= 4) return "***";
        return $"{trimmed[..2]}***{trimmed[^2..]}";
    }
}
