using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.BusinessMemory;

public sealed class BusinessMemoryRepository : IBusinessMemoryRepository
{
    private readonly ApplicationDbContext _db;

    public BusinessMemoryRepository(ApplicationDbContext db) => _db = db;

    public Task<BusinessMemoryRoot?> GetByEpisodeKeyAsync(Guid tenantId, string episodeKey, CancellationToken cancellationToken = default)
        => _db.BusinessMemoryRoots.FirstOrDefaultAsync(
            m => m.TenantId == tenantId && m.EpisodeKey == episodeKey, cancellationToken);

    public async Task AddMemoryAsync(BusinessMemoryRoot memory, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryRoots.AddAsync(memory, cancellationToken);

    public async Task AddEventAsync(BusinessMemoryEvent evt, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryEvents.AddAsync(evt, cancellationToken);

    public async Task AddFactAsync(BusinessMemoryFact fact, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryFacts.AddAsync(fact, cancellationToken);

    public async Task AddOutcomeAsync(BusinessMemoryOutcome outcome, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryOutcomes.AddAsync(outcome, cancellationToken);

    public async Task AddDecisionAsync(BusinessMemoryDecision decision, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryDecisions.AddAsync(decision, cancellationToken);

    public async Task AddRelationshipAsync(BusinessMemoryRelationship rel, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryRelationships.AddAsync(rel, cancellationToken);

    public async Task AddInsightAsync(BusinessMemoryInsight insight, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryInsights.AddAsync(insight, cancellationToken);

    public async Task AddObservationAsync(BusinessMemoryObservation obs, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryObservations.AddAsync(obs, cancellationToken);

    public Task<BusinessMemoryLearning?> GetLearningAsync(Guid tenantId, string strategyKey, CancellationToken cancellationToken = default)
        => _db.BusinessMemoryLearnings.FirstOrDefaultAsync(
            l => l.TenantId == tenantId && l.StrategyKey == strategyKey, cancellationToken);

    public async Task AddLearningAsync(BusinessMemoryLearning learning, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryLearnings.AddAsync(learning, cancellationToken);

    public Task UpdateLearningAsync(BusinessMemoryLearning learning, CancellationToken cancellationToken = default)
    {
        _db.BusinessMemoryLearnings.Update(learning);
        return Task.CompletedTask;
    }

    public async Task AddContextAsync(BusinessMemoryContext ctx, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryContexts.AddAsync(ctx, cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryRoot>> GetBySubjectAsync(
        Guid tenantId, string subjectType, Guid subjectId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryRoots
            .Where(m => m.TenantId == tenantId && m.SubjectType == subjectType && m.SubjectId == subjectId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryRoot>> GetRecentAsync(Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryRoots
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryLearning>> GetLearningsAsync(Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryLearnings
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.SuccessRate)
            .ThenByDescending(l => l.SuccessCount + l.FailureCount)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryInsight>> GetInsightsForCustomerAsync(
        Guid tenantId, Guid customerId, int take, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryInsights
            .Where(i => i.TenantId == tenantId && i.CustomerId == customerId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<int> CountFactsForSubjectAsync(
        Guid tenantId, string subjectType, Guid subjectId, CancellationToken cancellationToken = default)
    {
        var memoryIds = _db.BusinessMemoryRoots
            .Where(m => m.TenantId == tenantId && m.SubjectType == subjectType && m.SubjectId == subjectId)
            .Select(m => m.Id);
        return await _db.BusinessMemoryFacts.CountAsync(f => memoryIds.Contains(f.MemoryId), cancellationToken);
    }

    public async Task<int> CountOutcomesForSubjectAsync(
        Guid tenantId, string subjectType, Guid subjectId, CancellationToken cancellationToken = default)
    {
        var memoryIds = _db.BusinessMemoryRoots
            .Where(m => m.TenantId == tenantId && m.SubjectType == subjectType && m.SubjectId == subjectId)
            .Select(m => m.Id);
        return await _db.BusinessMemoryOutcomes.CountAsync(o => memoryIds.Contains(o.MemoryId), cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessMemoryRoot>> SearchAsync(
        Guid tenantId, string query, int take, CancellationToken cancellationToken = default)
    {
        var q = query.Trim().ToLowerInvariant();
        return await _db.BusinessMemoryRoots
            .Where(m => m.TenantId == tenantId
                && (m.Title.ToLower().Contains(q) || m.Summary.ToLower().Contains(q)
                    || m.Tags.Any(t => t.ToLower().Contains(q))))
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<BusinessMemoryDecision?> GetDecisionByAuditAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken = default)
        => _db.BusinessMemoryDecisions.FirstOrDefaultAsync(
            d => d.TenantId == tenantId && d.AiDecisionAuditId == auditId, cancellationToken);

    public async Task<IReadOnlyList<BusinessMemoryOutcome>> GetOutcomesForMemoryAsync(
        Guid memoryId, CancellationToken cancellationToken = default)
        => await _db.BusinessMemoryOutcomes
            .Where(o => o.MemoryId == memoryId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
}
