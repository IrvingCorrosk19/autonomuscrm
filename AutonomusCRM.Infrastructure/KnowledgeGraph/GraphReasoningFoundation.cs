using AutonomusCRM.Application.KnowledgeGraph;

namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

public sealed class GraphReasoningFoundation : IGraphReasoningFoundation
{
    private readonly IKnowledgeGraphService _graph;
    private readonly IGraphReasoningEngine _reasoning;

    public GraphReasoningFoundation(IKnowledgeGraphService graph, IGraphReasoningEngine reasoning)
    {
        _graph = graph;
        _reasoning = reasoning;
    }

    public async Task<GraphReasoningContextDto> PrepareReasoningContextAsync(
        Guid tenantId, string scenario, CancellationToken cancellationToken = default)
    {
        var view = await _graph.GetBusinessGraphAsync(tenantId, 50, cancellationToken);
        return new GraphReasoningContextDto(
            tenantId,
            scenario,
            GraphReady: view.EdgeCount > 0,
            NodeCount: view.Nodes.Count,
            EdgeCount: view.EdgeCount,
            PreparedCapabilities: new[]
            {
                "IGraphReasoningEngine.ExplainCustomerRiskAsync",
                "IGraphReasoningEngine.ExplainDecisionAsync",
                "IDecisionIntelligenceEngine.AnalyzeCustomerDecisionAsync",
                "IBusinessSimulationEngine.RunScenarioAsync",
                "DecisionEngine.GraphContext",
                "BusinessSimulation.PathReplay",
                "AutonomousWorkforce.AgentAccountability"
            },
            Notes: "Phase D — reasoning engines active; simulation uses historical learnings only.");
    }
}
