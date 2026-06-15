using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceBusinessDiscoveryUnitTests
{
    private readonly BusinessEntityInferenceEngine _engine = new();

    [Fact]
    public void CustomerInference_tbl_cli_and_cust_master()
    {
        var catalog = BusinessDiscoverySyntheticCatalogs.FullRetailSchema();
        var results = _engine.InferFromCatalog(catalog);
        AssertEntity(results, "public", "tbl_cli", BusinessEntityType.Customer, 70);
        AssertEntity(results, "public", "cust_master", BusinessEntityType.Customer, 70);
    }

    [Fact]
    public void ContactInference_customer_contacts()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        AssertEntity(results, "public", "customer_contacts", BusinessEntityType.Contact, 70);
    }

    [Fact]
    public void CompanyInference_empresas()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        AssertEntity(results, "public", "empresas", BusinessEntityType.Company, 65);
    }

    [Fact]
    public void InvoiceInference_inv_hdr_and_facturacion()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        AssertEntity(results, "public", "inv_hdr", BusinessEntityType.Invoice, 75);
        AssertEntity(results, "public", "facturacion", BusinessEntityType.Invoice, 70);
    }

    [Fact]
    public void PaymentInference_pagos()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        AssertEntity(results, "public", "pagos", BusinessEntityType.Payment, 70);
    }

    [Fact]
    public void SaleInference_sales_header_and_tbl_ventas()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        AssertEntity(results, "public", "sales_header", BusinessEntityType.Sale, 70);
        AssertEntity(results, "public", "tbl_ventas", BusinessEntityType.Sale, 70);
    }

    [Fact]
    public void ProductInference_products_and_order_lines()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        AssertEntity(results, "public", "products", BusinessEntityType.Product, 70);
        AssertEntity(results, "public", "order_lines", BusinessEntityType.Product, 55);
    }

    [Fact]
    public void ActivityInference_activities()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        AssertEntity(results, "public", "activities", BusinessEntityType.Activity, 70);
    }

    [Fact]
    public void MultilanguageInference_cliente_and_customer_master()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.MultilingualCustomer());
        AssertEntity(results, "dbo", "cliente_master", BusinessEntityType.Customer, 70);
        AssertEntity(results, "dbo", "customer_master", BusinessEntityType.Customer, 70);
    }

    [Fact]
    public void ConfidenceScoring_unknown_table_low_confidence()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        var unknown = Assert.Single(results, r => r.TableName == "unknown_data");
        Assert.Equal(BusinessEntityType.Unknown, unknown.EntityType);
        Assert.InRange(unknown.ConfidencePercent, 35, 55);
    }

    [Fact]
    public void Explainability_invoice_contains_reasons()
    {
        var results = _engine.InferFromCatalog(BusinessDiscoverySyntheticCatalogs.FullRetailSchema());
        var invoice = Assert.Single(results, r => r.TableName == "inv_hdr");
        Assert.NotEmpty(invoice.Reasons);
        Assert.Contains(invoice.Reasons, r => r.Contains("invoice", StringComparison.OrdinalIgnoreCase) ||
                                              r.Contains("factura", StringComparison.OrdinalIgnoreCase) ||
                                              r.Contains("columna", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProgressReporter_emits_stages()
    {
        var stages = new List<string>();
        _engine.InferFromCatalog(
            BusinessDiscoverySyntheticCatalogs.FullRetailSchema(),
            new SyncProgress<BusinessDiscoveryProgress>(p => stages.Add(p.Stage)));

        var captured = stages.ToArray();
        Assert.Contains(BusinessDiscoveryStages.AnalyzingTables, captured);
        Assert.Contains(BusinessDiscoveryStages.AnalyzingColumns, captured);
        Assert.Contains(BusinessDiscoveryStages.Completed, captured);
    }

    [Fact]
    public void SampleDataAnalysis_boosts_contact_with_email_pattern()
    {
        var catalog = new BusinessDiscoveryCatalogInput
        {
            Tables = [new BusinessDiscoveryTableInput { SchemaName = "public", TableName = "crm_contacts" }],
            Columns =
            [
                new BusinessDiscoveryColumnInput { SchemaName = "public", TableName = "crm_contacts", ColumnName = "email" },
                new BusinessDiscoveryColumnInput { SchemaName = "public", TableName = "crm_contacts", ColumnName = "phone" }
            ]
        };
        catalog.SampleRowsByTableKey[BusinessDiscoveryCatalogInput.TableKey("public", "crm_contacts")] =
        [
            new Dictionary<string, string?> { ["email"] = "user@company.com", ["phone"] = "+50761234567" }
        ];

        var result = Assert.Single(_engine.InferFromCatalog(catalog));
        Assert.Equal(BusinessEntityType.Contact, result.EntityType);
        Assert.True(result.ConfidencePercent >= 70);
    }

    private static void AssertEntity(
        IReadOnlyList<BusinessEntityInferenceResult> results,
        string schema,
        string table,
        BusinessEntityType expected,
        int minConfidence)
    {
        var match = Assert.Single(results, r =>
            r.SchemaName == schema && r.TableName.Equals(table, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(expected, match.EntityType);
        Assert.True(match.ConfidencePercent >= minConfidence,
            $"{table} expected >={minConfidence}% but was {match.ConfidencePercent}% ({match.EntityType})");
    }
}
