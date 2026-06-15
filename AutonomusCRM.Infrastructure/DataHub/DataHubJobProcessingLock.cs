using System.Collections.Concurrent;

namespace AutonomusCRM.Infrastructure.DataHub;

public static class DataHubJobProcessingLock
{
    private static readonly ConcurrentDictionary<Guid, byte> Active = new();

    public static bool TryAcquire(Guid jobId) => Active.TryAdd(jobId, 0);

    public static void Release(Guid jobId) => Active.TryRemove(jobId, out _);

    public static bool IsActive(Guid jobId) => Active.ContainsKey(jobId);
}
