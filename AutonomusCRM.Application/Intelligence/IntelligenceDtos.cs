namespace AutonomusCRM.Application.Intelligence;

public record ProductAnalyticsDto(
    int Dau,
    int Wau,
    int Mau,
    double Stickiness,
    double AvgSessionMinutes,
    IReadOnlyDictionary<string, int> UsageByModule,
    int TotalLogins,
    int ActiveUsers);

public record NpsSummaryDto(
    double GlobalNps,
    int Promoters,
    int Passives,
    int Detractors,
    int TotalResponses,
    IReadOnlyList<NpsByCustomerDto> ByCustomer,
    IReadOnlyList<NpsBySegmentDto> BySegment);

public record NpsByCustomerDto(Guid CustomerId, string CustomerName, int LatestScore, string Classification, DateTime? SubmittedAt);
public record NpsBySegmentDto(string Segment, double Nps, int Count);

public record CsatSummaryDto(
    double AverageScore,
    double TrendDelta,
    int TotalResponses,
    IReadOnlyList<CsatTrendPointDto> History);

public record CsatTrendPointDto(DateTime Period, double AverageScore, int Count);

public record CustomerInsightDto(
    string InsightType,
    string Title,
    string Description,
    string Severity,
    Guid? CustomerId,
    string? Module,
    bool Actionable);

public record ProductUsageInsightDto(
    string Module,
    int EventCount,
    int UniqueUsers,
    int UniqueCustomers,
    bool IsCritical,
    bool IsAbandoned);

public record ChurnPredictionV2Dto(
    Guid CustomerId,
    string CustomerName,
    int ChurnProbability,
    string TrendDirection,
    IReadOnlyList<string> RiskFactors,
    double? HealthTrend,
    double? EngagementTrend);

public record ExpansionIntelligenceDto(
    Guid CustomerId,
    string CustomerName,
    string ReadinessLevel,
    string OpportunityType,
    int ReadinessScore,
    string Recommendation);

public record CustomerSegmentDto(
    Guid CustomerId,
    string CustomerName,
    string Segment,
    int HealthScore,
    decimal? LifetimeValue);

public record FeedbackSummaryDto(
    int TotalFeedback,
    NpsSummaryDto Nps,
    CsatSummaryDto Csat,
    IReadOnlyList<FeedbackItemDto> RecentComments);

public record FeedbackItemDto(Guid Id, Guid CustomerId, string Type, int Score, string? Comment, DateTime SubmittedAt);

public record CustomerSnapshotTrendDto(
    Guid CustomerId,
    IReadOnlyList<SnapshotPointDto> Points);

public record SnapshotPointDto(DateTime Date, int HealthScore, int ChurnRiskScore, int? NpsScore, decimal? CsatScore);

public record ExecutiveIntelligenceDashboardDto(
    ProductAnalyticsDto ProductAnalytics,
    NpsSummaryDto Nps,
    CsatSummaryDto Csat,
    IReadOnlyList<CustomerHealthSummaryDto> HealthOverview,
    IReadOnlyList<ChurnPredictionV2Dto> ChurnPredictions,
    IReadOnlyList<ExpansionIntelligenceDto> ExpansionTrends,
    IReadOnlyList<CustomerSegmentDto> Segmentation,
    IReadOnlyList<CustomerInsightDto> TopInsights,
    FeedbackSummaryDto Feedback);

public record CustomerHealthSummaryDto(Guid CustomerId, string Name, int HealthScore, string Classification);

public record InsightActionDto(
    string AgentName,
    Guid? CustomerId,
    string ActionType,
    string Description,
    bool TaskCreated);
