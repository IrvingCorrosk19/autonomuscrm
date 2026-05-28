namespace AutonomusCRM.Application.CustomerSuccess;

public record CustomerHealthDto(
    Guid CustomerId,
    string CustomerName,
    int HealthScore,
    int AdoptionScore,
    int EngagementScore,
    int SupportScore,
    int RevenueScore,
    int RiskComponentScore,
    string Classification);

public record ChurnRiskSignalDto(
    Guid CustomerId,
    string CustomerName,
    string SignalType,
    string Severity,
    string Description);

public record RenewalAlertDto(
    Guid ContractId,
    Guid CustomerId,
    string CustomerName,
    DateTime RenewalDate,
    int DaysUntilRenewal,
    decimal AnnualValue,
    string Window);

public record RenewalForecastDto(
    int HorizonDays,
    decimal ExpectedRenewalArr,
    int ContractsInWindow,
    decimal AtRiskArr);

public record PlaybookExecutionDto(
    string PlaybookType,
    Guid CustomerId,
    int TasksCreated,
    IReadOnlyList<string> TaskTypes);

public record CommunicationSendResultDto(
    Guid LogId,
    string TrackingId,
    string Status,
    string Channel,
    string EventType);

public record JourneyStageMetricDto(
    string Stage,
    int Count,
    double? AvgDurationDays,
    double? ConversionPercent,
    double? AvgHealthScore);

public record ExpansionOpportunityDto(
    Guid CustomerId,
    string CustomerName,
    string OpportunityType,
    string Recommendation,
    decimal? SuggestedAmount);

public record CustomerIntelligenceActionDto(
    string AgentName,
    Guid CustomerId,
    string ActionType,
    string Description,
    bool TaskCreated);

public record CustomerKpiSnapshotDto(
    double AvgHealthScore,
    int CustomersAtRisk,
    double ChurnRiskPercent,
    double RenewalRatePercent,
    double RetentionRatePercent,
    decimal ExpansionRevenue,
    decimal UpsellRevenue,
    decimal CrossSellRevenue,
    decimal CustomerLifetimeValue,
    double AvgAdoptionScore,
    double AvgEngagementScore);

public record ExecutiveCustomerDashboardDto(
    CustomerKpiSnapshotDto Kpis,
    IReadOnlyList<CustomerHealthDto> HealthSummary,
    IReadOnlyList<ChurnRiskSignalDto> TopChurnSignals,
    IReadOnlyList<RenewalAlertDto> UpcomingRenewals,
    IReadOnlyList<ExpansionOpportunityDto> ExpansionOpportunities,
    IReadOnlyList<JourneyStageMetricDto> JourneyMetrics,
    RenewalForecastDto RenewalForecast90);
