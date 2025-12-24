using AutonomusCRM.Infrastructure.Metrics;
using AutonomusCRM.Infrastructure.Persistence.TimeSeries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly ITimeSeriesRepository _timeSeriesRepository;
    private readonly IMetricsService _metricsService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        ITimeSeriesRepository timeSeriesRepository,
        IMetricsService metricsService,
        ILogger<MetricsController> logger)
    {
        _timeSeriesRepository = timeSeriesRepository;
        _metricsService = metricsService;
        _logger = logger;
    }

    [HttpGet("timeseries/{tenantId}/{metricName}")]
    public async Task<ActionResult> GetTimeSeriesMetrics(
        Guid tenantId,
        string metricName,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var metrics = await _timeSeriesRepository.GetMetricsAsync(tenantId, metricName, from, to, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("timeseries/{tenantId}/{metricName}/aggregated")]
    public async Task<ActionResult> GetAggregatedMetrics(
        Guid tenantId,
        string metricName,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] TimeSpan interval,
        CancellationToken cancellationToken)
    {
        var aggregated = await _timeSeriesRepository.GetAggregatedMetricsAsync(
            tenantId, metricName, from, to, interval, cancellationToken);
        return Ok(aggregated);
    }

    [HttpPost("timeseries")]
    public async Task<ActionResult> RecordMetric(
        [FromBody] TimeSeriesMetric metric,
        CancellationToken cancellationToken)
    {
        metric.Timestamp = DateTime.UtcNow;
        await _timeSeriesRepository.RecordMetricAsync(metric, cancellationToken);
        return CreatedAtAction(nameof(GetTimeSeriesMetrics), new { tenantId = metric.TenantId, metricName = metric.MetricName }, metric);
    }
}

