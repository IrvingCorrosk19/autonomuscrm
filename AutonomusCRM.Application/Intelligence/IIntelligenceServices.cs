namespace AutonomusCRM.Application.Intelligence;

public interface IProductUsageEventRepository : Common.Interfaces.IRepository<ProductUsageEvent>
{
    Task<IEnumerable<ProductUsageEvent>> GetByTenantAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}

public interface ICustomerFeedbackRepository : Common.Interfaces.IRepository<CustomerFeedback>
{
    Task<IEnumerable<CustomerFeedback>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerFeedback>> GetByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}

public interface ICustomerAnalyticsSnapshotRepository : Common.Interfaces.IRepository<CustomerAnalyticsSnapshot>
{
    Task<IEnumerable<CustomerAnalyticsSnapshot>> GetByTenantAsync(Guid tenantId, DateTime? from = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerAnalyticsSnapshot>> GetByCustomerAsync(Guid tenantId, Guid customerId, int take = 90, CancellationToken cancellationToken = default);
    Task<CustomerAnalyticsSnapshot?> GetLatestAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}

public interface IProductAnalyticsEngine
{
    Task<ProductAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task RecordUsageAsync(Guid tenantId, string module, string eventType, Guid? userId = null, Guid? customerId = null, int durationMinutes = 0, CancellationToken cancellationToken = default);
    Task SyncFromUserLoginsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface INpsEngine
{
    Task<NpsSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> SubmitNpsAsync(Guid tenantId, Guid customerId, int score, string? comment = null, CancellationToken cancellationToken = default);
    static string Classify(int score) => score switch
    {
        >= 9 => IntelligenceConstants.NpsPromoter,
        >= 7 => IntelligenceConstants.NpsPassive,
        _ => IntelligenceConstants.NpsDetractor
    };
}

public interface ICsatEngine
{
    Task<CsatSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> SubmitCsatAsync(Guid tenantId, Guid customerId, int score, string? comment = null, CancellationToken cancellationToken = default);
}

public interface ICustomerInsightsEngine
{
    Task<IReadOnlyList<CustomerInsightDto>> GenerateInsightsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IProductUsageIntelligence
{
    Task<IReadOnlyList<ProductUsageInsightDto>> AnalyzeUsageAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IChurnPredictionV2
{
    Task<IReadOnlyList<ChurnPredictionV2Dto>> PredictAsync(Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default);
}

public interface IExpansionIntelligence
{
    Task<IReadOnlyList<ExpansionIntelligenceDto>> AnalyzeAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICustomerSegmentationEngine
{
    Task<IReadOnlyList<CustomerSegmentDto>> SegmentAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<string> ResolveSegmentAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task ApplySegmentsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IFeedbackEngine
{
    Task<FeedbackSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> SubmitCommentAsync(Guid tenantId, Guid customerId, string comment, CancellationToken cancellationToken = default);
}

public interface ICustomerDataMartService
{
    Task<int> BuildDailySnapshotsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerSnapshotTrendDto>> GetTrendsAsync(Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default);
}

public interface IExecutiveIntelligenceDashboardService
{
    Task<ExecutiveIntelligenceDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICustomerInsightsAgentService
{
    Task<IReadOnlyList<InsightActionDto>> AnalyzeAndActAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IIntelligenceAutomationEngine
{
    Task RunPeriodicIntelligenceScanAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
