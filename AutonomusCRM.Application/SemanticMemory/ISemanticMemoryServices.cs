namespace AutonomusCRM.Application.SemanticMemory;

public record SemanticMemoryHitDto(
    Guid EmbeddingId,
    string SourceType,
    Guid SourceId,
    string Text,
    double Similarity,
    double RelevanceScore,
    double ConfidenceScore,
    int UsageCount,
    DateTime CreatedAt);

public record SemanticBusinessContextDto(
    string Query,
    IReadOnlyList<SemanticMemoryHitDto> SimilarMemories,
    IReadOnlyList<string> RelatedLearnings,
    IReadOnlyList<string> TopInsights,
    string NarrativeSummary);

public record MemoryTimelineItemDto(
    string ItemType,
    Guid ItemId,
    string Title,
    string Summary,
    DateTime OccurredAt,
    Guid? SubjectId,
    string? SubjectType);

public record SemanticMemoryDashboardDto(
    int TotalMemories,
    int LearnedPatterns,
    IReadOnlyList<SemanticMemoryHitDto> TopInsights,
    IReadOnlyList<SemanticMemoryHitDto> MostSuccessfulDecisions,
    IReadOnlyList<SemanticMemoryHitDto> MostEffectivePlaybooks);

public record CustomerMemoryProfileDto(
    Guid CustomerId,
    string HistorySummary,
    string RiskSummary,
    string PreferencesSummary,
    string SuccessfulDecisionsSummary,
    string FailedDecisionsSummary,
    string EffectiveChannelsSummary,
    DateTime LastRefreshedAt);

public interface ISemanticMemoryRepository
{
    Task<MemoryEmbedding?> GetBySourceAsync(Guid tenantId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default);
    Task AddEmbeddingAsync(MemoryEmbedding embedding, CancellationToken cancellationToken = default);
    Task UpdateEmbeddingAsync(MemoryEmbedding embedding, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryEmbedding>> GetByTenantAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryEmbedding>> GetBySourceTypesAsync(Guid tenantId, IEnumerable<string> sourceTypes, int take, CancellationToken cancellationToken = default);
    Task<CustomerMemoryProfile?> GetCustomerProfileAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task AddCustomerProfileAsync(CustomerMemoryProfile profile, CancellationToken cancellationToken = default);
    Task UpdateCustomerProfileAsync(CustomerMemoryProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetTenantIdsWithEmbeddingsAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetTenantIdsForConsolidationAsync(int take, CancellationToken cancellationToken = default);
    Task<int> CountEmbeddingsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryTimelineItemDto>> GetTimelineAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemory.BusinessMemoryObservation>> GetObservationsForConsolidationAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemory.BusinessMemoryLearning>> GetLearningsAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemory.BusinessMemoryDecision>> GetDecisionsAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessMemory.BusinessMemoryInsight>> GetInsightsAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
}

public interface ISemanticMemoryService
{
    Task<MemoryEmbedding> StoreMemoryAsync(
        Guid tenantId, string sourceType, Guid sourceId, string text,
        double confidence = 0.7, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SemanticMemoryHitDto>> SearchAsync(
        Guid tenantId, string query, int take = 20, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SemanticMemoryHitDto>> FindSimilarMemoriesAsync(
        Guid tenantId, string text, int take = 15, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRelatedLearningsAsync(
        Guid tenantId, string contextQuery, int take = 10, CancellationToken cancellationToken = default);

    Task<SemanticBusinessContextDto> GetBusinessContextAsync(
        Guid tenantId, string query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryTimelineItemDto>> GetTimelineAsync(
        Guid tenantId, int take = 100, CancellationToken cancellationToken = default);

    Task<SemanticMemoryDashboardDto> GetDashboardAsync(
        Guid tenantId, CancellationToken cancellationToken = default);

    Task<CustomerMemoryProfileDto> GetOrBuildCustomerProfileAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);

    Task<int> ConsolidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task IndexBusinessMemorySourcesAsync(Guid tenantId, int takePerType = 50, CancellationToken cancellationToken = default);
}
