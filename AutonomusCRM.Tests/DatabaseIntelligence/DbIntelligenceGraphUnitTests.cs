using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Graph;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Health;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceGraphUnitTests
{
    private readonly DataHealthEngine _healthEngine = new();
    private readonly DbBusinessGraphBuilder _builder = new();

    [Fact]
    public void GraphBuild_SmbDataset_ProducesBusinessFlow()
    {
        var input = EnrichWithHealth(GraphSyntheticDatasets.SmbDataset());
        var graph = _builder.Build(input);
        Assert.True(graph.Nodes.Count >= 4);
        Assert.True(graph.Edges.Count >= 3);
        Assert.Contains(graph.Nodes, n => n.EntityType == BusinessEntityType.Customer);
        Assert.Contains(graph.Nodes, n => n.EntityType == BusinessEntityType.Invoice);
        Assert.Contains(graph.Nodes, n => n.EntityType == BusinessEntityType.Payment);
    }

    [Fact]
    public void NodeCreation_IncludesConfidenceAndSources()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.EnterpriseDataset());
        var customer = graph.Nodes.First(n => n.EntityType == BusinessEntityType.Customer);
        Assert.True(customer.ConfidencePercent >= 85);
        Assert.NotEmpty(customer.Sources);
        Assert.All(customer.Sources, s => Assert.True(s.ConfidencePercent > 0));
    }

    [Fact]
    public void EdgeCreation_HasBusinessLabels()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.SmbDataset());
        Assert.Contains(graph.Edges, e => e.EdgeType == DbBusinessGraphEdgeTypes.GeneratedPayment);
        Assert.All(graph.Edges, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.BusinessLabel));
            Assert.InRange(e.ConfidencePercent, 0, 100);
        });
    }

    [Fact]
    public void Confidence_AggregatedPerNode()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.EnterpriseDataset());
        var company = graph.Nodes.First(n => n.EntityType == BusinessEntityType.Company);
        Assert.Equal(95, company.ConfidencePercent);
    }

    [Fact]
    public void HealthIntegration_AttachesScoresAndFindings()
    {
        var input = GraphSyntheticDatasets.MixedDataset();
        var graph = _builder.Build(input);
        var customer = graph.Nodes.First(n => n.EntityType == BusinessEntityType.Customer);
        Assert.Equal(62, customer.HealthScore);
        Assert.Equal("Fair", customer.HealthBand);
        Assert.Contains(customer.TopFindings, f => f.Category == DataHealthFindingCategory.Duplicate);
    }

    [Fact]
    public void Metrics_RecordCountsAndDuplicates()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.MixedDataset());
        var customer = graph.Nodes.First(n => n.EntityType == BusinessEntityType.Customer);
        Assert.True(customer.RecordCount > 0);
        Assert.True(customer.DuplicateCount > 0);
        Assert.True(graph.Summary.TotalRecords > 0);
    }

    [Fact]
    public void GraphExport_PngPdfSnapshot_ProduceContent()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.SmbDataset());
        var png = DbBusinessGraphExporter.ExportPng(graph);
        var pdf = DbBusinessGraphExporter.ExportPdf(graph);
        var snap = DbBusinessGraphExporter.ExportSnapshot(graph);
        Assert.NotNull(png.Content);
        Assert.True(png.Content!.Length > 100);
        Assert.NotNull(pdf.Content);
        Assert.True(pdf.Content!.Length > 50);
        Assert.NotNull(snap.SnapshotJson);
        Assert.Contains("Customers", snap.SnapshotJson!, StringComparison.Ordinal);
    }

    [Fact]
    public void BrokenRelationships_StillBuildsCanonicalFlow()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.BrokenRelationshipDataset());
        Assert.Contains(graph.Nodes, n => n.EntityType == BusinessEntityType.Invoice);
        Assert.Contains(graph.Edges, e =>
            e.FromEntityType == BusinessEntityType.Invoice &&
            e.ToEntityType == BusinessEntityType.Payment);
        var invoice = graph.Nodes.First(n => n.EntityType == BusinessEntityType.Invoice);
        Assert.Equal("Critical", invoice.RiskLevel);
    }

    [Fact]
    public void SummaryGeneration_BusinessViewMessage()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.EnterpriseDataset());
        Assert.Contains("business", graph.Summary.BusinessViewMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(graph.Summary.NodeCount >= 6);
        Assert.True(graph.Summary.GlobalHealthScore > 0);
    }

    [Fact]
    public void LargeDataset_BuildsWithinReasonableNodeCount()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.LargeDataset());
        Assert.Equal(2, graph.Nodes.Count);
        Assert.True(graph.Summary.TotalRecords > 1000);
    }

    [Fact]
    public void ProgressReporter_EmitsGraphStages()
    {
        var stages = new List<string>();
        _builder.Build(GraphSyntheticDatasets.SmbDataset(),
            new SyncProgress<DbBusinessGraphProgress>(p => stages.Add(p.Stage)));
        var captured = stages.ToArray();
        Assert.Contains(DbBusinessGraphStages.BuildingGraph, captured);
        Assert.Contains(DbBusinessGraphStages.CreatingNodes, captured);
        Assert.Contains(DbBusinessGraphStages.Completed, captured);
    }

    [Fact]
    public void BusinessView_NoTechnicalTableNamesInLabels()
    {
        var graph = _builder.Build(GraphSyntheticDatasets.SmbDataset());
        Assert.All(graph.Nodes, n =>
        {
            Assert.DoesNotContain("tbl_", n.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("inv_hdr", n.Label, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static DbBusinessGraphBuildInput EnrichWithHealth(DbBusinessGraphBuildInput input)
    {
        var health = new DataHealthEngine().Scan(DataHealthSyntheticDatasets.HealthyDataset());
        input.HealthScores = health.Scores;
        input.HealthFindings = health.Findings;
        return input;
    }
}
