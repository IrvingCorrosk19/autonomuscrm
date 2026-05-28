namespace AutonomusCRM.Application.Revenue;

public record RevenueForecastDto(
    int HorizonDays,
    decimal WeightedForecast,
    decimal UnweightedPipeline,
    double HistoricalWinRate,
    double ConfidenceFactor);

public record RepPerformanceDto(
    Guid UserId,
    string Email,
    decimal QuotaTarget,
    decimal RevenueClosed,
    decimal OpenPipelineWeighted,
    double AttainmentPercent,
    double PipelineCoveragePercent,
    int Rank,
    int OpenDeals,
    int WonDeals,
    int LostDeals);

public record PipelineCoverageDto(
    Guid? UserId,
    string Label,
    decimal OpenPipelineWeighted,
    decimal QuotaTarget,
    double CoveragePercent,
    bool IsSufficient);

public record WinLossBreakdownDto(
    string Dimension,
    string Key,
    int Count,
    decimal TotalAmount,
    double PercentOfLosses);

public record SalesProductivityDto(
    Guid? UserId,
    string Email,
    int TasksCompleted,
    int TasksOverdue,
    int OpenTasks,
    double? AvgLeadResponseHours,
    double? AvgSalesCycleDays,
    int ActivitiesCount);

public record SlaBreachDto(
    string SlaType,
    string EntityType,
    Guid EntityId,
    string Title,
    DateTime DueAt,
    int HoursOverdue,
    string RecommendedAction);

public record RevenueKpiSnapshotDto(
    decimal RevenueClosed,
    double WinRate,
    decimal AverageDealSize,
    double? AverageSalesCycleDays,
    double ForecastAccuracyProxy,
    double PipelineCoveragePercent,
    double ConversionRate,
    decimal RevenuePerRep,
    decimal LostRevenue,
    decimal RecoveryPipelineWeighted);

public record ExecutiveSalesDashboardDto(
    RevenueKpiSnapshotDto Kpis,
    IReadOnlyList<RevenueForecastDto> Forecasts,
    IReadOnlyList<RepPerformanceDto> Leaderboard,
    IReadOnlyList<PipelineCoverageDto> Coverage,
    IReadOnlyList<SlaBreachDto> SlaBreaches,
    IReadOnlyList<WinLossBreakdownDto> TopLossReasons,
    int AtRiskDeals);

public record SalesIntelligenceActionDto(
    Guid DealId,
    string Priority,
    string RecommendedAction,
    Guid? TaskId);
