using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataPlatform;

public sealed class CdpEventStreamService : ICdpEventStreamService
{
    private readonly ApplicationDbContext _db;

    public CdpEventStreamService(ApplicationDbContext db) => _db = db;

    public async Task PublishAsync(
        Guid tenantId, string eventType, Guid? customerId, Dictionary<string, object?> payload, CancellationToken cancellationToken = default)
    {
        var evt = CdpStreamEvent.Create(tenantId, eventType, customerId, payload);
        _db.CdpStreamEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CdpStreamEventDto>> GetRecentAsync(
        Guid tenantId, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _db.CdpStreamEvents
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(take)
            .Select(e => new CdpStreamEventDto(e.Id, e.EventType, e.CustomerId, e.OccurredAt, e.Payload))
            .ToListAsync(cancellationToken);
    }
}
