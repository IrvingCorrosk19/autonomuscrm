using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class ExecutiveIntelligenceDashboardService : IExecutiveIntelligenceDashboardService
{
    private readonly IProductAnalyticsEngine _productAnalytics;
    private readonly INpsEngine _nps;
    private readonly ICsatEngine _csat;
    private readonly ICustomerHealthEngine _health;
    private readonly IChurnPredictionV2 _churnV2;
    private readonly IExpansionIntelligence _expansion;
    private readonly ICustomerSegmentationEngine _segmentation;
    private readonly ICustomerInsightsEngine _insights;
    private readonly IFeedbackEngine _feedback;

    public ExecutiveIntelligenceDashboardService(
        IProductAnalyticsEngine productAnalytics,
        INpsEngine nps,
        ICsatEngine csat,
        ICustomerHealthEngine health,
        IChurnPredictionV2 churnV2,
        IExpansionIntelligence expansion,
        ICustomerSegmentationEngine segmentation,
        ICustomerInsightsEngine insights,
        IFeedbackEngine feedback)
    {
        _productAnalytics = productAnalytics;
        _nps = nps;
        _csat = csat;
        _health = health;
        _churnV2 = churnV2;
        _expansion = expansion;
        _segmentation = segmentation;
        _insights = insights;
        _feedback = feedback;
    }

    public async Task<ExecutiveIntelligenceDashboardDto> GetDashboardAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var healthAll = await _health.CalculateAllAsync(tenantId, cancellationToken);
        return new ExecutiveIntelligenceDashboardDto(
            await _productAnalytics.GetAnalyticsAsync(tenantId, cancellationToken),
            await _nps.GetSummaryAsync(tenantId, cancellationToken),
            await _csat.GetSummaryAsync(tenantId, cancellationToken),
            healthAll.Take(20).Select(h => new CustomerHealthSummaryDto(h.CustomerId, h.CustomerName, h.HealthScore, h.Classification)).ToList(),
            (await _churnV2.PredictAsync(tenantId, cancellationToken: cancellationToken)).Take(15).ToList(),
            (await _expansion.AnalyzeAsync(tenantId, cancellationToken)).Take(15).ToList(),
            (await _segmentation.SegmentAllAsync(tenantId, cancellationToken)).Take(50).ToList(),
            (await _insights.GenerateInsightsAsync(tenantId, cancellationToken)).Take(20).ToList(),
            await _feedback.GetSummaryAsync(tenantId, cancellationToken));
    }
}
