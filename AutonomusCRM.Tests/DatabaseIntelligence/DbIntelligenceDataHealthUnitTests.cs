using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Health;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceDataHealthUnitTests
{
    private readonly DataHealthEngine _engine = new();

    [Fact]
    public void CustomerQuality_IncompleteAndInvalidEmail_Detected()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.MixedDataset());
        Assert.Contains(result.Findings, f =>
            f.EntityType == BusinessEntityType.Customer &&
            f.Category == DataHealthFindingCategory.IncompleteData);
        Assert.Contains(result.Findings, f =>
            f.Category == DataHealthFindingCategory.InvalidFormat &&
            f.Title.Contains("correo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InvoiceQuality_OrphansDetected()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.OrphanDataset());
        Assert.Contains(result.Findings, f =>
            f.EntityType == BusinessEntityType.Invoice &&
            f.Title.Contains("sin cliente", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PaymentQuality_OrphansAndOverpayment()
    {
        var orphan = _engine.Scan(DataHealthSyntheticDatasets.OrphanDataset());
        Assert.Contains(orphan.Findings, f =>
            f.EntityType == BusinessEntityType.Payment &&
            f.Title.Contains("sin factura", StringComparison.OrdinalIgnoreCase));

        var mixed = _engine.Scan(DataHealthSyntheticDatasets.MixedDataset());
        Assert.Contains(mixed.Findings, f =>
            f.EntityType == BusinessEntityType.Payment &&
            f.Title.Contains("mayor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DuplicateDetection_EmailAndTaxId()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.DuplicateDataset());
        Assert.Contains(result.Findings, f => f.Category == DataHealthFindingCategory.Duplicate);
        Assert.True(result.Findings.Count(f => f.Category == DataHealthFindingCategory.Duplicate) >= 2);
    }

    [Fact]
    public void OrphanDetection_ContactsAndSales()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.OrphanDataset());
        Assert.Contains(result.Findings, f => f.Title.Contains("Contactos sin empresa", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Findings, f => f.Title.Contains("Ventas sin cliente", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BrokenFk_Detected()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.BrokenIntegrityDataset());
        Assert.Contains(result.Findings, f =>
            f.Category == DataHealthFindingCategory.BrokenRelationship ||
            f.Category == DataHealthFindingCategory.Orphan);
    }

    [Fact]
    public void ConsistencyRules_InvoiceTotalMismatch()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.MixedDataset());
        Assert.Contains(result.Findings, f =>
            f.Category == DataHealthFindingCategory.BusinessInconsistency &&
            f.Title.Contains("no coincide", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HealthScore_CalculatedPerEntityAndGlobal()
    {
        var healthy = _engine.Scan(DataHealthSyntheticDatasets.HealthyDataset());
        Assert.True(healthy.GlobalScore >= 75);
        Assert.NotEmpty(healthy.Scores);

        var mixed = _engine.Scan(DataHealthSyntheticDatasets.MixedDataset());
        Assert.True(mixed.GlobalScore < healthy.GlobalScore);
        Assert.All(mixed.Scores, s => Assert.InRange(s.Score, 0, 100));
    }

    [Fact]
    public void HealthFindings_HaveSeverityImpactAndRecommendation()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.MixedDataset());
        Assert.NotEmpty(result.Findings);
        Assert.All(result.Findings, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Explanation));
            Assert.False(string.IsNullOrWhiteSpace(f.BusinessImpact));
            Assert.False(string.IsNullOrWhiteSpace(f.Recommendation));
            Assert.Contains(f.Severity, new[] { DataHealthFindingSeverity.Critical, DataHealthFindingSeverity.High, DataHealthFindingSeverity.Medium, DataHealthFindingSeverity.Low });
        });
    }

    [Fact]
    public void HealthyDataset_MinimalFindings()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.HealthyDataset());
        Assert.True(result.Findings.Count <= 2);
        Assert.True(result.GlobalScore >= 80);
    }

    [Fact]
    public void IncrementalScanMode_Accepted()
    {
        var result = _engine.Scan(DataHealthSyntheticDatasets.IncrementalDataset());
        Assert.NotNull(result);
        Assert.True(result.GlobalScore >= 0);
    }

    [Fact]
    public void ProgressReporter_EmitsHealthStages()
    {
        var stages = new List<string>();
        _engine.Scan(DataHealthSyntheticDatasets.MixedDataset(),
            new SyncProgress<DataHealthProgress>(p => stages.Add(p.Stage)));
        var captured = stages.ToArray();
        Assert.Contains(DataHealthStages.ScanningCustomers, captured);
        Assert.Contains(DataHealthStages.CalculatingScore, captured);
        Assert.Contains(DataHealthStages.Completed, captured);
    }
}
