using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class TenantIntegrationRepository : ITenantIntegrationRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IIntegrationTokenProtector _protector;

    public TenantIntegrationRepository(ApplicationDbContext db, IIntegrationTokenProtector protector)
    {
        _db = db;
        _protector = protector;
    }

    public async Task<TenantIntegrationConnection?> GetAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default)
    {
        var row = await _db.TenantIntegrations
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Provider == provider, cancellationToken);
        return row is null ? null : DecryptConnection(row);
    }

    public async Task<IReadOnlyList<TenantIntegrationConnection>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.TenantIntegrations.Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        return rows.Select(DecryptConnection).ToList();
    }

    public async Task UpsertAsync(TenantIntegrationConnection connection, CancellationToken cancellationToken = default)
    {
        var access = _protector.Protect(connection.AccessToken);
        var refresh = _protector.Protect(connection.RefreshToken);
        var existing = await _db.TenantIntegrations
            .FirstOrDefaultAsync(x => x.TenantId == connection.TenantId && x.Provider == connection.Provider, cancellationToken);
        if (existing == null)
        {
            connection.Configure(access, refresh, connection.InstanceUrl, connection.Settings);
            await _db.TenantIntegrations.AddAsync(connection, cancellationToken);
        }
        else
        {
            existing.Configure(access, refresh, connection.InstanceUrl, connection.Settings);
            if (!string.IsNullOrWhiteSpace(connection.LastSyncStatus))
                existing.MarkSync(connection.LastSyncStatus!);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private TenantIntegrationConnection DecryptConnection(TenantIntegrationConnection row)
    {
        row.Configure(
            _protector.Unprotect(row.AccessToken),
            _protector.Unprotect(row.RefreshToken),
            row.InstanceUrl,
            row.Settings);
        return row;
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
