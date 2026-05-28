using AutonomusCRM.Application.Intelligence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/intelligence")]
[Authorize]
public class IntelligenceController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ExecutiveIntelligenceDashboardDto>> GetDashboard(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IExecutiveIntelligenceDashboardService>();
        return Ok(await svc.GetDashboardAsync(tenantId, cancellationToken));
    }

    [HttpGet("product-analytics")]
    public async Task<ActionResult<ProductAnalyticsDto>> GetProductAnalytics(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IProductAnalyticsEngine>()
            .GetAnalyticsAsync(tenantId, cancellationToken));
    }

    [HttpGet("nps")]
    public async Task<ActionResult<NpsSummaryDto>> GetNps(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<INpsEngine>()
            .GetSummaryAsync(tenantId, cancellationToken));
    }

    [HttpGet("csat")]
    public async Task<ActionResult<CsatSummaryDto>> GetCsat(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<ICsatEngine>()
            .GetSummaryAsync(tenantId, cancellationToken));
    }

    [HttpGet("insights")]
    public async Task<ActionResult<IReadOnlyList<CustomerInsightDto>>> GetInsights(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<ICustomerInsightsEngine>()
            .GenerateInsightsAsync(tenantId, cancellationToken));
    }

    [HttpGet("churn-predictions")]
    public async Task<ActionResult<IReadOnlyList<ChurnPredictionV2Dto>>> GetChurnPredictions(
        [FromQuery] Guid tenantId, [FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IChurnPredictionV2>()
            .PredictAsync(tenantId, customerId, cancellationToken));
    }

    [HttpGet("expansion")]
    public async Task<ActionResult<IReadOnlyList<ExpansionIntelligenceDto>>> GetExpansion(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IExpansionIntelligence>()
            .AnalyzeAsync(tenantId, cancellationToken));
    }

    [HttpGet("segmentation")]
    public async Task<ActionResult<IReadOnlyList<CustomerSegmentDto>>> GetSegmentation(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<ICustomerSegmentationEngine>()
            .SegmentAllAsync(tenantId, cancellationToken));
    }

    [HttpGet("feedback")]
    public async Task<ActionResult<FeedbackSummaryDto>> GetFeedback(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IFeedbackEngine>()
            .GetSummaryAsync(tenantId, cancellationToken));
    }

    [HttpGet("trends")]
    public async Task<ActionResult<IReadOnlyList<CustomerSnapshotTrendDto>>> GetTrends(
        [FromQuery] Guid tenantId, [FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<ICustomerDataMartService>()
            .GetTrendsAsync(tenantId, customerId, cancellationToken));
    }

    [HttpPost("nps")]
    public async Task<ActionResult<Guid>> SubmitNps(
        [FromQuery] Guid tenantId, [FromQuery] Guid customerId, [FromQuery] int score,
        [FromQuery] string? comment, CancellationToken cancellationToken)
    {
        var id = await HttpContext.RequestServices.GetRequiredService<INpsEngine>()
            .SubmitNpsAsync(tenantId, customerId, score, comment, cancellationToken);
        return Ok(id);
    }

    [HttpPost("csat")]
    public async Task<ActionResult<Guid>> SubmitCsat(
        [FromQuery] Guid tenantId, [FromQuery] Guid customerId, [FromQuery] int score,
        [FromQuery] string? comment, CancellationToken cancellationToken)
    {
        var id = await HttpContext.RequestServices.GetRequiredService<ICsatEngine>()
            .SubmitCsatAsync(tenantId, customerId, score, comment, cancellationToken);
        return Ok(id);
    }

    [HttpPost("usage")]
    public async Task<ActionResult> RecordUsage(
        [FromQuery] Guid tenantId, [FromQuery] string module, [FromQuery] string eventType,
        [FromQuery] Guid? userId, [FromQuery] Guid? customerId, [FromQuery] int durationMinutes = 0,
        CancellationToken cancellationToken = default)
    {
        await HttpContext.RequestServices.GetRequiredService<IProductAnalyticsEngine>()
            .RecordUsageAsync(tenantId, module, eventType, userId, customerId, durationMinutes, cancellationToken);
        return Ok();
    }

    [HttpPost("scan")]
    public async Task<ActionResult> RunIntelligenceScan([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        await HttpContext.RequestServices.GetRequiredService<IIntelligenceAutomationEngine>()
            .RunPeriodicIntelligenceScanAsync(tenantId, cancellationToken);
        return Ok(new { status = "completed" });
    }
}
