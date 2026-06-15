using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

internal static class DataHealthSyntheticDatasets
{
    public static DataHealthScanInput HealthyDataset() => BuildBase("Healthy", new Dictionary<string, List<Dictionary<string, string?>>>
    {
        ["public.customers"] =
        [
            new() { ["id"] = "1", ["customer_name"] = "ACME Corp", ["email"] = "info@acme.com", ["tax_id"] = "1234567890123" },
            new() { ["id"] = "2", ["customer_name"] = "Beta LLC", ["email"] = "hello@beta.com", ["tax_id"] = "9876543210987" }
        ],
        ["public.invoices"] =
        [
            new() { ["invoice_id"] = "100", ["invoice_number"] = "INV-001", ["customer_id"] = "1", ["total_amount"] = "500.00" }
        ],
        ["public.payments"] =
        [
            new() { ["payment_id"] = "1", ["invoice_id"] = "100", ["amount"] = "500.00" }
        ],
        ["public.contacts"] =
        [
            new() { ["contact_id"] = "1", ["first_name"] = "Maria", ["email"] = "maria@acme.com", ["customer_id"] = "1" }
        ]
    });

    public static DataHealthScanInput DuplicateDataset() => BuildBase("Duplicates", new Dictionary<string, List<Dictionary<string, string?>>>
    {
        ["public.customers"] =
        [
            new() { ["id"] = "1", ["customer_name"] = "ACME", ["email"] = "dup@test.com", ["tax_id"] = "1111111111111" },
            new() { ["id"] = "2", ["customer_name"] = "ACME Copy", ["email"] = "dup@test.com", ["tax_id"] = "2222222222222" }
        ],
        ["public.contacts"] =
        [
            new() { ["contact_id"] = "1", ["first_name"] = "Ana", ["email"] = "ana@test.com", ["phone"] = "+50760001111", ["customer_id"] = "1" },
            new() { ["contact_id"] = "2", ["first_name"] = "Ana B", ["email"] = "ana@test.com", ["phone"] = "+50760001111", ["customer_id"] = "1" }
        ]
    });

    public static DataHealthScanInput OrphanDataset() => BuildBase("Orphans", new Dictionary<string, List<Dictionary<string, string?>>>
    {
        ["public.customers"] =
        [
            new() { ["id"] = "1", ["customer_name"] = "Only Customer", ["email"] = "c@test.com" }
        ],
        ["public.invoices"] =
        [
            new() { ["invoice_id"] = "200", ["invoice_number"] = "INV-ORPHAN", ["customer_id"] = "999", ["total_amount"] = "100.00" }
        ],
        ["public.payments"] =
        [
            new() { ["payment_id"] = "9", ["invoice_id"] = "888", ["amount"] = "50.00" }
        ],
        ["public.contacts"] =
        [
            new() { ["contact_id"] = "5", ["first_name"] = "Luis", ["email"] = "luis@test.com", ["customer_id"] = "404" }
        ],
        ["public.sales_header"] =
        [
            new() { ["order_id"] = "7", ["customer_id"] = "missing", ["order_total"] = "200.00" }
        ]
    });

    public static DataHealthScanInput BrokenIntegrityDataset() => BuildBase("BrokenFk", new Dictionary<string, List<Dictionary<string, string?>>>
    {
        ["public.customers"] =
        [
            new() { ["id"] = "1", ["customer_name"] = "Valid", ["email"] = "v@test.com" }
        ],
        ["public.invoices"] =
        [
            new() { ["invoice_id"] = "300", ["customer_id"] = "INVALID_REF", ["total_amount"] = "75.00" }
        ]
    });

