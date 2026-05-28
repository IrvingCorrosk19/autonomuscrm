using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/customer")]
[Authorize]
public class CustomerEngagementController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ExecutiveCustomerDashboardDto>> GetDashboard(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IExecutiveCustomerDashboardService>();
        return Ok(await svc.GetDashboardAsync(tenantId, cancellationToken));
    }

    [HttpGet("health")]
    public async Task<ActionResult<IReadOnlyList<CustomerHealthDto>>> GetHealth(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ICustomerHealthEngine>();
        return Ok(await svc.CalculateAllAsync(tenantId, cancellationToken));
    }

    [HttpGet("health/{customerId:guid}")]
    public async Task<ActionResult<CustomerHealthDto>> GetCustomerHealth(
        [FromQuery] Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ICustomerHealthEngine>();
        return Ok(await svc.CalculateHealthAsync(tenantId, customerId, cancellationToken));
    }

    [HttpGet("churn-signals")]
    public async Task<ActionResult<IReadOnlyList<ChurnRiskSignalDto>>> GetChurnSignals(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IChurnRiskEngine>();
        return Ok(await svc.DetectSignalsAsync(tenantId, cancellationToken: cancellationToken));
    }

    [HttpGet("renewals")]
    public async Task<ActionResult<IReadOnlyList<RenewalAlertDto>>> GetRenewals(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IRenewalEngine>();
        return Ok(await svc.GetUpcomingRenewalsAsync(tenantId, cancellationToken));
    }

    [HttpGet("renewal-forecast")]
    public async Task<ActionResult<RenewalForecastDto>> GetRenewalForecast(
        [FromQuery] Guid tenantId, [FromQuery] int horizonDays = 90, CancellationToken cancellationToken = default)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IRenewalEngine>();
        return Ok(await svc.GetRenewalForecastAsync(tenantId, horizonDays, cancellationToken));
    }

    [HttpGet("expansion")]
    public async Task<ActionResult<IReadOnlyList<ExpansionOpportunityDto>>> GetExpansion(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IExpansionRevenueEngine>();
        return Ok(await svc.DetectOpportunitiesAsync(tenantId, cancellationToken));
    }

    [HttpGet("journey")]
    public async Task<ActionResult<IReadOnlyList<JourneyStageMetricDto>>> GetJourney(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ICustomerJourneyEngine>();
        return Ok(await svc.GetJourneyMetricsAsync(tenantId, cancellationToken));
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<CustomerKpiSnapshotDto>> GetKpis(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ICustomerKpiService>();
        return Ok(await svc.GetSnapshotAsync(tenantId, cancellationToken));
    }

    [HttpPost("playbooks/{playbookType}")]
    public async Task<ActionResult<PlaybookExecutionDto>> RunPlaybook(
        [FromQuery] Guid tenantId,
        [FromQuery] Guid customerId,
        string playbookType,
        CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ICustomerPlaybookService>();
        return Ok(await svc.ExecutePlaybookAsync(tenantId, customerId, playbookType, cancellationToken: cancellationToken));
    }

    [HttpPost("scan")]
    public async Task<ActionResult> RunRetentionScan([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var engine = HttpContext.RequestServices.GetRequiredService<IRetentionAutomationEngine>();
        await engine.RunPeriodicRetentionScanAsync(tenantId, cancellationToken);
        return Ok(new { status = "completed" });
    }

    [HttpPost("intelligence/{agent}/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CustomerIntelligenceActionDto>>> RunIntelligence(
        [FromQuery] Guid tenantId,
        string agent,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<ICustomerSuccessIntelligenceService>();
        var result = agent.ToLowerInvariant() switch
        {
            "health" => await svc.RunHealthIntelligenceAsync(tenantId, customerId, cancellationToken),
            "churn" => await svc.RunChurnIntelligenceAsync(tenantId, customerId, cancellationToken),
            "renewal" => await svc.RunRenewalIntelligenceAsync(tenantId, customerId, cancellationToken),
            "expansion" => await svc.RunExpansionIntelligenceAsync(tenantId, customerId, cancellationToken),
            _ => Array.Empty<CustomerIntelligenceActionDto>()
        };
        return Ok(result);
    }
}
