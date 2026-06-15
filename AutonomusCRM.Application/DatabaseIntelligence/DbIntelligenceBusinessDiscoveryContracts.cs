namespace AutonomusCRM.Application.DatabaseIntelligence;

public enum BusinessEntityType
{
    Customer = 1,
    Company = 2,
    Contact = 3,
    Sale = 4,
    Invoice = 5,
    Payment = 6,
    Product = 7,
    Activity = 8,
    User = 9,
    Unknown = 99
}

public static class DbBusinessMappingStatus
{
    public const string Inferred = "Inferred";
    public const string Confirmed = "Confirmed";
    public const string Corrected = "Corrected";
    public const string Ignored = "Ignored";
}

public static class BusinessDiscoveryStages
{
    public const string AnalyzingTables = "AnalyzingTables";
    public const string AnalyzingColumns = "AnalyzingColumns";
    public const string DetectingEntities = "DetectingEntities";
    public const string CalculatingConfidence = "CalculatingConfidence";
    public const string Completed = "Completed";
}

public record BusinessEntityInferenceResult(
    string SchemaName,
    string TableName,
    BusinessEntityType EntityType,
    string DisplayName,
    int ConfidencePercent,
    IReadOnlyList<string> Reasons);

public record BusinessDiscoveryProgress(
    string Stage,
    int ProgressPercent,
    string? TableName = null,
    string? Message = null);

public record DbTableBusinessMappingDto(
    Guid Id,
    Guid TenantId,
    Guid ConnectionProfileId,
    Guid SnapshotId,
    string SchemaName,
    string TableName,
    BusinessEntityType InferredEntityType,
    BusinessEntityType EffectiveEntityType,
    int ConfidencePercent,
    IReadOnlyList<string> Reasons,
    string Status,
    Guid? ConfirmedByUserId,
    DateTime? ConfirmedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record BusinessDiscoveryResultDto(
    Guid JobId,
    Guid TenantId,
    Guid ConnectionProfileId,
    Guid SnapshotId,
    string Status,
    int ProgressPercent,
    int TablesAnalyzed,
    int EntitiesDetected,
    IReadOnlyList<DbTableBusinessMappingDto> Mappings,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record ConfirmBusinessMappingRequest(
    Guid MappingId,
    string Action,
    BusinessEntityType? CorrectedEntityType = null);

public sealed class BusinessDiscoveryTableInput
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ObjectType { get; set; } = DbCatalogObjectTypes.Table;
    public long? EstimatedRowCount { get; set; }
}

public sealed class BusinessDiscoveryColumnInput
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public bool IsIndexed { get; set; }
}

public sealed class BusinessDiscoveryRelationshipInput
{
    public string FromSchema { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToSchema { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
}

public sealed class BusinessDiscoveryCatalogInput
{
    public List<BusinessDiscoveryTableInput> Tables { get; set; } = [];
    public List<BusinessDiscoveryColumnInput> Columns { get; set; } = [];
    public List<BusinessDiscoveryRelationshipInput> Relationships { get; set; } = [];
    public Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string?>>> SampleRowsByTableKey { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static string TableKey(string schema, string table) => $"{schema}.{table}";
}

public interface IBusinessEntityInferenceEngine
{
    IReadOnlyList<BusinessEntityInferenceResult> InferFromCatalog(
        BusinessDiscoveryCatalogInput catalog,
        IProgress<BusinessDiscoveryProgress>? progress = null);
}

public interface IBusinessDiscoveryService
{
    Task<BusinessDiscoveryResultDto> RunBusinessDiscoveryAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<BusinessDiscoveryResultDto?> GetLatestBusinessDiscoveryAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DbTableBusinessMappingDto>> ListMappingsAsync(
        Guid tenantId,
        Guid? connectionId = null,
        CancellationToken cancellationToken = default);

    Task<DbTableBusinessMappingDto> ConfirmMappingAsync(
        Guid tenantId,
        Guid userId,
        ConfirmBusinessMappingRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}

public class DbBusinessDiscoveryJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = DbDiscoveryJobStatus.Pending;
    public string Stage { get; set; } = BusinessDiscoveryStages.AnalyzingTables;
    public int ProgressPercent { get; set; }
    public int TablesAnalyzed { get; set; }
    public int EntitiesDetected { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class DbTableBusinessMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid? BusinessDiscoveryJobId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public BusinessEntityType InferredEntityType { get; set; }
    public BusinessEntityType? ConfirmedEntityType { get; set; }
    public int ConfidencePercent { get; set; }
    public string ExplanationJson { get; set; } = "[]";
    public string Status { get; set; } = DbBusinessMappingStatus.Inferred;
    public Guid? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IDbIntelligenceBusinessProgressNotifier
{
    Task NotifyBusinessDiscoveryStartedAsync(Guid tenantId, Guid jobId, Guid connectionId, CancellationToken cancellationToken = default);
    Task NotifyBusinessDiscoveryProgressAsync(Guid tenantId, Guid jobId, BusinessDiscoveryProgress progress, CancellationToken cancellationToken = default);
    Task NotifyBusinessDiscoveryCompletedAsync(Guid tenantId, Guid jobId, int mappingsCount, CancellationToken cancellationToken = default);
    Task NotifyBusinessDiscoveryFailedAsync(Guid tenantId, Guid jobId, string safeMessage, CancellationToken cancellationToken = default);
}
