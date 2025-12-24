using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Repositorio para series de tiempo
/// </summary>
public interface ITimeSeriesRepository
{
    Task RecordMetricAsync(TimeSeriesMetric metric, CancellationToken cancellationToken = default);
    Task<List<TimeSeriesMetric>> GetMetricsAsync(
        Guid tenantId,
        string metricName,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
    Task<Dictionary<string, double>> GetAggregatedMetricsAsync(
        Guid tenantId,
        string metricName,
        DateTime from,
        DateTime to,
        TimeSpan interval,
        CancellationToken cancellationToken = default);
}

public class TimeSeriesRepository : ITimeSeriesRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TimeSeriesRepository> _logger;

    public TimeSeriesRepository(
        ApplicationDbContext context,
        ILogger<TimeSeriesRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordMetricAsync(TimeSeriesMetric metric, CancellationToken cancellationToken = default)
    {
        _context.TimeSeriesMetrics.Add(metric);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<TimeSeriesMetric>> GetMetricsAsync(
        Guid tenantId,
        string metricName,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.TimeSeriesMetrics
            .Where(m => m.TenantId == tenantId &&
                       m.MetricName == metricName &&
                       m.Timestamp >= from &&
                       m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, double>> GetAggregatedMetricsAsync(
        Guid tenantId,
        string metricName,
        DateTime from,
        DateTime to,
        TimeSpan interval,
        CancellationToken cancellationToken = default)
    {
        var metrics = await GetMetricsAsync(tenantId, metricName, from, to, cancellationToken);

        var aggregated = metrics
            .GroupBy(m => new DateTime(
                m.Timestamp.Year,
                m.Timestamp.Month,
                m.Timestamp.Day,
                m.Timestamp.Hour,
                (m.Timestamp.Minute / (int)interval.TotalMinutes) * (int)interval.TotalMinutes,
                0))
            .ToDictionary(
                g => g.Key.ToString("yyyy-MM-dd HH:mm:ss"),
                g => g.Average(m => m.Value));

        return aggregated;
    }
}

public class TimeSeriesMetric
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

