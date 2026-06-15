using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

internal static class BusinessDiscoverySyntheticCatalogs
{
    public static BusinessDiscoveryCatalogInput FullRetailSchema()
    {
        var catalog = new BusinessDiscoveryCatalogInput
        {
            Tables =
            [
                Table("public", "tbl_cli"),
                Table("public", "cust_master"),
                Table("public", "customer_contacts"),
                Table("public", "empresas"),
                Table("public", "inv_hdr"),
                Table("public", "facturacion"),
                Table("public", "pagos"),
                Table("public", "sales_header"),
                Table("public", "tbl_ventas"),
                Table("public", "order_lines"),
                Table("public", "products"),
                Table("public", "activities"),
                Table("public", "unknown_data")
            ],
            Columns =
            [
                Col("public", "tbl_cli", "id", isPk: true),
                Col("public", "tbl_cli", "cli_name"),
                Col("public", "tbl_cli", "cli_email"),
                Col("public", "tbl_cli", "tax_id"),
                Col("public", "cust_master", "customer_id", isPk: true),
                Col("public", "cust_master", "customer_name"),
                Col("public", "cust_master", "customer_email"),
                Col("public", "customer_contacts", "contact_id", isPk: true),
                Col("public", "customer_contacts", "first_name"),
                Col("public", "customer_contacts", "email"),
                Col("public", "customer_contacts", "phone"),
                Col("public", "customer_contacts", "customer_id", isFk: true),
                Col("public", "empresas", "empresa_id", isPk: true),
                Col("public", "empresas", "razon_social"),
                Col("public", "empresas", "tax_id"),
                Col("public", "inv_hdr", "invoice_id", isPk: true),
                Col("public", "inv_hdr", "invoice_number"),
                Col("public", "inv_hdr", "invoice_date"),
                Col("public", "inv_hdr", "total_amount"),
                Col("public", "inv_hdr", "customer_id", isFk: true),
                Col("public", "facturacion", "factura_id", isPk: true),
                Col("public", "facturacion", "factura_numero"),
                Col("public", "facturacion", "factura_fecha"),
                Col("public", "facturacion", "total_amount"),
                Col("public", "pagos", "payment_id", isPk: true),
                Col("public", "pagos", "payment_date"),
                Col("public", "pagos", "amount"),
                Col("public", "pagos", "invoice_id", isFk: true),
                Col("public", "sales_header", "order_id", isPk: true),
                Col("public", "sales_header", "order_date"),
                Col("public", "sales_header", "order_total"),
                Col("public", "sales_header", "customer_id", isFk: true),
                Col("public", "tbl_ventas", "venta_id", isPk: true),
                Col("public", "tbl_ventas", "fecha_venta"),
                Col("public", "tbl_ventas", "total_amount"),
                Col("public", "order_lines", "line_id", isPk: true),
                Col("public", "order_lines", "product_id", isFk: true),
                Col("public", "order_lines", "sku"),
                Col("public", "order_lines", "unit_price"),
                Col("public", "products", "product_id", isPk: true),
                Col("public", "products", "sku"),
                Col("public", "products", "product_name"),
                Col("public", "products", "unit_price"),
                Col("public", "activities", "activity_id", isPk: true),
                Col("public", "activities", "subject"),
                Col("public", "activities", "activity_date"),
                Col("public", "activities", "contact_id", isFk: true),
                Col("public", "unknown_data", "col_a"),
                Col("public", "unknown_data", "col_b"),
                Col("public", "unknown_data", "misc_value")
            ],
            Relationships =
            [
                Rel("public", "customer_contacts", "customer_id", "public", "tbl_cli", "id"),
                Rel("public", "inv_hdr", "customer_id", "public", "tbl_cli", "id"),
                Rel("public", "pagos", "invoice_id", "public", "inv_hdr", "invoice_id"),
                Rel("public", "sales_header", "customer_id", "public", "cust_master", "customer_id"),
                Rel("public", "order_lines", "product_id", "public", "products", "product_id"),
                Rel("public", "activities", "contact_id", "public", "customer_contacts", "contact_id")
            ]
        };

        catalog.SampleRowsByTableKey[Key("public", "tbl_cli")] =
        [
            new Dictionary<string, string?> { ["cli_email"] = "cliente@empresa.com", ["cli_name"] = "ACME", ["tax_id"] = "1234567890123" }
        ];
        catalog.SampleRowsByTableKey[Key("public", "customer_contacts")] =
        [
            new Dictionary<string, string?> { ["email"] = "maria@example.com", ["phone"] = "+50760001234" }
        ];
        catalog.SampleRowsByTableKey[Key("public", "inv_hdr")] =
        [
            new Dictionary<string, string?> { ["invoice_number"] = "INV-2024-001", ["total_amount"] = "1500.00" }
        ];
        catalog.SampleRowsByTableKey[Key("public", "products")] =
        [
            new Dictionary<string, string?> { ["sku"] = "SKU-001-A", ["product_name"] = "Widget" }
        ];

        return catalog;
    }

    public static BusinessDiscoveryCatalogInput MultilingualCustomer()
    {
        return new BusinessDiscoveryCatalogInput
        {
            Tables = [Table("dbo", "cliente_master"), Table("dbo", "customer_master")],
            Columns =
            [
                Col("dbo", "cliente_master", "cliente_id", isPk: true),
                Col("dbo", "cliente_master", "nombre_cliente"),
                Col("dbo", "cliente_master", "correo"),
                Col("dbo", "customer_master", "customer_id", isPk: true),
                Col("dbo", "customer_master", "customer_name"),
                Col("dbo", "customer_master", "email")
            ]
        };
    }

    private static BusinessDiscoveryTableInput Table(string schema, string name) => new()
    {
        SchemaName = schema,
        TableName = name,
        ObjectType = DbCatalogObjectTypes.Table
    };

    private static BusinessDiscoveryColumnInput Col(string schema, string table, string column, bool isPk = false, bool isFk = false) => new()
    {
        SchemaName = schema,
        TableName = table,
        ColumnName = column,
        DataType = "varchar",
        IsPrimaryKey = isPk,
        IsForeignKey = isFk
    };

    private static BusinessDiscoveryRelationshipInput Rel(
        string fromSchema, string fromTable, string fromColumn,
        string toSchema, string toTable, string toColumn) => new()
    {
        FromSchema = fromSchema,
        FromTable = fromTable,
        FromColumn = fromColumn,
        ToSchema = toSchema,
        ToTable = toTable,
        ToColumn = toColumn
    };

    private static string Key(string schema, string table) =>
        BusinessDiscoveryCatalogInput.TableKey(schema, table);
}
