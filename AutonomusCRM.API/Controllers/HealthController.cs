using AutonomusCRM.Infrastructure.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IMetricsService _metricsService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        IMetricsService metricsService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _metricsService = metricsService;
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

        return Ok(new { message = "Metrics service not available" });
    }
}

