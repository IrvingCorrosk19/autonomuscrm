using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using AutonomusCRM.Infrastructure.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IMetricsService _metricsService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        IMetricsService metricsService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _metricsService = metricsService;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        var health = await _healthCheckService.CheckHealthAsync();
        
        var result = new
        {
            status = health.Status.ToString(),
            totalDuration = health.TotalDuration,
            entries = health.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                data = e.Value.Data,
                tags = e.Value.Tags
            })
        };

        var statusCode = health.Status == HealthStatus.Healthy ? 200 : 503;
        return StatusCode(statusCode, result);
    }

    [HttpGet("metrics")]
    public ActionResult GetMetrics()
    {
        if (_metricsService is MetricsService metricsService)
        {
            var metrics = metricsService.GetMetrics();
            return Ok(metrics);
        }

        return Ok(new { message = ApiLocalization.Text(_localizer, "Api_Error_MetricsUnavailable") });
    }
}
