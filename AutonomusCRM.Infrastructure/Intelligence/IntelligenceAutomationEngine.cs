using AutonomusCRM.Application.Intelligence;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class IntelligenceAutomationEngine : IIntelligenceAutomationEngine
{
    private readonly IProductAnalyticsEngine _productAnalytics;
    private readonly ICustomerDataMartService _dataMart;
    private readonly ICustomerSegmentationEngine _segmentation;
    private readonly ICustomerInsightsAgentService _insightsAgent;
    private readonly ILogger<IntelligenceAutomationEngine> _logger;

    public IntelligenceAutomationEngine(
        IProductAnalyticsEngine productAnalytics,
        ICustomerDataMartService dataMart,
        ICustomerSegmentationEngine segmentation,
        ICustomerInsightsAgentService insightsAgent,
        ILogger<IntelligenceAutomationEngine> logger)
    {
        _productAnalytics = productAnalytics;
        _dataMart = dataMart;
        _segmentation = segmentation;
        _insightsAgent = insightsAgent;
        _logger = logger;
    }

    public async Task RunPeriodicIntelligenceScanAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await _productAnalytics.SyncFromUserLoginsAsync(tenantId, cancellationToken);
        var snapshots = await _dataMart.BuildDailySnapshotsAsync(tenantId, cancellationToken);
        await _segmentation.ApplySegmentsAsync(tenantId, cancellationToken);
        var actions = await _insightsAgent.AnalyzeAndActAsync(tenantId, cancellationToken);
        _logger.LogInformation(
            "Intelligence scan tenant {TenantId}: {Snapshots} snapshots, {Actions} actions",
            tenantId, snapshots, actions.Count);
    }
}
