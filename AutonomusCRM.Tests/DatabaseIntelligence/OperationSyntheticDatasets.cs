using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

internal static class OperationSyntheticDatasets
{
    public static List<DbOperationRowContext> DuplicateCustomers() =>
    [
        Row(1, BusinessEntityType.Customer, "customers",
            new Dictionary<string, string?> { ["name"] = "Ana Garcia", ["email"] = "dup@example.com", ["phone"] = " +507 6000 1111 " },
            DateTime.UtcNow.AddDays(-2)),
        Row(2, BusinessEntityType.Customer, "customers",
            new Dictionary<string, string?> { ["name"] = "Ana G.", ["email"] = "dup@example.com", ["phone"] = "(507) 6000-2222" },
            DateTime.UtcNow)
    ];

    public static List<DbOperationRowContext> PhoneNormalizeSet() =>
    [
        Row(1, BusinessEntityType.Customer, "customers",
            new Dictionary<string, string?> { ["name"] = "Phone User", ["email"] = "phone@example.com", ["phone"] = " +507 600 1234 " })
    ];

    public static List<DbOperationRowContext> FilterAmountSet() =>
    [
        Row(1, BusinessEntityType.Sale, "sales",
            new Dictionary<string, string?> { ["title"] = "Small", ["order_total"] = "100" }),
        Row(2, BusinessEntityType.Sale, "sales",
            new Dictionary<string, string?> { ["title"] = "Large", ["order_total"] = "5000" })
    ];

    public static List<DbOperationRowContext> ExcludeTestSet() =>
    [
        Row(1, BusinessEntityType.Customer, "customers",
            new Dictionary<string, string?> { ["name"] = "Real User", ["email"] = "real@example.com" }),
        Row(2, BusinessEntityType.Customer, "customers",
            new Dictionary<string, string?> { ["name"] = "Test User", ["email"] = "test+skip@example.com" })
    ];

    public static List<DbOperationRowContext> TransformNameSet() =>
    [
        Row(1, BusinessEntityType.Contact, "contacts",
            new Dictionary<string, string?> { ["name"] = "Maria Lopez", ["email"] = "maria@example.com" })
    ];

    public static List<DbOperationRowContext> ImportReadySet() =>
    [
        Row(1, BusinessEntityType.Customer, "customers",
            new Dictionary<string, string?> { ["name"] = "Import Ready", ["email"] = "import-ready@example.com", ["phone"] = "50760009999" })
    ];

    public static DbOperationActionPlan CleanMergeExcludeTransformImportPlan() => new(
        Filter: false,
        Clean: true,
        Merge: true,
        Enrich: false,
        Exclude: true,
        Transform: true,
        Sync: false,
        Import: false,
        FilterRules: [],
        CleanRules:
        [
            new DbOperationCleanRule("phone", DbOperationCleanAction.NormalizePhone),
            new DbOperationCleanRule("email", DbOperationCleanAction.NormalizeEmail)
        ],
        MergeRules:
        [
            new DbOperationMergeRule(BusinessEntityType.Customer, "email", DbOperationMergeStrategy.KeepNewest)
        ],
        EnrichRules: [],
        ExcludeRules:
        [
            new DbOperationExcludeRule("Test record", "email", DbOperationFilterOperator.Contains, "test+")
        ],
        TransformRules:
        [
            new DbOperationTransformRule(DbOperationTransformType.SplitFullName, "name", "first_name", "last_name")
        ]);

    public static DbOperationActionPlan FilterPlan(decimal minAmount) => new(
        Filter: true,
        Clean: false,
        Merge: false,
        Enrich: false,
        Exclude: false,
        Transform: false,
        Sync: false,
        Import: false,
        FilterRules: [new DbOperationFilterRule("order_total", DbOperationFilterOperator.GreaterThan, minAmount.ToString())],
        CleanRules: [],
        MergeRules: [],
        EnrichRules: [],
        ExcludeRules: [],
        TransformRules: []);

    public static DbOperationActionPlan ImportOnlyPlan() => new(
        Filter: false,
        Clean: false,
        Merge: false,
        Enrich: false,
        Exclude: false,
        Transform: false,
        Sync: false,
        Import: true,
        FilterRules: [],
        CleanRules: [],
        MergeRules: [],
        EnrichRules: [],
        ExcludeRules: [],
        TransformRules: []);

    private static DbOperationRowContext Row(
        int n, BusinessEntityType type, string table, Dictionary<string, string?> data, DateTime? modified = null) => new()
    {
        RowNumber = n,
        EntityType = type,
        SchemaName = "public",
        TableName = table,
        Data = data,
        SourceModifiedAtUtc = modified
    };
}
