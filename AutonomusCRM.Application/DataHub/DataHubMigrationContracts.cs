namespace AutonomusCRM.Application.DataHub;

public enum DataHubMigrationImportMode
{
    Full,
    Delta
}

public record DataHubMigrationSourceDto(
    string Source,
    string DisplayName,
    string Description,
    bool ConnectorAvailable,
    bool IsConnected,
    DateTime? LastSyncAt);

public record DataHubMigrationEntityDto(
    string SourceEntity,
    string DisplayName,
    string TargetEntity,
    string Description);

public record DataHubMigrationConnectionStatusDto(
    string Source,
    bool IsConnected,
    bool OAuthConfigured,
    DateTime? LastSyncAt,
    string? Detail);

public record DataHubMigrationRequestDto(
    Guid TenantId,
    Guid UserId,
    string Source,
    string SourceEntity,
    DataHubMigrationImportMode Mode,
    string LoadMode = "Upsert",
    bool DryRun = false);

public record DataHubMigrationStartResultDto(
    Guid JobId,
    string Status,
    string Source,
    string SourceEntity,
    string TargetEntity,
    int RowCount,
    string ImportMode,
    IReadOnlyList<string> DetectedColumns);

public record DataHubMigrationQualityReportDto(
    Guid JobId,
    int DuplicateGroups,
    int ErrorCount,
    int BrokenRelations,
    int MissingOwners,
    IReadOnlyList<string> Issues,
    bool Passed);

public record MigrationExtractResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<Dictionary<string, string?>> Rows,
    string SourceEntity,
    string TargetEntity);

public interface IMigrationSourceExtractor
{
    string Source { get; }
    bool IsConfigured(TenantIntegrationConnectionSnapshot? connection);
    IReadOnlyList<DataHubMigrationEntityDto> SupportedEntities { get; }
    Task<MigrationExtractResult> ExtractAsync(
        TenantIntegrationConnectionSnapshot connection,
        string sourceEntity,
        DataHubMigrationImportMode mode,
        DateTime? sinceUtc,
        CancellationToken cancellationToken = default);
}

public record TenantIntegrationConnectionSnapshot(
    Guid TenantId,
    string Provider,
    string? AccessToken,
    string? RefreshToken,
    string? InstanceUrl,
    IReadOnlyDictionary<string, string> Settings,
    DateTime? LastSyncAt);

public static class DataHubMigrationCatalog
{
    public static readonly IReadOnlyList<string> SupportedSources =
        ["Salesforce", "HubSpot", "Dynamics", "Zoho", "Pipedrive"];

    public static string MapTargetEntity(string source, string sourceEntity) => (source, sourceEntity) switch
    {
        ("Salesforce", "Accounts") => "Customer",
        ("Salesforce", "Contacts") => "Customer",
        ("Salesforce", "Leads") => "Lead",
        ("Salesforce", "Opportunities") => "Deal",
        ("HubSpot", "Companies") => "Customer",
        ("HubSpot", "Contacts") => "Lead",
        ("HubSpot", "Deals") => "Deal",
        ("Dynamics", "Accounts") => "Customer",
        ("Dynamics", "Contacts") => "Customer",
        ("Dynamics", "Opportunities") => "Deal",
        ("Zoho", "Leads") => "Lead",
        ("Zoho", "Contacts") => "Customer",
        ("Zoho", "Accounts") => "Customer",
        ("Pipedrive", "Organizations") => "Customer",
        ("Pipedrive", "Persons") => "Lead",
        ("Pipedrive", "Deals") => "Deal",
        _ => "Customer"
    };

    public static string GetSourceDescription(string source) => source switch
    {
        "Salesforce" => "Accounts, Contacts, Leads, Opportunities",
        "HubSpot" => "Companies, Contacts, Deals",
        "Dynamics" => "Accounts, Contacts, Opportunities",
        "Zoho" => "Leads, Contacts, Accounts",
        "Pipedrive" => "Organizations, Persons, Deals",
        _ => "CRM migration"
    };
}
