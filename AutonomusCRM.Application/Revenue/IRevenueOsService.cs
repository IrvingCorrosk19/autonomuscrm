using AutonomusCRM.Application.Autonomous;

namespace AutonomusCRM.Application.Revenue;

public record RevenueExecutiveOverviewDto(
    decimal RevenueGenerated,
    decimal RevenueProtected,
    decimal RevenueAtRisk,
    decimal RevenueExpansion,
    decimal RevenueRenewals,
    decimal RevenueLost,
    decimal RevenueRecovered,
    bool HasData);

public record RevenueHealthDto(
    int HealthScore,
    int StabilityScore,
    int RiskIndex,
    int ExpansionIndex,
    int RenewalConfidence);

public record OutcomeAttributionRowDto(
    Guid AuditId,
    string DecisionType,
    string Action,
    string Status,
    decimal? RevenueImpact,
    string LearningStatus,
    DateTime CreatedAt);

public record RevenueInsightDto(string Category, string Title, string Detail, int Priority);

public record RevenueOsDashboardDto(
    RevenueExecutiveOverviewDto Overview,
    RevenueHealthDto Health,
    IReadOnlyList<OutcomeAttributionRowDto> AttributionChains,
    IReadOnlyList<RevenueInsightDto> Insights,
    PredictiveRevenueForecastDto Forecast,
    IReadOnlyList<WinLossBreakdownDto> WinLoss,
    IReadOnlyList<WinLossBreakdownDto> WinBreakdown,
    RevenueKpiSnapshotDto Kpis,
    bool HasData);

public interface IRevenueOsService
{
    Task<RevenueOsDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
