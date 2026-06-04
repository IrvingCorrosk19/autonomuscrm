using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Revenue;

namespace AutonomusCRM.Application.Executive;

public record ExecutivePulseDto(
    decimal RevenueGenerated,
    decimal RevenueProtected,
    decimal RevenueAtRisk,
    int AtRiskCustomers,
    int ExpansionReady,
    int PendingDecisions,
    int RecommendedActions);

public record AiImpactSummaryDto(
    decimal RevenueGeneratedByAi,
    decimal RevenueProtectedByAi,
    int AiDecisionsExecuted,
    int AiDecisionsPending,
    int HumanApprovals,
    int HumanRejections);

public record OutcomeAttributionChainDto(
    string Action,
    string Outcome,
    decimal Revenue,
    string Status,
    DateTime OccurredAt,
    string? CustomerName);

public record QbrPeriodDto(
    string PeriodKey,
    string Label,
    int Days,
    decimal RevenueGenerated,
    decimal RevenueProtected,
    decimal RevenueAtRisk,
    decimal RevenueLost,
    int DealsWon,
    int DealsLost,
    int AiDecisionsExecuted,
    int HumanApprovals,
    int CompleteOutcomeChains,
    string Headline);

public record ExecutiveQbrDto(
    QbrPeriodDto Monthly,
    QbrPeriodDto Quarterly,
    QbrPeriodDto Annual);

public record ExecutiveOsDashboardDto(
    ExecutivePulseDto Pulse,
    AiImpactSummaryDto AiImpact,
    IReadOnlyList<OutcomeAttributionChainDto> OutcomeChains,
    ExecutiveQbrDto Qbr,
    ExecutiveAiDashboardDto Ai,
    RevenueOsDashboardDto Revenue,
    Autonomous.AbosExecutiveLearningDto? Learning,
    int TrustPendingApprovals,
    bool HasData);

public interface IExecutiveOsService
{
    Task<ExecutiveOsDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<string> BuildExportHtmlAsync(
        Guid tenantId, string exportType, CancellationToken cancellationToken = default);
}
