namespace AutonomusCRM.Application.DataHub;

public enum DataHubJobStatus
{
    Uploaded,
    Parsing,
    MappingRequired,
    Validating,
    ValidationFailed,
    ReadyToImport,
    Importing,
    Completed,
    CompletedWithErrors,
    Failed,
    Cancelled,
    RolledBack,
    Paused
}

public enum DataHubTargetEntity
{
    Customer,
    Lead,
    Deal,
    User,
    WorkflowTask,
    Policy,
    Workflow
}

public enum DataHubFileFormat
{
    Csv,
    Json,
    Xlsx,
    Txt
}

public enum DataHubLoadMode
{
    InsertOnly,
    Upsert,
    UpdateExisting,
    SkipDuplicates,
    MergeDuplicates,
    DryRun
}

public enum DataHubDuplicateMatchField
{
    Email,
    Phone,
    Company,
    NameAndCompany,
    Custom
}

public enum DataHubDuplicateAction
{
    Skip,
    Update,
    Merge,
    CreateNew
}

public enum DataHubRowStatus
{
    Pending,
    Valid,
    Invalid,
    Imported,
    Skipped,
    Failed,
    RolledBack
}

public enum DataHubTransformType
{
    Trim,
    Uppercase,
    Lowercase,
    TitleCase,
    NormalizePhone,
    NormalizeEmail,
    NormalizeDate,
    NormalizeCurrency,
    MapStatus,
    MapPipelineStage,
    MapUser,
    MapCompany,
    MapCountry,
    ToEnum,
    ToDecimal,
    ToDate,
    ToBool,
    ToInt
}

public enum DataHubValidationType
{
    Required,
    Email,
    Phone,
    Date,
    Amount,
    TenantId,
    Duplicate,
    ForeignKey,
    BusinessRule,
    MaxLength,
    DataType
}

public enum DataHubMigrationSource
{
    Salesforce,
    HubSpot,
    Zoho,
    Dynamics,
    Pipedrive,
    ExcelHistorical,
    Custom
}
