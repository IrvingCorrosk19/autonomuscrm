using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class TenantIntegrationRepository : ITenantIntegrationRepository
{
    private readonly ApplicationDbContext _db;

    public TenantIntegrationRepository(ApplicationDbContext db) => _db = db;

    public Task<TenantIntegrationConnection?> GetAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default)
        => _db.TenantIntegrations
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Provider == provider, cancellationToken);

    public async Task<IReadOnlyList<TenantIntegrationConnection>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _db.TenantIntegrations.Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);

    public async Task UpsertAsync(TenantIntegrationConnection connection, CancellationToken cancellationToken = default)
    {
        var existing = await _db.TenantIntegrations
            .FirstOrDefaultAsync(x => x.TenantId == connection.TenantId && x.Provider == connection.Provider, cancellationToken);
        if (existing == null)
        {
            await _db.TenantIntegrations.AddAsync(connection, cancellationToken);
        }
        else
        {
            existing.Configure(connection.AccessToken, connection.RefreshToken, connection.InstanceUrl, connection.Settings);
            if (!string.IsNullOrWhiteSpace(connection.LastSyncStatus))
                existing.MarkSync(connection.LastSyncStatus!);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class IntegrationHubService : IIntegrationHubService
{
    private readonly ITenantIntegrationRepository _repo;
    private readonly IEnumerable<IIntegrationConnector> _connectors;

    public IntegrationHubService(ITenantIntegrationRepository repo, IEnumerable<IIntegrationConnector> connectors)
    {
        _repo = repo;
        _connectors = connectors;
    }

    public async Task ConnectAsync(Guid tenantId, ConnectIntegrationRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetAsync(tenantId, request.Provider, cancellationToken)
            ?? TenantIntegrationConnection.Create(tenantId, request.Provider);
        existing.Configure(request.AccessToken, request.RefreshToken, request.InstanceUrl, request.Settings);
        await _repo.UpsertAsync(existing, cancellationToken);
    }

    public Task<IReadOnlyList<TenantIntegrationConnection>> ListConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _repo.ListAsync(tenantId, cancellationToken);

    public async Task<IntegrationSyncResultDto> SyncProviderAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default)
    {
        var connector = _connectors.FirstOrDefault(c => c.Provider == provider)
            ?? throw new InvalidOperationException($"Unknown provider: {provider}");
        return await connector.SyncBidirectionalAsync(tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<IntegrationSyncResultDto>> SyncAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var results = new List<IntegrationSyncResultDto>();
        foreach (var connector in _connectors)
            results.Add(await connector.SyncBidirectionalAsync(tenantId, cancellationToken));
        return results;
    }
}
