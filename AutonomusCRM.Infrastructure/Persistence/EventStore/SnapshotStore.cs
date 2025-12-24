using AutonomusCRM.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Persistence.EventStore;

/// <summary>
/// Almacén de snapshots para Event Sourcing
/// </summary>
public interface ISnapshotStore
{
    Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version, CancellationToken cancellationToken = default) where T : class;
    Task<T?> GetSnapshotAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : class;
    Task<int?> GetSnapshotVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}

public class SnapshotStore : ISnapshotStore
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SnapshotStore> _logger;

    public SnapshotStore(
        ApplicationDbContext context,
        ILogger<SnapshotStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version, CancellationToken cancellationToken = default) where T : class
    {
        var snapshotJson = System.Text.Json.JsonSerializer.Serialize(snapshot);
        var snapshotType = typeof(T).FullName ?? typeof(T).Name;

        var existingSnapshot = await _context.Snapshots
            .FirstOrDefaultAsync(s => s.AggregateId == aggregateId && s.AggregateType == snapshotType, cancellationToken);

        if (existingSnapshot != null)
        {
            existingSnapshot.SnapshotData = snapshotJson;
            existingSnapshot.Version = version;
            existingSnapshot.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var newSnapshot = new Snapshot
            {
                Id = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = snapshotType,
                SnapshotData = snapshotJson,
                Version = version,
                CreatedAt = DateTime.UtcNow
            };
            _context.Snapshots.Add(newSnapshot);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved snapshot for aggregate {AggregateId} at version {Version}", aggregateId, version);
    }

    public async Task<T?> GetSnapshotAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : class
    {
        var snapshotType = typeof(T).FullName ?? typeof(T).Name;

        var snapshot = await _context.Snapshots
            .Where(s => s.AggregateId == aggregateId && s.AggregateType == snapshotType)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot == null)
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(snapshot.SnapshotData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing snapshot for aggregate {AggregateId}", aggregateId);
            return null;
        }
    }

    public async Task<int?> GetSnapshotVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.Snapshots
            .Where(s => s.AggregateId == aggregateId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot?.Version;
    }
}

public class Snapshot
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string SnapshotData { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

