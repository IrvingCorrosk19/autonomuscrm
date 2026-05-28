namespace AutonomusCRM.Application.Revenue;

/// <summary>Motor unificado Revenue Operations (Fase 12).</summary>
public interface IRevenueForecastEngine
{
    Task<IReadOnlyList<RevenueForecastDto>> GetForecastAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ISalesPerformanceEngine
{
    Task<IReadOnlyList<RepPerformanceDto>> GetLeaderboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IPipelineCoverageService
{
    Task<IReadOnlyList<PipelineCoverageDto>> GetCoverageAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IWinLossAnalyticsService
{
    Task<IReadOnlyList<WinLossBreakdownDto>> GetAnalysisAsync(Guid tenantId, string groupBy = "reason", CancellationToken cancellationToken = default);
}

public interface ISalesProductivityService
{
    Task<IReadOnlyList<SalesProductivityDto>> GetProductivityAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICommercialSlaEngine
{
    Task<IReadOnlyList<SlaBreachDto>> DetectBreachesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task EnforceLeadCreatedSlaAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default);
}

public interface ISmartAssignmentEngine
{
    Task<Guid?> AssignLeadToBestRepAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default);
    Task<Guid?> GetRecommendedOwnerAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IRevenueAutomationEngine
{
    Task ProcessEventAsync(AutonomusCRM.Domain.Events.IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task RunPeriodicRevenueScanAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IRevenueKpiService
{
    Task<RevenueKpiSnapshotDto> GetSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IExecutiveSalesDashboardService
{
    Task<ExecutiveSalesDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ISalesIntelligenceService
{
    Task<IReadOnlyList<SalesIntelligenceActionDto>> AnalyzeAndActAsync(Guid tenantId, Guid dealId, CancellationToken cancellationToken = default);
}

public interface IDataQualityRevenueService
{
    Task<int> ScanAndCreateTasksAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
