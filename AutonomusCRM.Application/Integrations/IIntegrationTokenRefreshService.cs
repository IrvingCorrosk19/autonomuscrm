namespace AutonomusCRM.Application.Integrations;

public interface IIntegrationTokenRefreshService
{
    Task<int> RefreshExpiringTokensAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> RefreshProviderAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default);
}

public record SyncConflictDto(
    string Provider,
    string EntityType,
    string ExternalId,
    string ConflictType,
    string Message);

public interface ISyncConflictService
{
    Task<IReadOnlyList<SyncConflictDto>> DetectConflictsAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default);
}
