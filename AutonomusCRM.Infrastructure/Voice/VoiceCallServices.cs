using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Voice;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Voice;

public sealed class VoiceCallLogRepository : IVoiceCallLogRepository
{
    private readonly ApplicationDbContext _db;

    public VoiceCallLogRepository(ApplicationDbContext db) => _db = db;

    public async Task AddAsync(VoiceCallLog log, CancellationToken cancellationToken = default)
    {
        await _db.VoiceCallLogs.AddAsync(log, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<VoiceCallLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.VoiceCallLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VoiceCallLog>> ListAsync(Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.VoiceCallLogs.Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.StartedAt).Take(take).ToListAsync(cancellationToken);
}

public sealed class VoiceCallService : IVoiceCallService
{
    private readonly IVoiceCallLogRepository _repo;
    private readonly ApplicationDbContext _db;
    private readonly IOperationalGraphFeed _graphFeed;

    public VoiceCallService(IVoiceCallLogRepository repo, ApplicationDbContext db, IOperationalGraphFeed graphFeed)
    {
        _repo = repo;
        _db = db;
        _graphFeed = graphFeed;
    }

    public async Task<VoiceCallLog> LogCallAsync(VoiceCallLog log, CancellationToken cancellationToken = default)
    {
        await _repo.AddAsync(log, cancellationToken);
        await _graphFeed.RecordVoiceCallAsync(log.TenantId, log.Id, log.CustomerId, log.Outcome, log.AiSummary, cancellationToken);
        return log;
    }

    public async Task<IReadOnlyList<VoiceCallLogDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var logs = await _repo.ListAsync(tenantId, 100, cancellationToken);
        var result = new List<VoiceCallLogDto>();
        foreach (var log in logs)
        {
            string? customerName = null, leadName = null, dealTitle = null;
            if (log.CustomerId.HasValue)
                customerName = (await _db.Customers.FindAsync(new object[] { log.CustomerId.Value }, cancellationToken))?.Name;
            if (log.LeadId.HasValue)
                leadName = (await _db.Leads.FindAsync(new object[] { log.LeadId.Value }, cancellationToken))?.Name;
            if (log.DealId.HasValue)
                dealTitle = (await _db.Deals.FindAsync(new object[] { log.DealId.Value }, cancellationToken))?.Title;

            result.Add(new VoiceCallLogDto(log.Id, log.PhoneNumber, log.Direction, log.DurationSeconds, log.Outcome,
                customerName, leadName, dealTitle, log.StartedAt, log.TranscriptStatus, log.AiSummary));
        }

        return result;
    }
}
