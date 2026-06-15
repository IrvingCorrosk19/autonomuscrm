using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncScheduleService : IDbSyncScheduleService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IDbSyncOrchestrator _orchestrator;
    private readonly IDbIntelligenceAuditService _audit;

    public DbSyncScheduleService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IDbSyncOrchestrator orchestrator,
        IDbIntelligenceAuditService audit)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _orchestrator = orchestrator;
        _audit = audit;
    }

    public async Task<DbSyncScheduleDto> CreateScheduleAsync(
        Guid tenantId, Guid userId, ScheduleDbSyncRequest request,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var schedule = new DbSyncSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = request.ConnectionId,
            CreatedByUserId = userId,
            Name = request.Name,
            SyncMode = string.IsNullOrWhiteSpace(request.SyncMode) ? DbSyncMode.Full : request.SyncMode,
            Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? DbSyncScheduleFrequency.Daily : request.Frequency,
            ConflictPolicy = string.IsNullOrWhiteSpace(request.ConflictPolicy) ? DbSyncConflictPolicy.SourceWins : request.ConflictPolicy,
            RunOnceAt = request.RunOnceAt,
            NextRunAt = ComputeNextRun(request.Frequency, request.RunOnceAt)
        };
        _db.DbSyncSchedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.SyncScheduleCreated, userId, request.ConnectionId,
            null, null, null, true, ipAddress, userAgent), cancellationToken);

        return Map(schedule);
    }

    public async Task<IReadOnlyList<DbSyncScheduleDto>> ListSchedulesAsync(
        Guid tenantId, Guid? connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var query = _db.DbSyncSchedules.AsNoTracking().Where(s => s.TenantId == tenantId);
        if (connectionId.HasValue)
            query = query.Where(s => s.ConnectionProfileId == connectionId.Value);
        var items = await query.OrderByDescending(s => s.CreatedAtUtc).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await RecoverExpiredLeasesAsync(now, cancellationToken);

        var due = await _db.DbSyncSchedules
            .Where(s => s.IsEnabled && !s.IsRunning && s.NextRunAt != null && s.NextRunAt <= now)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var schedule in due)
            await ExecuteScheduleAsync(schedule, cancellationToken);
    }

    private async Task ExecuteScheduleAsync(DbSyncSchedule schedule, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid();
        var leaseUntil = DateTime.UtcNow.AddMinutes(15);
        schedule.IsRunning = true;
        schedule.RunningLeaseUntil = leaseUntil;
        schedule.ActiveRunId = runId;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var job = schedule.SyncMode == DbSyncMode.Delta
                ? await _orchestrator.StartDeltaSyncAsync(
                    schedule.TenantId, schedule.CreatedByUserId, schedule.ConnectionProfileId,
                    schedule.ConflictPolicy, null, null, cancellationToken)
                : await _orchestrator.StartFullSyncAsync(
                    schedule.TenantId, schedule.CreatedByUserId, schedule.ConnectionProfileId,
                    schedule.ConflictPolicy, null, null, cancellationToken);

            schedule.LastRunAt = DateTime.UtcNow;
            schedule.NextRunAt = schedule.Frequency == DbSyncScheduleFrequency.Once
                ? null
                : ComputeNextRun(schedule.Frequency, null, schedule.LastRunAt);
            if (schedule.Frequency == DbSyncScheduleFrequency.Once)
                schedule.IsEnabled = false;
        }
        finally
        {
            schedule.IsRunning = false;
            schedule.RunningLeaseUntil = null;
            schedule.ActiveRunId = null;
            schedule.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RecoverExpiredLeasesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var expired = await _db.DbSyncSchedules
            .Where(s => s.IsRunning && s.RunningLeaseUntil != null && s.RunningLeaseUntil < now)
            .ToListAsync(cancellationToken);
        foreach (var s in expired)
        {
            s.IsRunning = false;
            s.RunningLeaseUntil = null;
            s.ActiveRunId = null;
        }
        if (expired.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }

    private static DateTime? ComputeNextRun(string frequency, DateTime? runOnceAt, DateTime? lastRun = null)
    {
        if (frequency == DbSyncScheduleFrequency.Once)
            return runOnceAt ?? DateTime.UtcNow;

        var baseTime = lastRun ?? DateTime.UtcNow;
        return frequency switch
        {
            DbSyncScheduleFrequency.Hourly => baseTime.AddHours(1),
            DbSyncScheduleFrequency.Daily => baseTime.AddDays(1),
            DbSyncScheduleFrequency.Weekly => baseTime.AddDays(7),
            _ => baseTime.AddDays(1)
        };
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    private static DbSyncScheduleDto Map(DbSyncSchedule s) => new(
        s.Id, s.TenantId, s.ConnectionProfileId, s.Name, s.SyncMode, s.Frequency,
        s.ConflictPolicy, s.IsEnabled, s.NextRunAt, s.LastRunAt, s.CreatedAtUtc);
}
