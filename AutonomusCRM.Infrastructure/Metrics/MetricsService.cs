using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Metrics;

/// <summary>
/// Servicio de métricas para observabilidad
/// </summary>
public interface IMetricsService
{
    void IncrementCounter(string name, Dictionary<string, string>? tags = null);
    void RecordGauge(string name, double value, Dictionary<string, string>? tags = null);
    void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);
    void RecordTimer(string name, TimeSpan duration, Dictionary<string, string>? tags = null);
}

/// <summary>
/// Implementación básica de métricas (preparada para Prometheus)
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly Dictionary<string, long> _counters = new();
    private readonly Dictionary<string, double> _gauges = new();
    private readonly Dictionary<string, List<double>> _histograms = new();
    private readonly Dictionary<string, List<TimeSpan>> _timers = new();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
    }

    public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        lock (_counters)
        {
            _counters[key] = _counters.GetValueOrDefault(key, 0) + 1;
        }
        _logger.LogDebug("Counter {Name} incremented", name);
    }

    public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        lock (_gauges)
        {
            _gauges[key] = value;
        }
        _logger.LogDebug("Gauge {Name} set to {Value}", name, value);
    }

    public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        lock (_histograms)
        {
            if (!_histograms.ContainsKey(key))
                _histograms[key] = new List<double>();
            _histograms[key].Add(value);
        }
        _logger.LogDebug("Histogram {Name} recorded value {Value}", name, value);
    }

    public void RecordTimer(string name, TimeSpan duration, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        lock (_timers)
        {
            if (!_timers.ContainsKey(key))
                _timers[key] = new List<TimeSpan>();
            _timers[key].Add(duration);
        }
        _logger.LogDebug("Timer {Name} recorded duration {Duration}", name, duration);
    }

    public Dictionary<string, object> GetMetrics()
    {
        return new Dictionary<string, object>
        {
            ["counters"] = _counters,
            ["gauges"] = _gauges,
            ["histograms"] = _histograms.ToDictionary(kvp => kvp.Key, kvp => new
            {
                count = kvp.Value.Count,
                sum = kvp.Value.Sum(),
                avg = kvp.Value.Average(),
                min = kvp.Value.Min(),
                max = kvp.Value.Max()
            }),
            ["timers"] = _timers.ToDictionary(kvp => kvp.Key, kvp => new
            {
                count = kvp.Value.Count,
                total = kvp.Value.Sum(t => t.TotalMilliseconds),
                avg = kvp.Value.Average(t => t.TotalMilliseconds),
                min = kvp.Value.Min(t => t.TotalMilliseconds),
                max = kvp.Value.Max(t => t.TotalMilliseconds)
            })
        };
    }

    private string BuildKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return name;

        var tagString = string.Join(",", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}[{tagString}]";
    }
}

