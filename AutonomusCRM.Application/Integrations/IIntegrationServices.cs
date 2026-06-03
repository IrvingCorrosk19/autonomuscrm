namespace AutonomusCRM.Application.Integrations;

public interface ITenantIntegrationRepository
{
    Task<TenantIntegrationConnection?> GetAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantIntegrationConnection>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task UpsertAsync(TenantIntegrationConnection connection, CancellationToken cancellationToken = default);
}

public interface IIntegrationConnector
{
    string Provider { get; }
    Task<IntegrationSyncResultDto> SyncBidirectionalAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IIntegrationHubService
{
    Task ConnectAsync(Guid tenantId, ConnectIntegrationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantIntegrationConnection>> ListConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IntegrationSyncResultDto> SyncProviderAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IntegrationSyncResultDto>> SyncAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
