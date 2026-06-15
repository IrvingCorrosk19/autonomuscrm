using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncPipeline : IDbSyncPipeline
{
    private readonly ApplicationDbContext _db;
    private readonly IDbSyncExtractService _extract;
    private readonly IDbSyncStagingService _staging;
    private readonly IDbSyncLoadService _load;

    public DbSyncPipeline(
        ApplicationDbContext db,
        IDbSyncExtractService extract,
        IDbSyncStagingService staging,
        IDbSyncLoadService load)
    {
        _db = db;
        _extract = extract;
        _staging = staging;
        _load = load;
    }

    public async Task ExecuteAsync(
        DbSyncExecutionInput input, IProgress<DbSyncProgress>? progress, CancellationToken cancellationToken = default)
    {
        var job = await _db.DbSyncJobs.FirstAsync(j => j.Id == input.JobId, cancellationToken);

        progress?.Report(new DbSyncProgress(DbSyncStages.ReadingSource, 10));
        var rows = input.ExtractedRows.Count > 0
            ? input.ExtractedRows
            : await _extract.ExtractAsync(
                input.TenantId, input.ConnectionProfileId, input.Mappings,
                input.SyncMode, input.WatermarkUtc, cancellationToken);

        progress?.Report(new DbSyncProgress(DbSyncStages.BuildingStaging, 30, $"{rows.Count} rows"));
        await ClearStagingAsync(input.TenantId, input.JobId, cancellationToken);
        if (rows.Count > 0)
            await _staging.StageRowsAsync(input.TenantId, input.JobId, rows, cancellationToken);

        job.TotalRows = rows.Count;
        job.Stage = DbSyncStages.Validating;
        job.ProgressPercent = 45;
        await _db.SaveChangesAsync(cancellationToken);

        progress?.Report(new DbSyncProgress(DbSyncStages.Validating, 45));
        var staged = await _staging.GetPendingRowsAsync(input.TenantId, input.JobId, cancellationToken);
        foreach (var row in staged)
        {
            row.Status = ValidateRow(row) ? DbSyncStagingStatus.Valid : DbSyncStagingStatus.Invalid;
        }
        await _db.SaveChangesAsync(cancellationToken);

        progress?.Report(new DbSyncProgress(DbSyncStages.Importing, 60));
        job.Stage = DbSyncStages.Importing;
        var validRows = staged.Where(r => r.Status == DbSyncStagingStatus.Valid).ToList();
        var imported = 0;
        var updated = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var row in validRows)
        {
            var result = await _load.LoadRowAsync(input.TenantId, input.JobId, row, input.ConflictPolicy, cancellationToken);
            imported += result.Created;
            updated += result.Updated;
            skipped += result.Skipped;
            errors += result.Errors;

            if (result.Snapshot != null)
                _db.DbSyncRollbackSnapshots.Add(result.Snapshot);

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                row.Status = DbSyncStagingStatus.Invalid;
                row.ValidationError = result.Error;
            }
            else if (result.Skipped > 0)
                row.Status = DbSyncStagingStatus.Skipped;
            else
            {
                row.Status = DbSyncStagingStatus.Imported;
                row.CreatedEntityId = result.EntityId;
            }
        }

        job.ImportedRows = imported;
        job.UpdatedRows = updated;
        job.SkippedRows = skipped;
        job.ErrorRows = errors + staged.Count(r => r.Status == DbSyncStagingStatus.Invalid);
        job.Stage = DbSyncStages.Completed;
        job.ProgressPercent = 100;
        job.Status = job.ErrorRows > 0 ? DbSyncJobStatus.CompletedWithWarnings : DbSyncJobStatus.Completed;
        job.CompletedAtUtc = DateTime.UtcNow;
        job.WatermarkAfterUtc = DateTime.UtcNow;

        await UpdateWatermarksAsync(input, cancellationToken);
        DetachNonSyncEntities(job, staged);
        await _db.SaveChangesAsync(cancellationToken);

        progress?.Report(new DbSyncProgress(DbSyncStages.Completed, 100,
            $"Imported {imported}, updated {updated}, skipped {skipped}"));
    }

    private void DetachNonSyncEntities(DbSyncJob job, IReadOnlyList<DbSyncStagingRow> stagedRows)
    {
        var syncIds = new HashSet<object>(stagedRows) { job };
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is DbSyncRollbackSnapshot or DbSyncWatermark)
                continue;
            if (entry.Entity is DbSyncJob or DbSyncStagingRow && syncIds.Contains(entry.Entity))
                continue;
            entry.State = EntityState.Detached;
        }
    }

    private static bool ValidateRow(DbSyncStagingRow row) =>
        row.EntityType is BusinessEntityType.Customer or BusinessEntityType.Company
            or BusinessEntityType.Contact or BusinessEntityType.Sale or BusinessEntityType.Activity;

    private async Task ClearStagingAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        var existing = await _db.DbSyncStagingRows
            .Where(r => r.TenantId == tenantId && r.JobId == jobId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.DbSyncStagingRows.RemoveRange(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpdateWatermarksAsync(DbSyncExecutionInput input, CancellationToken cancellationToken)
    {
        foreach (var entity in input.Mappings.Select(m => m.EntityType).Distinct())
        {
            var wm = await _db.DbSyncWatermarks.FirstOrDefaultAsync(
                w => w.TenantId == input.TenantId &&
                     w.ConnectionProfileId == input.ConnectionProfileId &&
                     w.EntityType == entity, cancellationToken);

            if (wm == null)
            {
                _db.DbSyncWatermarks.Add(new DbSyncWatermark
                {
                    Id = Guid.NewGuid(),
                    TenantId = input.TenantId,
                    ConnectionProfileId = input.ConnectionProfileId,
                    EntityType = entity,
                    LastSyncedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                wm.LastSyncedAtUtc = DateTime.UtcNow;
                wm.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
