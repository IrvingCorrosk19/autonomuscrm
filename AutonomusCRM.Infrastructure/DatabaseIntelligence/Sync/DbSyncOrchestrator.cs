using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncOrchestrator : IDbSyncOrchestrator
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IBusinessDiscoveryService _businessDiscovery;
    private readonly IDbSyncPipeline _pipeline;
    private readonly IDbSyncDispatcher _dispatcher;
    private readonly IDbSyncRollbackService _rollback;
    private readonly IDbIntelligenceAuditService _audit;
    private readonly IDbIntelligenceSyncProgressNotifier _notifier;

    public DbSyncOrchestrator(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IBusinessDiscoveryService businessDiscovery,
        IDbSyncPipeline pipeline,
        IDbSyncDispatcher dispatcher,
        IDbSyncRollbackService rollback,
        IDbIntelligenceAuditService audit,
        IDbIntelligenceSyncProgressNotifier notifier)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _businessDiscovery = businessDiscovery;
        _pipeline = pipeline;
        _dispatcher = dispatcher;
        _rollback = rollback;
        _audit = audit;
        _notifier = notifier;
    }

    public Task<DbSyncJobDto> StartFullSyncAsync(
        Guid tenantId, Guid userId, Guid connectionId, string conflictPolicy,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) =>
        StartSyncAsync(tenantId, userId, connectionId, DbSyncMode.Full, conflictPolicy, ipAddress, userAgent, cancellationToken);

    public Task<DbSyncJobDto> StartDeltaSyncAsync(
        Guid tenantId, Guid userId, Guid connectionId, string conflictPolicy,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) =>
        StartSyncAsync(tenantId, userId, connectionId, DbSyncMode.Delta, conflictPolicy, ipAddress, userAgent, cancellationToken);

    public async Task<DbSyncJobDto?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbSyncJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);
        return job == null ? null : MapJob(job);
    }

    public async Task<IReadOnlyList<DbSyncHistoryItemDto>> GetHistoryAsync(
        Guid tenantId, Guid? connectionId, int take, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var query = _db.DbSyncJobs.AsNoTracking().Where(j => j.TenantId == tenantId);
        if (connectionId.HasValue)
            query = query.Where(j => j.ConnectionProfileId == connectionId.Value);

        return await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 200))
            .Select(j => new DbSyncHistoryItemDto(
                j.Id, j.SyncMode, j.Status, j.TotalRows, j.ImportedRows, j.ErrorRows,
                j.DurationMs, j.CreatedAtUtc, j.CompletedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<DbSyncRollbackResultDto> RollbackJobAsync(
        Guid tenantId, Guid userId, Guid jobId,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var connection = await _db.DbSyncJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.Id == jobId)
            .Select(j => j.ConnectionProfileId)
            .FirstOrDefaultAsync(cancellationToken);

        var result = await _rollback.ExecuteRollbackAsync(tenantId, jobId, cancellationToken);

        if (connection != Guid.Empty)
        {
            var profile = await _db.DbConnectionProfiles.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == connection, cancellationToken);
            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.SyncRolledBack, userId, connection,
                profile?.EngineType, profile != null ? DbIntelligenceMasking.MaskHost(profile.Host) : null,
                profile?.DatabaseName, true, ipAddress, userAgent), cancellationToken);
        }

        return result;
    }

    public async Task ProcessPendingJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _db.DbSyncJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null || job.Status != DbSyncJobStatus.Pending)
            return;

        _tenantAccessor.TenantId = job.TenantId;
        var started = DateTime.UtcNow;
        job.Status = DbSyncJobStatus.Running;
        job.StartedAtUtc = started;
        await _db.SaveChangesAsync(cancellationToken);

        await _notifier.NotifySyncStartedAsync(job.TenantId, job.Id, job.ConnectionProfileId, cancellationToken);

        try
        {
            var mappings = await BuildMappingsAsync(job.TenantId, job.ConnectionProfileId, cancellationToken);
            var watermark = job.SyncMode == DbSyncMode.Delta
                ? await GetWatermarkAsync(job.TenantId, job.ConnectionProfileId, cancellationToken)
                : null;

            job.WatermarkBeforeUtc = watermark;
            await _db.SaveChangesAsync(cancellationToken);

            var input = new DbSyncExecutionInput
            {
                TenantId = job.TenantId,
                ConnectionProfileId = job.ConnectionProfileId,
                JobId = job.Id,
                SyncMode = job.SyncMode,
                ConflictPolicy = job.ConflictPolicy,
                WatermarkUtc = watermark,
                Mappings = mappings
            };

            var progress = new Progress<DbSyncProgress>(p =>
            {
                job.Stage = p.Stage;
                job.ProgressPercent = p.ProgressPercent;
                _ = _notifier.NotifySyncProgressAsync(job.TenantId, job.Id, p, cancellationToken);
            });

            await _pipeline.ExecuteAsync(input, progress, cancellationToken);

            job.Status = job.ErrorRows > 0 ? DbSyncJobStatus.CompletedWithWarnings : DbSyncJobStatus.Completed;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.DurationMs = (int)(job.CompletedAtUtc.Value - started).TotalMilliseconds;
            await _db.SaveChangesAsync(cancellationToken);

            await _notifier.NotifySyncCompletedAsync(
                job.TenantId, job.Id, job.ImportedRows + job.UpdatedRows, job.ErrorRows, cancellationToken);
        }
        catch (Exception ex)
        {
            job.Status = DbSyncJobStatus.Failed;
            job.ErrorMessage = ex.Message.Length > 512 ? ex.Message[..512] : ex.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.DurationMs = (int)((job.CompletedAtUtc ?? DateTime.UtcNow) - started).TotalMilliseconds;
            await _db.SaveChangesAsync(cancellationToken);
            await _notifier.NotifySyncFailedAsync(job.TenantId, job.Id, job.ErrorMessage, cancellationToken);
            throw;
        }
    }

    private async Task<DbSyncJobDto> StartSyncAsync(
        Guid tenantId, Guid userId, Guid connectionId, string syncMode, string conflictPolicy,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        ScopeToTenant(tenantId);
        var connection = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Connection not found.");

        var mappings = await _businessDiscovery.ListMappingsAsync(tenantId, connectionId, cancellationToken);
        if (mappings.Count == 0)
            throw new InvalidOperationException("Run business discovery before sync.");

        var job = new DbSyncJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            CreatedByUserId = userId,
            SyncMode = syncMode,
            ConflictPolicy = string.IsNullOrWhiteSpace(conflictPolicy) ? DbSyncConflictPolicy.SourceWins : conflictPolicy,
            Status = DbSyncJobStatus.Pending,
            Stage = DbSyncStages.ReadingSource
        };
        _db.DbSyncJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        var action = syncMode == DbSyncMode.Delta
            ? DbIntelligenceForensicActions.SyncDeltaStarted
            : DbIntelligenceForensicActions.SyncFullStarted;

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, action, userId, connectionId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        await _dispatcher.EnqueueSyncJobAsync(tenantId, job.Id, cancellationToken);
        return MapJob(job);
    }

    private async Task<List<DbSyncMappingContext>> BuildMappingsAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        var mappings = await _businessDiscovery.ListMappingsAsync(tenantId, connectionId, cancellationToken);
        return mappings
            .Where(m => m.Status != DbBusinessMappingStatus.Ignored)
            .Select(m => new DbSyncMappingContext(m.Id, m.SchemaName, m.TableName, m.EffectiveEntityType, m.Status))
            .ToList();
    }

    private async Task<DateTime?> GetWatermarkAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        var watermarks = await _db.DbSyncWatermarks.AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.ConnectionProfileId == connectionId)
            .Select(w => w.LastSyncedAtUtc)
            .ToListAsync(cancellationToken);
        return watermarks.Count == 0 ? null : watermarks.Min();
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    internal static DbSyncJobDto MapJob(DbSyncJob job) => new(
        job.Id, job.TenantId, job.ConnectionProfileId, job.SyncMode, job.Status, job.Stage,
        job.ProgressPercent, job.ConflictPolicy, job.TotalRows, job.ImportedRows, job.UpdatedRows,
        job.SkippedRows, job.ErrorRows, job.DurationMs, job.ErrorMessage, job.CreatedByUserId,
        job.CreatedAtUtc, job.StartedAtUtc, job.CompletedAtUtc);
}
