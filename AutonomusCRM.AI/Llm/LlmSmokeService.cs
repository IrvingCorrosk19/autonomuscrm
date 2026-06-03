using AutonomusCRM.AI;
using AutonomusCRM.AI.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI.Llm;

public enum LlmSmokeStatus
{
    NotConfigured,
    Configured,
    Success,
    ProviderUnavailable,
    RateLimited,
    InvalidKey,
    BlockedNoLiveOptIn
}

public sealed record LlmProviderHealthDto(string ProviderId, bool IsConfigured, bool CircuitOpen);

public sealed record LlmHealthResponseDto(
    bool Enabled,
    string PrimaryProvider,
    IReadOnlyList<LlmProviderHealthDto> Providers,
    long TotalRequests,
    long TotalFailures,
    long TotalTokens,
    IReadOnlyList<string> OpenCircuits);

public sealed record LlmSmokeResponseDto(
    string Provider,
    LlmSmokeStatus Status,
    string Message,
    bool LiveAttempted,
    int? TokensUsed);

public interface ILlmSmokeService
{
    LlmHealthResponseDto GetHealth();
    Task<LlmSmokeResponseDto> SmokeAsync(string? providerId, CancellationToken cancellationToken = default);
}

public sealed class LlmSmokeService : ILlmSmokeService
{
    private readonly IEnumerable<ILlmProviderImplementation> _providers;
    private readonly ILlmUsageTracker _usage;
    private readonly AiOptions _options;
    private readonly ILogger<LlmSmokeService> _logger;

    public LlmSmokeService(
        IEnumerable<ILlmProviderImplementation> providers,
        ILlmUsageTracker usage,
        IOptions<AiOptions> options,
        ILogger<LlmSmokeService> logger)
    {
        _providers = providers;
        _usage = usage;
        _options = options.Value;
        _logger = logger;
    }

    public LlmHealthResponseDto GetHealth()
    {
        var runtime = _usage.GetHealth();
        var open = runtime.OpenCircuits.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var providerHealth = _providers.Select(p => new LlmProviderHealthDto(
            p.ProviderId, p.IsConfigured, open.Contains(p.ProviderId))).ToList();

        return new LlmHealthResponseDto(
            _options.Enabled,
            _options.Provider,
            providerHealth,
            runtime.TotalRequests,
            runtime.TotalFailures,
            runtime.TotalTokens,
            runtime.OpenCircuits);
    }

    public async Task<LlmSmokeResponseDto> SmokeAsync(string? providerId, CancellationToken cancellationToken = default)
    {
        var target = string.IsNullOrWhiteSpace(providerId) ? _options.Provider : providerId;
        var impl = _providers.FirstOrDefault(p => p.ProviderId.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (impl == null)
            return new LlmSmokeResponseDto(target, LlmSmokeStatus.ProviderUnavailable, "Unknown provider", false, null);

        if (!impl.IsConfigured)
            return new LlmSmokeResponseDto(target, LlmSmokeStatus.NotConfigured, $"Provider '{target}' has no API key configured", false, null);

        var liveOptIn = string.Equals(Environment.GetEnvironmentVariable("INTEGRATION_SMOKE_LIVE"), "1", StringComparison.Ordinal);
        if (!liveOptIn)
            return new LlmSmokeResponseDto(target, LlmSmokeStatus.Configured, "Credentials present — set INTEGRATION_SMOKE_LIVE=1 to run live call", false, null);

        try
        {
            var result = await impl.CompleteAsync(
                new LlmCompletionRequest("You are a health check.", "Reply with OK only.", null, 16),
                cancellationToken);

            if (result.IsPlaceholder)
                return new LlmSmokeResponseDto(target, LlmSmokeStatus.InvalidKey, "Placeholder response detected", true, null);

            return new LlmSmokeResponseDto(target, LlmSmokeStatus.Success, result.Content[..Math.Min(80, result.Content.Length)], true, result.TokensUsed);
        }
        catch (LlmProviderUnavailableException ex) when (ex.Message.Contains("Rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return new LlmSmokeResponseDto(target, LlmSmokeStatus.RateLimited, ex.Message, true, null);
        }
        catch (LlmProviderUnavailableException ex) when (ex.Message.Contains("401", StringComparison.Ordinal) || ex.Message.Contains("403", StringComparison.Ordinal))
        {
            return new LlmSmokeResponseDto(target, LlmSmokeStatus.InvalidKey, ex.Message, true, null);
        }
        catch (LlmProviderUnavailableException ex)
        {
            _logger.LogWarning(ex, "LLM smoke failed for {Provider}", target);
            return new LlmSmokeResponseDto(target, LlmSmokeStatus.ProviderUnavailable, ex.Message, true, null);
        }
        catch (LlmNotConfiguredException ex)
        {
            return new LlmSmokeResponseDto(target, LlmSmokeStatus.NotConfigured, ex.Message, true, null);
        }
    }
}
