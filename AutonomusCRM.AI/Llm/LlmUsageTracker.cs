using System.Collections.Concurrent;

namespace AutonomusCRM.AI.Llm;

public sealed class LlmUsageTracker : ILlmUsageTracker
{
    private readonly ConcurrentQueue<LlmUsageRecord> _records = new();
    private long _requests;
    private long _failures;
    private long _tokens;

    public void Record(LlmUsageRecord record)
    {
        Interlocked.Increment(ref _requests);
        if (!record.Success)
            Interlocked.Increment(ref _failures);
        Interlocked.Add(ref _tokens, record.TotalTokens);
        _records.Enqueue(record);
        while (_records.Count > 500 && _records.TryDequeue(out _)) { }
    }

    public IReadOnlyList<LlmUsageRecord> GetRecent(int count = 50) =>
        _records.Reverse().Take(count).ToList();

    public LlmRuntimeHealthSnapshot GetHealth() =>
        new("", Array.Empty<string>(), Array.Empty<string>(), _requests, _failures, _tokens);
}
