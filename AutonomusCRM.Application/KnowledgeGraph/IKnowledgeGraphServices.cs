namespace AutonomusCRM.Application.KnowledgeGraph;

public interface IKnowledgeGraphRepository
{
    Task<int> DeleteAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddEdgeAsync(EnterpriseAI.BusinessKnowledgeGraphEdge edge, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnterpriseAI.BusinessKnowledgeGraphEdge>> GetEdgesAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnterpriseAI.BusinessKnowledgeGraphEdge>> GetEdgesForCustomerAsync(
        Guid tenantId, Guid customerId, int take, CancellationToken cancellationToken = default);
    Task<int> CountEdgesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IKnowledgeGraphService
{
    Task<int> BuildGraphAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<CustomerKnowledgeGraphDto> GetCustomerGraphAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<BusinessKnowledgeGraphViewDto> GetBusinessGraphAsync(
        Guid tenantId, int maxNodes = 150, CancellationToken cancellationToken = default);
    Task<DecisionKnowledgeGraphDto?> GetDecisionGraphAsync(
        Guid tenantId, Guid decisionAuditId, CancellationToken cancellationToken = default);
    Task<OutcomeKnowledgeGraphDto?> GetOutcomeGraphAsync(
        Guid tenantId, Guid outcomeId, bool fromMemoryOutcome = true, CancellationToken cancellationToken = default);
    Task<RevenueKnowledgeGraphDto> GetRevenueGraphAsync(
        Guid tenantId, CancellationToken cancellationToken = default);
    Task<GraphSearchResultDto> SearchGraphAsync(
        Guid tenantId, string query, int take = 40, CancellationToken cancellationToken = default);
    Task LinkMemoryToDecisionAsync(
        Guid tenantId, Guid memoryId, Guid decisionAuditId, CancellationToken cancellationToken = default);
}

/// <summary>Fundación para razonamiento sobre grafo — extensible en fases futuras.</summary>
public interface IGraphReasoningFoundation
{
    Task<GraphReasoningContextDto> PrepareReasoningContextAsync(
        Guid tenantId, string scenario, CancellationToken cancellationToken = default);
}
