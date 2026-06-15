using System.Text.Json;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Operations;

public sealed class DbOperationService : IDbOperationService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IBusinessDiscoveryService _businessDiscovery;
    private readonly IDbSyncExtractService _extract;
    private readonly IDbSyncLoadService _load;
    private readonly IDbOperationEngine _engine;
    private readonly DbOperationRollbackService _rollback;
    private readonly IDbIntelligenceAuditService _audit;
    private readonly IDbOperationProgressNotifier _notifier;

    public DbOperationService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IBusinessDiscoveryService businessDiscovery,
        IDbSyncExtractService extract,
        IDbSyncLoadService load,
        IDbOperationEngine engine,
        DbOperationRollbackService rollback,
        IDbIntelligenceAuditService audit,
        IDbOperationProgressNotifier notifier)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _businessDiscovery = businessDiscovery;
        _extract = extract;
        _load = load;
        _engine = engine;
        _rollback = rollback;
        _audit = audit;
        _notifier = notifier;
    }

    public async Task<DbOperationJobDto> StartSessionAsync(
        Guid tenantId, Guid userId, Guid connectionId,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var connection = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Connection not found.");

        var mappings = await _businessDiscovery.ListMappingsAsync(tenantId, connectionId, cancellationToken);
        if (mappings.Count == 0)
            throw new InvalidOperationException("Run business discovery before operating on data.");

        var job = new DbOperationJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            CreatedByUserId = userId,
            Status = DbOperationJobStatus.Running,
            Stage = DbOperationStages.Analyzing,
            ProgressPercent = 10,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.DbOperationJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.OperationStarted, userId, connectionId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        await _notifier.NotifyOperationStartedAsync(tenantId, job.Id, connectionId, cancellationToken);

        try
        {
            var mappingContexts = mappings
                .Where(m => m.Status == DbBusinessMappingStatus.Confirmed)
                .Select(m => new DbSyncMappingContext(m.Id, m.SchemaName, m.TableName, m.InferredEntityType, m.Status))
                .ToList();

            await Report(tenantId, job, DbOperationStages.Preparing, 30, "Reading source data", cancellationToken);

            var extracted = await _extract.ExtractAsync(
                tenantId, connectionId, mappingContexts, DbSyncMode.Full, null, cancellationToken);

            var staging = extracted.Select(r =>
            {
                var payload = JsonSerializer.Serialize(r.Data, JsonOptions);
                return new DbOperationStagingRow
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    TenantId = tenantId,
                    RowNumber = r.RowNumber,
                    EntityType = r.EntityType,
                    SchemaName = r.SchemaName,
                    TableName = r.TableName,
                    PayloadJson = payload,
                    OriginalPayloadJson = payload,
                    Status = DbOperationRowStatus.Active,
                    SourceModifiedAtUtc = r.ModifiedAtUtc
                };
            }).ToList();

            _db.DbOperationStagingRows.AddRange(staging);
            job.TotalRows = staging.Count;
            job.Stage = DbOperationStages.Validating;
            job.ProgressPercent = 50;
            job.Status = DbOperationJobStatus.Pending;
            await _db.SaveChangesAsync(cancellationToken);

            return MapJob(job);
        }
        catch (Exception ex)
        {
            job.Status = DbOperationJobStatus.Failed;
            job.ErrorMessage = Truncate(ex.Message);
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _notifier.NotifyOperationFailedAsync(tenantId, job.Id, job.ErrorMessage, cancellationToken);
            throw;
        }
    }

    public async Task<DbOperationPreviewResultDto> PreviewAsync(
        Guid tenantId, Guid jobId, DbOperationActionPlan plan, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await GetJobEntityAsync(tenantId, jobId, cancellationToken);
        var rows = await LoadRowContextsAsync(tenantId, jobId, cancellationToken);
        job.Status = DbOperationJobStatus.Preview;
        job.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
        await _db.SaveChangesAsync(cancellationToken);
        return _engine.BuildPreview(jobId, plan, rows);
    }

    public async Task<DbOperationResultDto> ExecuteAsync(
        Guid tenantId, Guid userId, Guid jobId, DbOperationActionPlan plan,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await GetJobEntityAsync(tenantId, jobId, cancellationToken);
        var connection = await _db.DbConnectionProfiles.AsNoTracking()
            .FirstAsync(c => c.Id == job.ConnectionProfileId, cancellationToken);

        job.Status = DbOperationJobStatus.Running;
        job.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.OperationExecuteStarted, userId, job.ConnectionProfileId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        try
        {
            var rows = await LoadRowContextsAsync(tenantId, jobId, cancellationToken);
            await Report(tenantId, job, DbOperationStages.Transforming, 60, "Applying operations", cancellationToken);

            var applied = _engine.ApplyPreview(plan, rows);
            await PersistRowChangesAsync(tenantId, jobId, applied.Rows, cancellationToken);

            job.CorrectedRows = applied.Corrected;
            job.MergedRows = applied.Merged;
            job.ExcludedRows = applied.Excluded + applied.Filtered;
            job.TransformedRows = applied.Transformed;

            if (plan.Import || plan.Sync)
            {
                await Report(tenantId, job, DbOperationStages.Importing, 80, "Importing to CRM", cancellationToken);
                job.ImportedRows = await ImportActiveRowsAsync(tenantId, jobId, plan.ConflictPolicy, cancellationToken);
            }

            job.Status = job.ErrorRows > 0 ? DbOperationJobStatus.CompletedWithWarnings : DbOperationJobStatus.Completed;
            job.Stage = DbOperationStages.Completed;
            job.ProgressPercent = 100;
            job.CompletedAtUtc = DateTime.UtcNow;
            DetachNonOperationEntities(job);
            await _db.SaveChangesAsync(cancellationToken);

            var result = MapResult(job);
            await _notifier.NotifyOperationCompletedAsync(tenantId, jobId, result, cancellationToken);
            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.OperationExecuteCompleted, userId, job.ConnectionProfileId,
                connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
                connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            job.Status = DbOperationJobStatus.Failed;
            job.ErrorMessage = Truncate(ex.Message);
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _notifier.NotifyOperationFailedAsync(tenantId, jobId, job.ErrorMessage, cancellationToken);
            throw;
        }
    }

    public async Task<DbOperationResultDto?> GetResultAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbOperationJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);
        return job == null ? null : MapResult(job);
    }

    public async Task<DbOperationJobDto?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbOperationJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);
        return job == null ? null : MapJob(job);
    }

    public async Task<DbOperationRollbackResultDto> RollbackAsync(
        Guid tenantId, Guid userId, Guid jobId,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await GetJobEntityAsync(tenantId, jobId, cancellationToken);
        var connection = await _db.DbConnectionProfiles.AsNoTracking()
            .FirstAsync(c => c.Id == job.ConnectionProfileId, cancellationToken);

        var result = await _rollback.ExecuteRollbackAsync(tenantId, jobId, cancellationToken);
        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.OperationRolledBack, userId, job.ConnectionProfileId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);
        return result;
    }

    internal async Task LoadSyntheticRowsAsync(
        Guid tenantId, Guid jobId, IReadOnlyList<DbOperationRowContext> rows, CancellationToken cancellationToken = default)
    {
        var entities = rows.Select(r =>
        {
            var payload = JsonSerializer.Serialize(r.Data, JsonOptions);
            return new DbOperationStagingRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                TenantId = tenantId,
                RowNumber = r.RowNumber,
                EntityType = r.EntityType,
                SchemaName = r.SchemaName,
                TableName = r.TableName,
                PayloadJson = payload,
                OriginalPayloadJson = payload,
                Status = r.Status,
                ExclusionReason = r.ExclusionReason,
                SourceModifiedAtUtc = r.SourceModifiedAtUtc
            };
        }).ToList();
        _db.DbOperationStagingRows.AddRange(entities);
        var job = await _db.DbOperationJobs.FirstAsync(j => j.Id == jobId, cancellationToken);
        job.TotalRows = entities.Count;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> ImportActiveRowsAsync(
        Guid tenantId, Guid jobId, string conflictPolicy, CancellationToken cancellationToken)
    {
        var rows = await _db.DbOperationStagingRows
            .Where(r => r.TenantId == tenantId && r.JobId == jobId && r.Status == DbOperationRowStatus.Active)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);

        var imported = 0;
        var errors = 0;

        foreach (var row in rows)
        {
            var staging = new DbSyncStagingRow
            {
                Id = row.Id,
                TenantId = tenantId,
                JobId = jobId,
                RowNumber = row.RowNumber,
                EntityType = row.EntityType,
                SchemaName = row.SchemaName,
                TableName = row.TableName,
                PayloadJson = row.PayloadJson,
                Status = DbSyncStagingStatus.Valid
            };

            var result = await _load.LoadRowAsync(tenantId, jobId, staging, conflictPolicy, cancellationToken);
            if (result.Errors > 0 || !string.IsNullOrWhiteSpace(result.Error))
            {
                row.Status = DbOperationRowStatus.Error;
                errors++;
                continue;
            }

            if (result.Snapshot != null)
                _db.DbOperationRollbackSnapshots.Add(MapSnapshot(result.Snapshot, jobId, tenantId));

            row.Status = DbOperationRowStatus.Imported;
            row.CreatedEntityId = result.EntityId;
            imported++;
        }

        var job = await _db.DbOperationJobs.FirstAsync(j => j.Id == jobId, cancellationToken);
        job.ErrorRows = errors;
        DetachNonOperationEntities(job, rows);
        await _db.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private void DetachNonOperationEntities(DbOperationJob job, IReadOnlyList<DbOperationStagingRow>? stagingRows = null)
    {
        var keep = new HashSet<object> { job };
        if (stagingRows != null)
        {
            foreach (var row in stagingRows)
                keep.Add(row);
        }

        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is DbOperationRollbackSnapshot)
                continue;
            if (entry.Entity is DbOperationJob or DbOperationStagingRow && keep.Contains(entry.Entity))
                continue;
            entry.State = EntityState.Detached;
        }
    }

    private static DbOperationRollbackSnapshot MapSnapshot(
        DbSyncRollbackSnapshot snap, Guid jobId, Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId,
        TenantId = tenantId,
        RowNumber = snap.RowNumber,
        EntityType = snap.EntityType,
        EntityId = snap.EntityId,
        Action = snap.Action,
        PreviousStateJson = snap.PreviousStateJson,
        CreatedAtUtc = snap.CreatedAtUtc
    };

    private async Task PersistRowChangesAsync(
        Guid tenantId, Guid jobId, IReadOnlyList<DbOperationRowContext> rows, CancellationToken cancellationToken)
    {
        var existing = await _db.DbOperationStagingRows
            .Where(r => r.TenantId == tenantId && r.JobId == jobId)
            .ToListAsync(cancellationToken);

        foreach (var ctx in rows)
        {
            var row = existing.FirstOrDefault(r => r.RowNumber == ctx.RowNumber);
            if (row == null) continue;
            row.PayloadJson = JsonSerializer.Serialize(ctx.Data, JsonOptions);
            row.Status = ctx.Status;
            row.ExclusionReason = ctx.ExclusionReason;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DbOperationRowContext>> LoadRowContextsAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        var rows = await _db.DbOperationStagingRows.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.JobId == jobId)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DbOperationRowContext
        {
            RowNumber = r.RowNumber,
            EntityType = r.EntityType,
            SchemaName = r.SchemaName,
            TableName = r.TableName,
            Data = JsonSerializer.Deserialize<Dictionary<string, string?>>(r.PayloadJson, JsonOptions) ?? new(),
            Status = r.Status,
            ExclusionReason = r.ExclusionReason,
            SourceModifiedAtUtc = r.SourceModifiedAtUtc
        }).ToList();
    }

    private async Task<DbOperationJob> GetJobEntityAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken) =>
        await _db.DbOperationJobs.FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken)
        ?? throw new KeyNotFoundException("Operation job not found.");

    private async Task Report(Guid tenantId, DbOperationJob job, string stage, int percent, string message, CancellationToken ct)
    {
        job.Stage = stage;
        job.ProgressPercent = percent;
        await _notifier.NotifyOperationProgressAsync(tenantId, job.Id, new DbOperationProgress(stage, percent, message), ct);
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    private static string Truncate(string msg) => msg.Length > 512 ? msg[..512] : msg;

    internal static DbOperationJobDto MapJob(DbOperationJob job) => new(
        job.Id, job.TenantId, job.ConnectionProfileId, job.Status, job.Stage,
        job.ProgressPercent, job.TotalRows, job.PlanJson, job.ErrorMessage, job.CreatedAtUtc, job.CompletedAtUtc);

    internal static DbOperationResultDto MapResult(DbOperationJob job) => new(
        job.Id, job.TenantId, job.ConnectionProfileId, job.Status, job.Stage, job.ProgressPercent,
        job.CorrectedRows, job.MergedRows, job.ExcludedRows, job.TransformedRows,
        job.ImportedRows, job.ErrorRows, job.ErrorMessage, job.CreatedAtUtc, job.CompletedAtUtc);
}
