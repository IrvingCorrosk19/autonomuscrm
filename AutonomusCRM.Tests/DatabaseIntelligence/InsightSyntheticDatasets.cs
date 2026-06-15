using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

internal static class InsightSyntheticDatasets
{
    private static readonly Guid ConnectionId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid SnapshotId = Guid.Parse("22222222-3333-4444-5555-666666666666");
    private static readonly Guid TenantId = Guid.Parse("33333333-4444-5555-6666-777777777777");

    public static DbIntelligenceInsightBuildInput DemoDataset() => new()
    {
        TenantId = TenantId,
        ConnectionProfileId = ConnectionId,
        SnapshotId = SnapshotId,
        GlobalHealthScore = 68,
        ConfirmedMappings =
        [
            Mapping("tbl_cli", BusinessEntityType.Customer, 92, 5200),
            Mapping("cust_master", BusinessEntityType.Customer, 88, 3100),
            Mapping("empresas", BusinessEntityType.Company, 90, 450),
            Mapping("customer_contacts", BusinessEntityType.Contact, 88, 3400),
            Mapping("sales_header", BusinessEntityType.Sale, 90, 890),
            Mapping("inv_hdr", BusinessEntityType.Invoice, 94, 850),
            Mapping("pagos", BusinessEntityType.Payment, 91, 820)
        ],
        UnmappedTables =
        [
            Unmapped("facturacion_legacy", BusinessEntityType.Invoice, 82,
                ["Column invoice_number matches invoice pattern", "Amount and date columns present"]),
            Unmapped("tbl_unknown_staging", BusinessEntityType.Product, 72,
                ["SKU-like column detected", "Price column present"])
        ],
        CatalogTables =
        [
            Catalog("tbl_cli", 5200, 4, 2, 12, 18, true, true),
            Catalog("cust_master", 3100, 3, 1, 10, 16, true, true),
            Catalog("empresas", 450, 2, 1, 8, 14, false, true),
            Catalog("customer_contacts", 3400, 2, 1, 9, 15, true, true),
            Catalog("sales_header", 890, 1, 1, 11, 20, true, true),
            Catalog("inv_hdr", 850, 2, 1, 10, 17, true, true),
            Catalog("pagos", 820, 1, 1, 9, 14, true, true),
            Catalog("legacy_archive", 12000, 0, 0, 20, 20, false, false),
            Catalog("sparse_import", 800, 0, 0, 18, 20, false, true),
            Catalog("no_timestamp_bulk", 5000, 1, 0, 6, 8, false, true)
        ],
        Relationships =
        [
            Rel("customer_contacts", "tbl_cli"),
            Rel("sales_header", "tbl_cli"),
            Rel("inv_hdr", "tbl_cli"),
            Rel("pagos", "inv_hdr"),
            Rel("sales_header", "cust_master")
        ],
        HealthScores =
        [
            Score(BusinessEntityType.Customer, 62),
            Score(BusinessEntityType.Invoice, 70),
            Score(BusinessEntityType.Payment, 75)
        ],
        HealthFindings =
        [
            Finding(BusinessEntityType.Customer, DataHealthFindingSeverity.High, DataHealthFindingCategory.Duplicate,
                "Duplicate customers inflate revenue", 18),
            Finding(BusinessEntityType.Invoice, DataHealthFindingSeverity.Critical, DataHealthFindingCategory.Orphan,
                "Invoices without customer", 8),
            Finding(BusinessEntityType.Payment, DataHealthFindingSeverity.High, DataHealthFindingCategory.Orphan,
                "Payments without invoice", 6),
            Finding(BusinessEntityType.Invoice, DataHealthFindingSeverity.Medium, DataHealthFindingCategory.BusinessInconsistency,
                "Invoice total mismatch", 4)
        ]
    };

    public static DbIntelligenceInsightBuildInput MinimalDataset() => new()
    {
        TenantId = TenantId,
        ConnectionProfileId = ConnectionId,
        SnapshotId = SnapshotId,
        ConfirmedMappings = [Mapping("customers", BusinessEntityType.Customer, 90, 100)],
        CatalogTables = [Catalog("customers", 100, 0, 0, 2, 5, true, true)],
        HealthScores = [Score(BusinessEntityType.Customer, 95)]
    };

    private static DbBusinessGraphMappingContext Mapping(
        string table, BusinessEntityType entity, int confidence, long rows) => new()
    {
        MappingId = Guid.NewGuid(),
        SchemaName = "public",
        TableName = table,
        DisplayName = table,
        EntityType = entity,
        ConfidencePercent = confidence,
        Status = DbBusinessMappingStatus.Confirmed,
        EstimatedRowCount = rows
    };

    private static DbIntelligenceUnmappedTableContext Unmapped(
        string table, BusinessEntityType entity, int confidence, List<string> reasons) => new()
    {
        SchemaName = "public",
        TableName = table,
        InferredEntityType = entity,
        ConfidencePercent = confidence,
        Status = DbBusinessMappingStatus.Inferred,
        EstimatedRowCount = 500,
        InferenceReasons = reasons
    };

    private static DbIntelligenceCatalogTableContext Catalog(
        string table, long rows, int incoming, int outgoing,
        int nullable, int total, bool hasUpdatedAt, bool mapped) => new()
    {
        SchemaName = "public",
        TableName = table,
        EstimatedRowCount = rows,
        IncomingFkCount = incoming,
        OutgoingFkCount = outgoing,
        NullableColumnCount = nullable,
        TotalColumnCount = total,
        HasUpdatedAtColumn = hasUpdatedAt,
        IsMapped = mapped
    };

    private static DbBusinessGraphRelationshipContext Rel(string from, string to) => new()
    {
        FromSchema = "public",
        FromTable = from,
        FromColumn = "fk_id",
        ToSchema = "public",
        ToTable = to,
        ToColumn = "id",
        ConfidencePercent = 100
    };

    private static DataHealthScoreDto Score(BusinessEntityType entity, int score) => new(
        entity, score, DataHealthScoreBand.Label(score), score, score, score, score);

    private static DataHealthFindingDto Finding(
        BusinessEntityType entity, string severity, string category, string title, int count) => new(
        Guid.NewGuid(), entity, severity, category, title,
        "Quality issue detected in sampled records",
        "Revenue reporting and CRM sync may be inaccurate",
        $"{count} records in sample",
        "Clean duplicates and fix relationships before sync",
        "public", "table", count);
}