    public static DataHealthScanInput MixedDataset()
    {
        var input = OrphanDataset();
        input.ScanMode = DataHealthScanMode.Full;
        var customers = input.Tables.First(t => t.TableName == "customers");
        customers.Rows = customers.Rows.Concat([
            new Dictionary<string, string?> { ["id"] = "3", ["customer_name"] = "", ["email"] = "bad-email", ["tax_id"] = "1111111111111" },
            new Dictionary<string, string?> { ["id"] = "4", ["customer_name"] = "Dup", ["email"] = "dup@test.com", ["tax_id"] = "3333333333333" },
            new Dictionary<string, string?> { ["id"] = "5", ["customer_name"] = "Dup2", ["email"] = "dup@test.com", ["tax_id"] = "4444444444444" }
        ]).ToList();

        input.Tables.Add(new DataHealthTableContext
        {
            SchemaName = "public",
            TableName = "order_lines",
            EntityType = BusinessEntityType.Product,
            Columns =
            [
                new() { ColumnName = "line_id" },
                new() { ColumnName = "invoice_id" },
                new() { ColumnName = "amount" }
            ],
            Rows =
            [
                new Dictionary<string, string?> { ["line_id"] = "1", ["invoice_id"] = "200", ["amount"] = "40.00" },
                new Dictionary<string, string?> { ["line_id"] = "2", ["invoice_id"] = "200", ["amount"] = "30.00" }
            ]
        });

        var inv = input.Tables.First(t => t.EntityType == BusinessEntityType.Invoice);
        inv.Rows = inv.Rows.Select(r =>
        {
            var copy = new Dictionary<string, string?>(r);
            if (copy.TryGetValue("invoice_id", out var id) && id == "200")
                copy["total_amount"] = "100.00";
            return (IReadOnlyDictionary<string, string?>)copy;
        }).ToList();

        input.Tables.Add(new DataHealthTableContext
        {
            SchemaName = "public",
            TableName = "payments_over",
            EntityType = BusinessEntityType.Payment,
            Columns =
            [
                new() { ColumnName = "payment_id" },
                new() { ColumnName = "invoice_id" },
                new() { ColumnName = "amount" }
            ],
            Rows =
            [
                new Dictionary<string, string?> { ["payment_id"] = "99", ["invoice_id"] = "200", ["amount"] = "150.00" }
            ]
        });

        return input;
    }

    public static DataHealthScanInput IncrementalDataset()
    {
        var input = HealthyDataset();
        input.ScanMode = DataHealthScanMode.Incremental;
        return input;
    }

    private static DataHealthScanInput BuildBase(string label, Dictionary<string, List<Dictionary<string, string?>>> tableRows)
    {
        var input = new DataHealthScanInput
        {
            TenantId = Guid.NewGuid(),
            ConnectionProfileId = Guid.NewGuid(),
            SnapshotId = Guid.NewGuid(),
            ScanMode = DataHealthScanMode.Full,
            Relationships =
            [
                new() { FromSchema = "public", FromTable = "invoices", FromColumn = "customer_id", ToSchema = "public", ToTable = "customers", ToColumn = "id", FromEntity = BusinessEntityType.Invoice, ToEntity = BusinessEntityType.Customer },
                new() { FromSchema = "public", FromTable = "payments", FromColumn = "invoice_id", ToSchema = "public", ToTable = "invoices", ToColumn = "invoice_id", FromEntity = BusinessEntityType.Payment, ToEntity = BusinessEntityType.Invoice },
                new() { FromSchema = "public", FromTable = "contacts", FromColumn = "customer_id", ToSchema = "public", ToTable = "customers", ToColumn = "id", FromEntity = BusinessEntityType.Contact, ToEntity = BusinessEntityType.Customer },
                new() { FromSchema = "public", FromTable = "sales_header", FromColumn = "customer_id", ToSchema = "public", ToTable = "customers", ToColumn = "id", FromEntity = BusinessEntityType.Sale, ToEntity = BusinessEntityType.Customer },
                new() { FromSchema = "public", FromTable = "order_lines", FromColumn = "invoice_id", ToSchema = "public", ToTable = "invoices", ToColumn = "invoice_id", FromEntity = BusinessEntityType.Product, ToEntity = BusinessEntityType.Invoice }
            ]
        };

        foreach (var (key, rows) in tableRows)
        {
            var parts = key.Split('.');
            var schema = parts[0];
            var table = parts[1];
            var entity = EntityForTable(table);
            var cols = rows.SelectMany(r => r.Keys).Distinct()
                .Select(c => new DataHealthColumnContext { ColumnName = c }).ToList();

            input.Tables.Add(new DataHealthTableContext
            {
                SchemaName = schema,
                TableName = table,
                EntityType = entity,
                Columns = cols,
                Rows = rows.Select(r => (IReadOnlyDictionary<string, string?>)r).ToList()
            });
        }

        _ = label;
        return input;
    }

    private static BusinessEntityType EntityForTable(string table) => table switch
    {
        "customers" or "tbl_cli" => BusinessEntityType.Customer,
        "contacts" or "customer_contacts" => BusinessEntityType.Contact,
        "empresas" => BusinessEntityType.Company,
        "invoices" or "inv_hdr" => BusinessEntityType.Invoice,
        "payments" or "payments_over" or "pagos" => BusinessEntityType.Payment,
        "products" or "order_lines" => BusinessEntityType.Product,
        "sales_header" => BusinessEntityType.Sale,
        _ => BusinessEntityType.Unknown
    };
}
