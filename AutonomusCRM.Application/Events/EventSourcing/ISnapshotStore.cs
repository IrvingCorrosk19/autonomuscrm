namespace AutonomusCRM.Application.Events.EventSourcing;

/// <summary>
/// Interfaz para Snapshot Store (definida en Application para evitar dependencia circular)
/// </summary>
public interface ISnapshotStore
{
    Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version, CancellationToken cancellationToken = default) where T : class;
    Task<T?> GetSnapshotAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : class;
    Task<int?> GetSnapshotVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}


