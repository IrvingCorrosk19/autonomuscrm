using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI.Llm;

/// <summary>
/// Resilient LLM orchestrator: provider selection, retry, circuit breaker, rate limit, usage tracking.
/// </summary>
public sealed class ResilientLlmProvider : ILLMProvider, ILlmUsageTracker
{
    private readonly IReadOnlyDictionary<string, ILlmProviderImplementation> _providers;
    private readonly AiOptions _options;
    private readonly ILogger<ResilientLlmProvider> _logger;
    private readonly LlmUsageTracker _usage = new();
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();
    private readonly ConcurrentDictionary<string, RateWindow> _rateWindows = new();

    public ResilientLlmProvider(
        IEnumerable<ILlmProviderImplementation> providers,
        IOptions<AiOptions> options,
        ILogger<ResilientLlmProvider> logger)
    {
        _providers = providers.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new LlmNotConfiguredException("AI is disabled (AI:Enabled=false). Configure a provider to enable.");

        var chain = BuildProviderChain();
        if (chain.Count == 0)
            throw new LlmNotConfiguredException(
                "No LLM provider configured. Set AI:OpenAI:ApiKey, AI:AzureOpenAI, AI:Anthropic:ApiKey, or AI:Gemini:ApiKey.");

        Exception? lastError = null;
        foreach (var providerId in chain)
        {
            if (!_providers.TryGetValue(providerId, out var provider) || !provider.IsConfigured)
                continue;

            if (IsCircuitOpen(providerId))
            {
                _logger.LogDebug("Circuit open for {Provider}, skipping", providerId);
                continue;
            }

            EnforceRateLimit(providerId);

            for (var attempt = 1; attempt <= _options.MaxRetries; attempt++)
            {
                try
                {
                    var result = await provider.CompleteAsync(request, cancellationToken);
                    RecordSuccess(providerId);
                    _usage.Record(new LlmUsageRecord
                    {
                        Provider = result.Provider ?? providerId,
                        Model = result.Model ?? "",
                        PromptTokens = result.TokensUsed / 2,
                        CompletionTokens = result.TokensUsed / 2,
                        EstimatedCostUsd = EstimateCost(providerId, result.TokensUsed),
                        Success = true
                    });
                    return result;
                }
                catch (LlmNotConfiguredException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    RecordFailure(providerId);
                    _usage.Record(new LlmUsageRecord
                    {
                        Provider = providerId,
                        Model = request.Model ?? "",
                        Success = false,
                        Error = ex.Message
                    });
                    _logger.LogWarning(ex, "LLM {Provider} attempt {Attempt}/{Max} failed", providerId, attempt, _options.MaxRetries);
                    if (attempt < _options.MaxRetries)
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                }
            }
        }

        throw new LlmProviderUnavailableException(
            chain[0],
            lastError?.Message ?? "All configured LLM providers failed or circuits are open.");
    }

    public void Record(LlmUsageRecord record) => _usage.Record(record);

    public IReadOnlyList<LlmUsageRecord> GetRecent(int count = 50) => _usage.GetRecent(count);

    public LlmRuntimeHealthSnapshot GetHealth()
    {
        var configured = _providers.Values.Where(p => p.IsConfigured).Select(p => p.ProviderId).ToList();
        var openCircuits = _circuits.Where(c => IsCircuitOpen(c.Key)).Select(c => c.Key).ToList();
        var baseHealth = _usage.GetHealth();
        return new LlmRuntimeHealthSnapshot(
            _options.Provider,
            configured,
            openCircuits,
            baseHealth.TotalRequests,
            baseHealth.TotalFailures,
            baseHealth.TotalTokens);
    }

    private List<string> BuildProviderChain()
    {
        var chain = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.Provider))
            chain.Add(_options.Provider.Trim().ToLowerInvariant());
        foreach (var fb in _options.FallbackProviders ?? Array.Empty<string>())
        {
            var id = fb.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(id) && !chain.Contains(id, StringComparer.OrdinalIgnoreCase))
                chain.Add(id);
        }
        return chain;
    }

    private bool IsCircuitOpen(string providerId)
    {
        if (!_circuits.TryGetValue(providerId, out var state))
            return false;
        if (state.Failures < _options.CircuitBreakerFailureThreshold)
            return false;
        if (DateTime.UtcNow - state.LastFailureUtc > TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds))
        {
            _circuits.TryRemove(providerId, out _);
            return false;
        }
        return true;
    }

    private void RecordFailure(string providerId)
    {
        _circuits.AddOrUpdate(providerId,
            _ => new CircuitState(1, DateTime.UtcNow),
            (_, s) => new CircuitState(s.Failures + 1, DateTime.UtcNow));
    }

    private void RecordSuccess(string providerId) => _circuits.TryRemove(providerId, out _);

    private void EnforceRateLimit(string providerId)
    {
        var window = _rateWindows.GetOrAdd(providerId, _ => new RateWindow());
        lock (window)
        {
            var now = DateTime.UtcNow;
            if ((now - window.WindowStart).TotalMinutes >= 1)
            {
                window.WindowStart = now;
                window.Count = 0;
            }
            window.Count++;
            if (window.Count > _options.RequestsPerMinuteLimit)
                throw new LlmProviderUnavailableException(providerId, "Rate limit exceeded for provider.");
        }
    }

    private static decimal EstimateCost(string providerId, int tokens) =>
        providerId switch
        {
            "openai" or "azure-openai" => tokens * 0.000002m,
            "anthropic" => tokens * 0.000003m,
            "gemini" => tokens * 0.000001m,
            _ => 0m
        };

    private sealed record CircuitState(int Failures, DateTime LastFailureUtc);

    private sealed class RateWindow
    {
        public DateTime WindowStart = DateTime.UtcNow;
        public int Count;
    }
}
