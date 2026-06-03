using AutonomusCRM.Application.BusinessMemory;

namespace AutonomusCRM.Infrastructure.BusinessMemory;

public sealed class BusinessMemoryService : IBusinessMemoryService
{
    private readonly IBusinessMemoryRepository _repo;

    public BusinessMemoryService(IBusinessMemoryRepository repo) => _repo = repo;

    public async Task<CustomerMemoryBundleDto> GetCustomerMemoryAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var episodes = await _repo.GetBySubjectAsync(
            tenantId, BusinessMemoryConstants.SubjectCustomer, customerId, 100, cancellationToken);
        var learnings = await _repo.GetLearningsAsync(tenantId, 20, cancellationToken);
        var insights = await _repo.GetInsightsForCustomerAsync(tenantId, customerId, 20, cancellationToken);
        var facts = await _repo.CountFactsForSubjectAsync(
            tenantId, BusinessMemoryConstants.SubjectCustomer, customerId, cancellationToken);
        var outcomes = await _repo.CountOutcomesForSubjectAsync(
            tenantId, BusinessMemoryConstants.SubjectCustomer, customerId, cancellationToken);

        return new CustomerMemoryBundleDto(
            customerId,
            episodes.Select(ToDto).ToList(),
            learnings.Select(l => new BusinessMemoryLearningDto(
                l.StrategyKey, l.ActionTaken, l.SuccessRate, l.SuccessCount, l.FailureCount, l.LastOutcome)).ToList(),
            insights.Select(i => new BusinessMemoryInsightDto(i.InsightType, i.Content, i.Confidence)).ToList(),
            facts,
            outcomes);
    }

    public async Task<IReadOnlyList<BusinessMemoryDto>> GetBusinessMemoryAsync(
        Guid tenantId, int take = 50, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetRecentAsync(tenantId, take, cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public Task<BusinessMemoryDecision?> GetDecisionMemoryAsync(
        Guid tenantId, Guid aiDecisionAuditId, CancellationToken cancellationToken = default)
        => _repo.GetDecisionByAuditAsync(tenantId, aiDecisionAuditId, cancellationToken);

    public Task<IReadOnlyList<BusinessMemoryOutcome>> GetOutcomeMemoryAsync(
        Guid memoryId, CancellationToken cancellationToken = default)
        => _repo.GetOutcomesForMemoryAsync(memoryId, cancellationToken);

    public async Task<IReadOnlyList<BusinessMemorySearchHitDto>> SearchMemoryAsync(
        Guid tenantId, string query, int take = 30, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<BusinessMemorySearchHitDto>();

        var hits = await _repo.SearchAsync(tenantId, query, take, cancellationToken);
        return hits.Select(m => new BusinessMemorySearchHitDto(
            m.Id, m.Title, m.Summary, m.SubjectType, m.SubjectId, m.CreatedAt,
            ScoreRelevance(m, query))).ToList();
    }

    public async Task<IReadOnlyList<BusinessMemoryLearningDto>> GetLearningHistoryAsync(
        Guid tenantId, int take = 50, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetLearningsAsync(tenantId, take, cancellationToken);
        return list.Select(l => new BusinessMemoryLearningDto(
            l.StrategyKey, l.ActionTaken, l.SuccessRate, l.SuccessCount, l.FailureCount, l.LastOutcome)).ToList();
    }

    private static BusinessMemoryDto ToDto(BusinessMemoryRoot m) =>
        new(m.Id, m.TenantId, m.SubjectType, m.SubjectId, m.Title, m.Summary, m.MemoryType, m.Importance, m.CreatedAt, m.Tags);

    private static double ScoreRelevance(BusinessMemoryRoot m, string query)
    {
        var q = query.ToLowerInvariant();
        var score = 0.0;
        if (m.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) score += 2;
        if (m.Summary.Contains(q, StringComparison.OrdinalIgnoreCase)) score += 1;
        if (m.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase))) score += 1.5;
        return score + m.Importance * 0.1;
    }
}
