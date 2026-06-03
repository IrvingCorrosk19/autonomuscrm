using AutonomusCRM.Application.Events;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Events;

public sealed class FailedEventReplayService : IFailedEventReplayService
{
    private readonly ApplicationDbContext _db;

    public FailedEventReplayService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FailedEventListItemDto>> ListAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _db.FailedEventMessages.AsNoTracking()
            .Where(m => m.TenantId == tenantId || m.TenantId == null)
            .OrderByDescending(m => m.FailedAt)
            .Take(take)
            .Select(m => new FailedEventListItemDto(
                m.Id, m.TenantId, m.MessageId, m.EventType, m.FailedAt, m.RetryCount, m.Error))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkReplayRequestedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.FailedEventMessages.FirstOrDefaultAsync(
            m => m.Id == id && (m.TenantId == tenantId || m.TenantId == null), cancellationToken);
        if (row is null) return false;
        row.Error = $"[replay-requested {DateTime.UtcNow:O}] {row.Error}";
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
