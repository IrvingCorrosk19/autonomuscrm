using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.BusinessMemory;

public record BusinessMemoryDto(
    Guid Id,
    Guid TenantId,
    string SubjectType,
    Guid SubjectId,
    string Title,
    string Summary,
    string MemoryType,
    int Importance,
    DateTime CreatedAt,
    IReadOnlyList<string> Tags);

public record CustomerMemoryBundleDto(
    Guid CustomerId,
    IReadOnlyList<BusinessMemoryDto> Episodes,
    IReadOnlyList<BusinessMemoryLearningDto> Learnings,
    IReadOnlyList<BusinessMemoryInsightDto> Insights,
    int TotalFacts,
    int TotalOutcomes);

public record BusinessMemoryLearningDto(
    string StrategyKey,
    string ActionTaken,
    decimal SuccessRate,
    int SuccessCount,
    int FailureCount,
    string? LastOutcome);

public record BusinessMemoryInsightDto(string InsightType, string Content, double Confidence);

public record BusinessMemorySearchHitDto(
    Guid MemoryId,
    string Title,
    string Summary,
    string SubjectType,
    Guid SubjectId,
    DateTime CreatedAt,
    double Relevance);

public interface IBusinessMemoryRepository
{
    Task<BusinessMemoryRoot?> GetByEpisodeKeyAsync(Guid tenantId, string episodeKey, CancellationToken cancellationToken = default);
    Task AddMemoryAsync(BusinessMemoryRoot memory, CancellationToken cancellationToken = default);
    Task AddEventAsync(BusinessMemoryEvent evt, CancellationToken cancellationToken = default);
    Task AddFactAsync(BusinessMemoryFact fact, CancellationToken cancellationToken = default);
    Task AddOutcomeAsync(BusinessMemoryOutcome outcome, CancellationToken cancellationToken = default);
    Task AddDecisionAsync(BusinessMemoryDecision decision, CancellationToken cancellationToken = default);
    Task AddRelationshipAsync(BusinessMemoryRelationship rel, CancellationToken cancellationToken = default);
    Task AddInsightAsync(BusinessMemoryInsight insight, CancellationToken cancellationToken = default);
    Task AddObservationAsync(BusinessMemoryObservation obs, CancellationToken cancellationToken = default);
    Task<BusinessMemoryLearning?> GetLearningAsync(Guid tenantId, string strategyKey, CancellationToken cancellationToken = default);
    Task AddLearningAsync(BusinessMemoryLearning learning, CancellationToken cancellationToken = default);
    Task UpdateLearningAsync(BusinessMemoryLearning learning, CancellationToken cancellationToken = default);
    Task AddContextAsync(BusinessMemoryContext ctx, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryRoot>> GetBySubjectAsync(Guid tenantId, string subjectType, Guid subjectId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryRoot>> GetRecentAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryLearning>> GetLearningsAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryInsight>> GetInsightsForCustomerAsync(Guid tenantId, Guid customerId, int take, CancellationToken cancellationToken = default);
    Task<int> CountFactsForSubjectAsync(Guid tenantId, string subjectType, Guid subjectId, CancellationToken cancellationToken = default);
    Task<int> CountOutcomesForSubjectAsync(Guid tenantId, string subjectType, Guid subjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryRoot>> SearchAsync(Guid tenantId, string query, int take, CancellationToken cancellationToken = default);
    Task<BusinessMemoryDecision?> GetDecisionByAuditAsync(Guid tenantId, Guid auditId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryOutcome>> GetOutcomesForMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default);
}

public interface IBusinessMemoryPipeline
{
    Task CaptureFromDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task CaptureFromDecisionAuditAsync(Guid auditId, CancellationToken cancellationToken = default);
}

public interface IBusinessMemoryService
{
    Task<CustomerMemoryBundleDto> GetCustomerMemoryAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryDto>> GetBusinessMemoryAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
    Task<BusinessMemoryDecision?> GetDecisionMemoryAsync(Guid tenantId, Guid aiDecisionAuditId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryOutcome>> GetOutcomeMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemorySearchHitDto>> SearchMemoryAsync(Guid tenantId, string query, int take = 30, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemoryLearningDto>> GetLearningHistoryAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
}
