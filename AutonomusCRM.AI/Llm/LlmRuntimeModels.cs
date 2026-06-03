namespace AutonomusCRM.AI.Llm;

public sealed class LlmUsageRecord
{
    public string Provider { get; init; } = "";
    public string Model { get; init; } = "";
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal EstimatedCostUsd { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public bool Success { get; init; }
    public string? Error { get; init; }
}

public interface ILlmUsageTracker
{
    void Record(LlmUsageRecord record);
    IReadOnlyList<LlmUsageRecord> GetRecent(int count = 50);
    LlmRuntimeHealthSnapshot GetHealth();
}

public sealed record LlmRuntimeHealthSnapshot(
    string ActiveProvider,
    IReadOnlyList<string> ConfiguredProviders,
    IReadOnlyList<string> OpenCircuits,
    long TotalRequests,
    long TotalFailures,
    long TotalTokens);

public sealed class LlmNotConfiguredException : InvalidOperationException
{
    public LlmNotConfiguredException(string message) : base(message) { }
}

public sealed class LlmProviderUnavailableException : Exception
{
    public LlmProviderUnavailableException(string provider, string message) : base($"LLM provider '{provider}': {message}") { }
}

public interface ILlmProviderImplementation
{
    string ProviderId { get; }
    bool IsConfigured { get; }
    Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken);
}
