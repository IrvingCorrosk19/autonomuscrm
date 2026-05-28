using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.Revenue.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/revenue")]
[Authorize]
public class RevenueController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ExecutiveSalesDashboardDto>> GetDashboard(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IExecutiveSalesDashboardService>();
        return Ok(await svc.GetDashboardAsync(tenantId, cancellationToken));
    }

    [HttpGet("forecast")]
    public async Task<ActionResult<IReadOnlyList<RevenueForecastDto>>> GetForecast(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IRevenueForecastEngine>();
        return Ok(await svc.GetForecastAsync(tenantId, cancellationToken));
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<IReadOnlyList<RepPerformanceDto>>> GetLeaderboard(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ISalesPerformanceEngine>();
        return Ok(await svc.GetLeaderboardAsync(tenantId, cancellationToken));
    }

    [HttpGet("pipeline-coverage")]
    public async Task<ActionResult<IReadOnlyList<PipelineCoverageDto>>> GetCoverage(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IPipelineCoverageService>();
        return Ok(await svc.GetCoverageAsync(tenantId, cancellationToken));
    }

    [HttpGet("win-loss")]
    public async Task<ActionResult<IReadOnlyList<WinLossBreakdownDto>>> GetWinLoss(
        [FromQuery] Guid tenantId, [FromQuery] string groupBy = "reason", CancellationToken cancellationToken = default)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IWinLossAnalyticsService>();
        return Ok(await svc.GetAnalysisAsync(tenantId, groupBy, cancellationToken));
    }

    [HttpGet("productivity")]
    public async Task<ActionResult<IReadOnlyList<SalesProductivityDto>>> GetProductivity(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ISalesProductivityService>();
        return Ok(await svc.GetProductivityAsync(tenantId, cancellationToken));
    }

    [HttpGet("sla-breaches")]
    public async Task<ActionResult<IReadOnlyList<SlaBreachDto>>> GetSlaBreaches(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ICommercialSlaEngine>();
        return Ok(await svc.DetectBreachesAsync(tenantId, cancellationToken));
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<RevenueKpiSnapshotDto>> GetKpis(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IRevenueKpiService>();
        return Ok(await svc.GetSnapshotAsync(tenantId, cancellationToken));
    }

    [HttpPost("quotas")]
    public async Task<ActionResult<Guid>> UpsertQuota([FromBody] UpsertSalesQuotaCommand command, CancellationToken cancellationToken)
    {
        var handler = HttpContext.RequestServices.GetRequiredService<IRequestHandler<UpsertSalesQuotaCommand, Guid>>();
        var id = await handler.HandleAsync(command, cancellationToken);
        return Ok(id);
    }

    [HttpPost("scan")]
    public async Task<ActionResult> RunRevenueScan([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var revenue = HttpContext.RequestServices.GetRequiredService<IRevenueAutomationEngine>();
        var dq = HttpContext.RequestServices.GetRequiredService<IDataQualityRevenueService>();
        await revenue.RunPeriodicRevenueScanAsync(tenantId, cancellationToken);
        var tasks = await dq.ScanAndCreateTasksAsync(tenantId, cancellationToken);
        return Ok(new { dataQualityTasksCreated = tasks });
    }
}
