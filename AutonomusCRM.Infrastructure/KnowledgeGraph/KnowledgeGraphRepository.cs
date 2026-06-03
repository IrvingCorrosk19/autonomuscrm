using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

public sealed class KnowledgeGraphRepository : IKnowledgeGraphRepository
{
    private readonly ApplicationDbContext _db;

    public KnowledgeGraphRepository(ApplicationDbContext db) => _db = db;

    public async Task<int> DeleteAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var edges = await _db.BusinessKnowledgeGraphEdges.Where(e => e.TenantId == tenantId).ToListAsync(cancellationToken);
        _db.BusinessKnowledgeGraphEdges.RemoveRange(edges);
        return edges.Count;
    }

    public async Task AddEdgeAsync(BusinessKnowledgeGraphEdge edge, CancellationToken cancellationToken = default)
        => await _db.BusinessKnowledgeGraphEdges.AddAsync(edge, cancellationToken);

    public async Task<IReadOnlyList<BusinessKnowledgeGraphEdge>> GetEdgesAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessKnowledgeGraphEdges
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.Weight)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessKnowledgeGraphEdge>> GetEdgesForCustomerAsync(
        Guid tenantId, Guid customerId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessKnowledgeGraphEdges
            .Where(e => e.TenantId == tenantId &&
                ((e.SourceType == KnowledgeGraphNodeTypes.Customer && e.SourceId == customerId) ||
                 (e.TargetType == KnowledgeGraphNodeTypes.Customer && e.TargetId == customerId) ||
                 (e.SourceId == customerId || e.TargetId == customerId)))
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountEdgesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _db.BusinessKnowledgeGraphEdges.CountAsync(e => e.TenantId == tenantId, cancellationToken);
}
