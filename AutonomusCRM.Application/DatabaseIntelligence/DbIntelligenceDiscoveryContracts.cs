namespace AutonomusCRM.Application.DatabaseIntelligence;

public static class DbDiscoveryJobStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string CompletedWithWarnings = "CompletedWithWarnings";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public static class DbCatalogObjectTypes
{
    public const string Table = "Table";
    public const string View = "View";
}

public static class DbRelationshipSource
{
    public const string ExplicitForeignKey = "ExplicitForeignKey";
    public const string NamingHeuristic = "NamingHeuristic";
}

public record DbDiscoveryJobDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    string Status,
    int ProgressPercent,
    int TablesDiscovered,
    int ViewsDiscovered,
    int ColumnsDiscovered,
    int RelationshipsDiscovered,
    Guid? CatalogSnapshotId,
    string? ErrorMessage,
    IReadOnlyList<string> Logs,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    long? DurationMs);

public record DbCatalogSnapshotDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    Guid? DiscoveryJobId,
    int SchemaVersion,
    int SchemaCount,
    int TableCount,
    int ViewCount,
    int ColumnCount,
    int IndexCount,
    int RelationshipCount,
    DateTime CreatedAtUtc);

public record DbCatalogTableDto(
    Guid Id,
    string SchemaName,
    string ObjectName,
    string ObjectType,
    long? EstimatedRowCount,
    int ColumnCount,
    bool HasPrimaryKey);

public record DbCatalogColumnDto(
    Guid Id,
    string SchemaName,
    string ObjectName,
    string ColumnName,
    string DataType,
    bool IsNullable,
    string? DefaultValue,
    bool IsPrimaryKey,
    bool IsForeignKey,
    bool IsIndexed,
    int Ordinal);

public record DbCatalogRelationshipDto(
    Guid Id,
    string FromSchema,
    string FromTable,
    string FromColumn,
    string ToSchema,
    string ToTable,
    string ToColumn,
    string Source,
    int ConfidencePercent);

public record DbCatalogExploreDto(
    DbConnectionProfileDto Connection,
    DbDiscoveryJobDto? LatestJob,
    DbCatalogSnapshotDto? Snapshot,
    IReadOnlyList<DbCatalogTableDto> Tables,
    IReadOnlyList<DbCatalogColumnDto> Columns,
    IReadOnlyList<DbCatalogRelationshipDto> Relationships);

public interface IDbSchemaDiscoveryService
{
    Task<DbDiscoveryJobDto> StartDiscoveryAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DbDiscoveryJobDto?> GetDiscoveryJobAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<DbCatalogSnapshotDto?> GetCatalogSnapshotAsync(
        Guid tenantId,
        Guid snapshotId,
        CancellationToken cancellationToken = default);

    Task<DbCatalogSnapshotDto?> GetLatestCatalogForConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<(DbDiscoveryJobDto Job, DbCatalogSnapshotDto Snapshot)> DiscoverNowAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbCatalogTableDto>> ListCatalogTablesAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbCatalogRelationshipDto>> ListCatalogRelationshipsAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbCatalogColumnDto>> ListCatalogColumnsAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);
}

public interface IDbSchemaIntrospector
{
    DbEngineType EngineType { get; }
    Task<PhysicalSchemaDiscoveryResult> DiscoverAsync(
        DbConnectionEndpoint endpoint,
        DbConnectionSecrets secrets,
        bool readOnly,
        int timeoutSeconds,
        IProgress<DbDiscoveryProgress>? progress,
        CancellationToken cancellationToken = default);
}

public record DbDiscoveryProgress(
    string Stage,
    int ProgressPercent,
    string? SchemaName = null,
    string? TableName = null,
    string? Message = null,
    string? ObjectType = null);

public interface IDbIntelligenceProgressNotifier
{
    Task NotifyDiscoveryStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default);
    Task NotifySchemaDiscoveredAsync(Guid tenantId, Guid jobId, string schemaName, CancellationToken cancellationToken = default);
    Task NotifyTableDiscoveredAsync(Guid tenantId, Guid jobId, string schemaName, string tableName, string objectType, CancellationToken cancellationToken = default);
    Task NotifyDiscoveryProgressAsync(Guid tenantId, Guid jobId, DbDiscoveryProgress progress, CancellationToken cancellationToken = default);
    Task NotifyDiscoveryCompletedAsync(Guid tenantId, Guid jobId, Guid snapshotId, CancellationToken cancellationToken = default);
    Task NotifyDiscoveryFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default);
}

public sealed class PhysicalSchemaDiscoveryResult
{
    public List<PhysicalSchemaInfo> Schemas { get; set; } = [];
    public List<PhysicalTableInfo> Tables { get; set; } = [];
    public List<PhysicalColumnInfo> Columns { get; set; } = [];
    public List<PhysicalIndexInfo> Indexes { get; set; } = [];
    public List<PhysicalConstraintInfo> Constraints { get; set; } = [];
    public List<PhysicalRelationshipInfo> Relationships { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class PhysicalSchemaInfo
{
    public string SchemaName { get; set; } = string.Empty;
}

public sealed class PhysicalTableInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string ObjectType { get; set; } = DbCatalogObjectTypes.Table;
    public long? EstimatedRowCount { get; set; }
}

public sealed class PhysicalColumnInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
    public int Ordinal { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public bool IsIndexed { get; set; }
}

public sealed class PhysicalIndexInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public IReadOnlyList<string> ColumnNames { get; set; } = Array.Empty<string>();
}

public sealed class PhysicalConstraintInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string ConstraintName { get; set; } = string.Empty;
    public string ConstraintType { get; set; } = string.Empty;
    public IReadOnlyList<string> ColumnNames { get; set; } = Array.Empty<string>();
}

public sealed class PhysicalRelationshipInfo
{
    public string FromSchema { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToSchema { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
    public string Source { get; set; } = DbRelationshipSource.ExplicitForeignKey;
    public int ConfidencePercent { get; set; } = 100;
}
