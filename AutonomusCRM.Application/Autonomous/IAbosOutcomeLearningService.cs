namespace AutonomusCRM.Application.Autonomous;

public static class AbosOutcomeLearningConstants
{
    public const string TagAction = "abos:action";
    public const string TagPending = "abos:pending";
    public const string TagResolved = "abos:resolved";
    public const string SourceChannel = "abos_outcome_learning";
    public const string StrategyPrefixRecommendation = "recommendation.";
    public const string StrategyPrefixPlaybook = "playbook.";
    public const string StrategyPrefixAction = "action.";
    public const string StrategyPrefixAgent = "agent.";
}

public record AbosActionRecordedDto(Guid MemoryId, string EpisodeKey);

public record AbosLearningRatesDto(
    double OutcomeSuccessRate,
    double PlaybookSuccessRate,
    double RecommendationSuccessRate,
    double AgentSuccessRate,
    double SegmentSuccessRate);

public record AbosExecutiveLearningDto(
    AbosLearningRatesDto Rates,
    decimal RevenueGeneratedByActions,
    decimal RevenueProtected,
    decimal RevenueLost,
    IReadOnlyList<AbosEffectiveActionDto> TopActions);

public record AbosEffectiveActionDto(
    string Label,
    string Category,
    decimal SuccessRate,
    int TotalAttempts,
    decimal RevenueImpact);

public record CustomerActionLearningDto(
    IReadOnlyList<AbosActionHistoryItemDto> Worked,
    IReadOnlyList<AbosActionHistoryItemDto> Failed,
    IReadOnlyList<AbosEffectiveActionDto> BestActions);

public record AbosActionHistoryItemDto(
    string Action,
    string Recommendation,
    string Outcome,
    decimal RevenueDelta,
    DateTime OccurredAt,
    bool Succeeded);

public interface IAbosOutcomeLearningService
{
    Task<AbosActionRecordedDto> RecordActionExecutedAsync(
        Guid tenantId,
        Guid? executedByUserId,
        string actionType,
        string actionDetail,
        string? insightType,
        string? recommendation,
        string? rationale,
        Guid? customerId,
        Guid? relatedAuditId,
        CancellationToken cancellationToken = default);

    Task ResolvePendingActionsForCustomerAsync(
        Guid tenantId,
        Guid customerId,
        bool succeeded,
        string outcomeCategory,
        decimal revenueDelta,
        string narrative,
        CancellationToken cancellationToken = default);

    Task<AbosExecutiveLearningDto> GetExecutiveLearningAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<CustomerActionLearningDto> GetCustomerLearningAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}
