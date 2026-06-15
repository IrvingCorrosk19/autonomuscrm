using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Insights;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceInsightUnitTests
{
    private readonly DbIntelligenceInsightEngine _engine = new();

    [Fact]
    public void DemoDataset_GeneratesAtLeastEightInsights()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        Assert.True(insights.Count >= 8);
    }

    [Fact]
    public void CriticalTableInsight_DetectsHighFanOutLowHealth()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        Assert.Contains(insights, i =>
            i.Type == DbIntelligenceInsightType.CriticalTable &&
            i.Category == DbIntelligenceInsightCategory.Risk);
    }

    [Fact]
    public void UnusedDataInsight_DetectsOrphanAndSparseTables()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        Assert.Contains(insights, i =>
            i.Type == DbIntelligenceInsightType.UnusedData &&
            i.Title.Contains("legacy_archive", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(insights, i =>
            i.Type == DbIntelligenceInsightType.UnusedData &&
            i.Title.Contains("sparse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MigrationOpportunity_SuggestsCustomerUnification()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        var migration = Assert.Single(insights.Where(i => i.Type == DbIntelligenceInsightType.MigrationOpportunity));
        Assert.Equal(DbIntelligenceInsightCategory.Opportunity, migration.Category);
        Assert.Contains("customer", migration.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QualityRisk_MapsHealthFindings()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        var risks = insights.Where(i => i.Type == DbIntelligenceInsightType.QualityRisk).ToList();
        Assert.True(risks.Count >= 4);
        Assert.Contains(risks, r => r.Title.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(risks, r => r.Title.Contains("without customer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnmappedEntity_SurfacesInferredTables()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        Assert.Contains(insights, i =>
            i.Type == DbIntelligenceInsightType.UnmappedEntity &&
            i.TableName == "facturacion_legacy");
    }

    [Fact]
    public void Explainability_ProvidesReasonsForEachInsight()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        Assert.All(insights, i =>
        {
            Assert.NotEmpty(i.ExplainabilityReasons);
            Assert.NotEmpty(i.SuggestedAction);
            Assert.NotEmpty(i.Evidence);
        });
    }

    [Fact]
    public void Insights_ArePrioritizedByImpactAndConfidence()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset()).ToList();
        for (var i = 1; i < insights.Count; i++)
            Assert.True(insights[i - 1].PriorityScore >= insights[i].PriorityScore - 5);
    }

    [Fact]
    public void SemanticMatch_ComputesLocalTableSimilarity()
    {
        var score = DbIntelligenceInsightEngine.ComputeLocalSemanticMatch("tbl_cli", BusinessEntityType.Customer);
        Assert.True(score >= 60);
    }

    [Fact]
    public void Progress_StagesReported()
    {
        var stages = new List<string>();
        _engine.Generate(InsightSyntheticDatasets.MinimalDataset(), new SyncProgress<DbIntelligenceInsightProgress>(p =>
        {
            stages.Add(p.Stage);
        }));
        Assert.Contains(DbIntelligenceInsightStages.Completed, stages);
    }

    [Fact]
    public void AllFiveInsightTypes_RepresentedInDemo()
    {
        var insights = _engine.Generate(InsightSyntheticDatasets.DemoDataset());
        Assert.Contains(insights, i => i.Type == DbIntelligenceInsightType.CriticalTable);
        Assert.Contains(insights, i => i.Type == DbIntelligenceInsightType.UnusedData);
        Assert.Contains(insights, i => i.Type == DbIntelligenceInsightType.MigrationOpportunity);
        Assert.Contains(insights, i => i.Type == DbIntelligenceInsightType.QualityRisk);
        Assert.Contains(insights, i => i.Type == DbIntelligenceInsightType.UnmappedEntity);
    }
}
