namespace AutonomusCRM.Application.DataHub;

public static class DataHubFieldCatalogDefaults
{
    public static IReadOnlyList<DataHubFieldDefinition> CustomerFields { get; } =
    [
        new("Name", "Name", true, "string", 200),
        new("Email", "Email", false, "email", 255),
        new("Phone", "Phone", false, "phone", 50),
        new("Company", "Company", false, "string", 200),
        new("Status", "Status", false, "enum", null)
    ];

    public static IReadOnlyList<DataHubFieldDefinition> LeadFields { get; } =
    [
        new("Name", "Name", true, "string", 200),
        new("Source", "Source", false, "enum", null),
        new("Email", "Email", false, "email", 255),
        new("Phone", "Phone", false, "phone", 50),
        new("Company", "Company", false, "string", 200)
    ];

    public static IReadOnlyList<DataHubFieldDefinition> DealFields { get; } =
    [
        new("Title", "Title", true, "string", 200),
        new("Amount", "Amount", true, "decimal", null),
        new("Stage", "Stage", false, "enum", null),
        new("ExpectedCloseDate", "Expected Close Date", false, "date", null),
        new("CustomerEmail", "Customer Email", false, "email", 255),
        new("Description", "Description", false, "string", 2000)
    ];

    public static IReadOnlyList<DataHubFieldDefinition> UserFields { get; } =
    [
        new("Email", "Email", true, "email", 255),
        new("Password", "Password", true, "string", 128),
        new("FirstName", "First Name", false, "string", 100),
        new("LastName", "Last Name", false, "string", 100)
    ];

    public static IReadOnlyList<DataHubFieldDefinition> WorkflowTaskFields { get; } =
    [
        new("Title", "Title", true, "string", 300),
        new("WorkflowId", "Workflow ID", true, "guid", null),
        new("Status", "Status", false, "string", 50),
        new("Priority", "Priority", false, "string", 50),
        new("AssignedToUserId", "Assigned User ID", false, "guid", null)
    ];

    public static IReadOnlyList<DataHubFieldDefinition> PolicyFields { get; } =
    [
        new("Name", "Name", true, "string", 200),
        new("Expression", "Expression", true, "string", 2000),
        new("Description", "Description", false, "string", 1000)
    ];

    public static IReadOnlyList<DataHubFieldDefinition> WorkflowFields { get; } =
    [
        new("Name", "Name", true, "string", 200),
        new("Description", "Description", false, "string", 1000)
    ];
}

public static class DataHubConstants
{
    public const long MaxFileBytes = 100 * 1024 * 1024;
    public const int DefaultBatchSize = 1000;
    public const int MaxPreviewRows = 25;
    public const int StagingInsertBatch = 500;
    /// <summary>Rows per PostgreSQL COPY batch (P2 enterprise staging).</summary>
    public const int CopyBatchSize = 5000;
    /// <summary>Rows parsed per chunk from large CSV files.</summary>
    public const int ExtractChunkSize = 5000;
    /// <summary>Rows fetched per DB batch during streaming export.</summary>
    public const int ExportStreamBatchSize = 5000;
    /// <summary>CSV files above this size use chunked extract + COPY path.</summary>
    public const long LargeFileChunkThresholdBytes = 512 * 1024;
    public static readonly string[] AllowedExtensions = [".csv", ".json", ".xlsx", ".xls", ".txt"];
    public static readonly string[] AllowedMimeTypes =
    [
        "text/csv", "application/json", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel", "text/plain", "application/octet-stream"
    ];
}
