using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

internal static class SyncSyntheticDatasets
{
    public static readonly Guid TenantId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111");
    public static readonly Guid ConnectionId = Guid.Parse("eeeeeeee-ffff-0000-1111-222222222222");

    public static List<DbSyncExtractedRow> SmbDataset() =>
    [
        CustomerRow(1, "ACME Corp", "acme@example.com", "+50760001111"),
        ContactRow(2, "Maria Lopez", "maria@example.com", "+50760002222"),
        SaleRow(3, "Order-1001", "2500.00", "acme@example.com")
    ];

    public static List<DbSyncExtractedRow> EnterpriseDataset()
    {
        var rows = new List<DbSyncExtractedRow>();
        for (var i = 0; i < 25; i++)
            rows.Add(CustomerRow(i + 1, $"Enterprise Customer {i}", $"ent{i}@corp.com", null));
        return rows;
    }

    public static List<DbSyncExtractedRow> LargeDataset()
    {
        var rows = new List<DbSyncExtractedRow>();
        for (var i = 0; i < 200; i++)
            rows.Add(CustomerRow(i + 1, $"Bulk Customer {i}", $"bulk{i}@test.com", null));
        return rows;
    }

    public static List<DbSyncExtractedRow> DeltaDataset(DateTime watermark) =>
    [
        CustomerRow(1, "Delta Customer", "delta@example.com", null, watermark.AddHours(1)),
        CustomerRow(2, "Old Customer", "old@example.com", null, watermark.AddDays(-2))
    ];

    public static List<DbSyncExtractedRow> ConflictDataset() =>
    [
        CustomerRow(1, "Conflict User", "conflict@example.com", null)
    ];

    public static List<DbSyncMappingContext> DefaultMappings() =>
    [
        new(Guid.NewGuid(), "public", "customers", BusinessEntityType.Customer, DbBusinessMappingStatus.Confirmed),
        new(Guid.NewGuid(), "public", "contacts", BusinessEntityType.Contact, DbBusinessMappingStatus.Confirmed),
        new(Guid.NewGuid(), "public", "sales", BusinessEntityType.Sale, DbBusinessMappingStatus.Confirmed)
    ];

    private static DbSyncExtractedRow CustomerRow(int n, string name, string email, string? phone, DateTime? modified = null) =>
        new(BusinessEntityType.Customer, "public", "customers", n,
            new Dictionary<string, string?> { ["name"] = name, ["email"] = email, ["phone"] = phone },
            modified);

    private static DbSyncExtractedRow ContactRow(int n, string name, string email, string phone) =>
        new(BusinessEntityType.Contact, "public", "contacts", n,
            new Dictionary<string, string?> { ["name"] = name, ["email"] = email, ["phone"] = phone }, null);

    private static DbSyncExtractedRow SaleRow(int n, string title, string amount, string customerEmail) =>
        new(BusinessEntityType.Sale, "public", "sales", n,
            new Dictionary<string, string?> { ["title"] = title, ["order_total"] = amount, ["customer_email"] = customerEmail }, null);
}
