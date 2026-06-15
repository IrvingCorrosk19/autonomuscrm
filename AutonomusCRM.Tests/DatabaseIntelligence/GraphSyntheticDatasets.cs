using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

internal static class GraphSyntheticDatasets
{
    private static readonly Guid ConnectionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid SnapshotId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
    private static readonly Guid TenantId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");

    public static DbBusinessGraphBuildInput SmbDataset() => Build(
        [
            Mapping("tbl_cli", BusinessEntityType.Customer, 92, 1200),
            Mapping("customer_contacts", BusinessEntityType.Contact, 88, 3400),
            Mapping("sales_header", BusinessEntityType.Sale, 90, 890),
            Mapping("inv_hdr", BusinessEntityType.Invoice, 94, 850),
            Mapping("pagos", BusinessEntityType.Payment, 91, 820)
        ],
        [
            Rel("customer_contacts", "tbl_cli"),
            Rel("sales_header", "tbl_cli"),
            Rel("inv_hdr", "tbl_cli"),
            Rel("pagos", "inv_hdr")
        ],
        healthScores: [(BusinessEntityType.Customer, 91), (BusinessEntityType.Invoice, 88)]);

    public static DbBusinessGraphBuildInput EnterpriseDataset() => Build(
        [
            Mapping("empresas", BusinessEntityType.Company, 95, 450),
            Mapping("cust_master", BusinessEntityType.Customer, 93, 12000),
            Mapping("customer_contacts", BusinessEntityType.Contact, 90, 28000),
            Mapping("tbl_ventas", BusinessEntityType.Sale, 89, 45000),
            Mapping("facturacion", BusinessEntityType.Invoice, 92, 44000),
            Mapping("pagos", BusinessEntityType.Payment, 90, 43000),
            Mapping("products", BusinessEntityType.Product, 87, 3200),
            Mapping("activities", BusinessEntityType.Activity, 85, 15000)
        ],
        [
            Rel("customer_contacts", "cust_master"),
            Rel("tbl_ventas", "cust_master"),
            Rel("facturacion", "cust_master"),
            Rel("pagos", "facturacion"),
            Rel("activities", "customer_contacts")
        ],
        healthScores:
        [
            (BusinessEntityType.Company, 94),
            (BusinessEntityType.Customer, 91),
            (BusinessEntityType.Invoice, 89),
            (BusinessEntityType.Payment, 90)
        ]);

    public static DbBusinessGraphBuildInput MixedDataset() => Build(
        [
            Mapping("tbl_cli", BusinessEntityType.Customer, 78, 500),
            Mapping("inv_hdr", BusinessEntityType.Invoice, 82, 480),
            Mapping("pagos", BusinessEntityType.Payment, 80, 470),
            Mapping("products", BusinessEntityType.Product, 75, 200)
        ],
        [
            Rel("pagos", "inv_hdr")
        ],
        healthScores: [(BusinessEntityType.Customer, 62), (BusinessEntityType.Invoice, 70)],
        findings:
        [
            Finding(BusinessEntityType.Customer, DataHealthFindingSeverity.High, DataHealthFindingCategory.Duplicate, "Duplicate customers", 12),
            Finding(BusinessEntityType.Invoice, DataHealthFindingSeverity.Medium, DataHealthFindingCategory.Orphan, "Invoices without customer", 5)
        ]);

    public static DbBusinessGraphBuildInput BrokenRelationshipDataset() => Build(
        [
            Mapping("tbl_cli", BusinessEntityType.Customer, 85, 100),
            Mapping("inv_hdr", BusinessEntityType.Invoice, 80, 95),
            Mapping("pagos", BusinessEntityType.Payment, 78, 90)
        ],
        [
            Rel("inv_hdr", "missing_customers"),
            Rel("pagos", "missing_invoices")
        ],
        healthScores: [(BusinessEntityType.Invoice, 55), (BusinessEntityType.Payment, 48)],
        findings:
        [
            Finding(BusinessEntityType.Invoice, DataHealthFindingSeverity.Critical, DataHealthFindingCategory.Orphan, "Invoices without customer", 8),
            Finding(BusinessEntityType.Payment, DataHealthFindingSeverity.Critical, DataHealthFindingCategory.Orphan, "Payments without invoice", 6)
        ]);

    public static DbBusinessGraphBuildInput LargeDataset()
    {
        var mappings = new List<DbBusinessGraphMappingContext>();
        var relationships = new List<DbBusinessGraphRelationshipContext>();
        for (var i = 0; i < 50; i++)
        {
            mappings.Add(Mapping($"customer_{i}", BusinessEntityType.Customer, 80 + i % 15, 1000 + i * 10));
            mappings.Add(Mapping($"invoice_{i}", BusinessEntityType.Invoice, 75 + i % 20, 900 + i * 8));
            relationships.Add(Rel($"invoice_{i}", $"customer_{i % 10}"));
        }

        return Build(mappings, relationships);
    }

    private static DbBusinessGraphBuildInput Build(
        IReadOnlyList<DbBusinessGraphMappingContext> mappings,
        IReadOnlyList<DbBusinessGraphRelationshipContext> relationships,
        IReadOnlyList<(BusinessEntityType Entity, int Score)>? healthScores = null,
        IReadOnlyList<DataHealthFindingDto>? findings = null) => new()
    {
        TenantId = TenantId,
        ConnectionProfileId = ConnectionId,
        SnapshotId = SnapshotId,
        Mappings = mappings.ToList(),
        Relationships = relationships.ToList(),
        HealthScores = (healthScores ?? []).Select(h => new DataHealthScoreDto(
            h.Entity, h.Score, DataHealthScoreBand.Label(h.Score), h.Score, h.Score, h.Score, h.Score)).ToList(),
        HealthFindings = findings?.ToList() ?? []
    };

    private static DbBusinessGraphMappingContext Mapping(string table, BusinessEntityType entity, int confidence, long rows) => new()
    {
        MappingId = Guid.NewGuid(),
        SchemaName = "public",
        TableName = table,
        DisplayName = EntityLabel(entity),
        EntityType = entity,
        ConfidencePercent = confidence,
        EstimatedRowCount = rows
    };

    private static DbBusinessGraphRelationshipContext Rel(string fromTable, string toTable) => new()
    {
        FromSchema = "public",
        FromTable = fromTable,
        FromColumn = "fk_id",
        ToSchema = "public",
        ToTable = toTable,
        ToColumn = "id",
        ConfidencePercent = 100
    };

    private static DataHealthFindingDto Finding(
        BusinessEntityType entity, string severity, string category, string title, int count) => new(
        Guid.NewGuid(), entity, severity, category, title,
        "Explanation", "Business impact", "Evidence", "Recommendation",
        "public", "table", count);

    private static string EntityLabel(BusinessEntityType type) => type switch
    {
        BusinessEntityType.Customer => "Customers",
        BusinessEntityType.Company => "Companies",
        BusinessEntityType.Contact => "Contacts",
        BusinessEntityType.Sale => "Sales",
        BusinessEntityType.Invoice => "Invoices",
        BusinessEntityType.Payment => "Payments",
        BusinessEntityType.Product => "Products",
        BusinessEntityType.Activity => "Activities",
        _ => type.ToString()
    };
}
