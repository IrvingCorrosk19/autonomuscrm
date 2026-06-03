using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.KnowledgeGraph;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

/// <summary>Adaptador legacy — delega en <see cref="IKnowledgeGraphService"/> (ABOS Phase C).</summary>
public class BusinessKnowledgeGraphService : IBusinessKnowledgeGraphService
{
    private readonly IKnowledgeGraphService _knowledgeGraph;

    public BusinessKnowledgeGraphService(IKnowledgeGraphService knowledgeGraph)
    {
        _knowledgeGraph = knowledgeGraph;
    }

    public Task<int> RebuildGraphAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _knowledgeGraph.BuildGraphAsync(tenantId, cancellationToken);

    public async Task<KnowledgeGraphDto> GetGraphAsync(
        Guid tenantId, int maxNodes = 100, CancellationToken cancellationToken = default)
    {
        var view = await _knowledgeGraph.GetBusinessGraphAsync(tenantId, maxNodes, cancellationToken);
        return new KnowledgeGraphDto(
            view.Nodes.Select(n => new KnowledgeGraphNodeDto(n.NodeType, n.NodeId, n.Label, n.Weight)).ToList(),
            view.Edges.Select(e => new KnowledgeGraphEdgeDto(
                e.SourceType, e.SourceId, e.TargetType, e.TargetId, e.Relation, e.Weight)).ToList());
    }
}
