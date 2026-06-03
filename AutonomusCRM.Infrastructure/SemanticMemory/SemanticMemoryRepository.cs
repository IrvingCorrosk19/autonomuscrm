using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.SemanticMemory;

public sealed class SemanticMemoryRepository : ISemanticMemoryRepository
{
    private readonly ApplicationDbContext _db;

    public SemanticMemoryRepository(ApplicationDbContext db) => _db = db;

    public Task<MemoryEmbedding?> GetBySourceAsync(
        Guid tenantId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
        => _db.MemoryEmbeddings
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.SourceType == sourceType && e.SourceId == sourceId, cancellationToken);

    public async Task AddEmbeddingAsync(MemoryEmbedding embedding, CancellationToken cancellationToken = default)
    {
        await _db.MemoryEmbeddings.AddAsync(embedding, cancellationToken);
    }

    public Task UpdateEmbeddingAsync(MemoryEmbedding embedding, CancellationToken cancellationToken = default)
    {
        _db.MemoryEmbeddings.Update(embedding);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MemoryEmbedding>> GetByTenantAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.MemoryEmbeddings
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.RelevanceScore)
            .ThenByDescending(e => e.UsageCount)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MemoryEmbedding>> GetBySourceTypesAsync(
        Guid tenantId, IEnumerable<string> sourceTypes, int take, CancellationToken cancellationToken = default)
    {
        var types = sourceTypes.ToList();
        return await _db.MemoryEmbeddings
            .Where(e => e.TenantId == tenantId && types.Contains(e.SourceType))
            .OrderByDescending(e => e.RelevanceScore)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerMemoryProfile?> GetCustomerProfileAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
        => _db.CustomerMemoryProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.CustomerId == customerId, cancellationToken);

    public async Task AddCustomerProfileAsync(CustomerMemoryProfile profile, CancellationToken cancellationToken = default)
        => await _db.CustomerMemoryProfiles.AddAsync(profile, cancellationToken);

    public Task UpdateCustomerProfileAsync(CustomerMemoryProfile profile, CancellationToken cancellationToken = default)
    {
        _db.CustomerMemoryProfiles.Update(profile);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Guid>> GetTenantIdsWithEmbeddingsAsync(int take, CancellationToken cancellationToken = default)
        => await _db.MemoryEmbeddings
            .Select(e => e.TenantId)
            .Distinct()
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetTenantIdsForConsolidationAsync(int take, CancellationToken cancellationToken = default)
    {
        var fromObs = _db.BusinessMemoryObservations.Select(o => o.TenantId);
        var fromEmb = _db.MemoryEmbeddings.Select(e => e.TenantId);
        var fromRoots = _db.BusinessMemoryRoots.Select(m => m.TenantId);
        return await fromObs.Union(fromEmb).Union(fromRoots).Distinct().Take(take).ToListAsync(cancellationToken);
    }

    public Task<int> CountEmbeddingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _db.MemoryEmbeddings.CountAsync(e => e.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<MemoryTimelineItemDto>> GetTimelineAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
    {
        var items = new List<MemoryTimelineItemDto>();

        var observations = await _db.BusinessMemoryObservations
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.ObservedAt)
            .Take(take)
            .Select(o => new MemoryTimelineItemDto(
                SemanticMemoryConstants.SourceObservation,
                o.Id,
                "Observación",
                o.Content,
                o.ObservedAt,
                o.SubjectId,
                o.SubjectType))
            .ToListAsync(cancellationToken);
        items.AddRange(observations);

        var decisions = await _db.BusinessMemoryDecisions
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .Select(d => new MemoryTimelineItemDto(
                SemanticMemoryConstants.SourceDecision,
                d.Id,
                d.DecisionType,
                $"{d.Action}: {d.Reason}",
                d.CreatedAt,
                null,
                null))
            .ToListAsync(cancellationToken);
        items.AddRange(decisions);

        var outcomes = await _db.BusinessMemoryOutcomes
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(take)
            .Select(o => new MemoryTimelineItemDto(
                SemanticMemoryConstants.SourceOutcome,
                o.Id,
                o.OutcomeCategory,
                o.Narrative,
                o.CreatedAt,
                null,
                null))
            .ToListAsync(cancellationToken);
        items.AddRange(outcomes);

        var learnings = await _db.BusinessMemoryLearnings
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
            .Take(take)
            .Select(l => new MemoryTimelineItemDto(
                SemanticMemoryConstants.SourceLearning,
                l.Id,
                l.StrategyKey,
                $"{l.ActionTaken} ({l.SuccessRate:F0}% éxito)",
                l.LastAppliedAt ?? l.CreatedAt,
                null,
                null))
            .ToListAsync(cancellationToken);
        items.AddRange(learnings);

        return items
            .OrderByDescending(i => i.OccurredAt)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<BusinessMemoryObservation>> GetObservationsForConsolidationAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryObservations
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.ObservedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryLearning>> GetLearningsAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryLearnings
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.SuccessRate)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryDecision>> GetDecisionsAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryDecisions
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.Score)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryInsight>> GetInsightsAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryInsights
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.Confidence)
            .Take(take)
            .ToListAsync(cancellationToken);
}
