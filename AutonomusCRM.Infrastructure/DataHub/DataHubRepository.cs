using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubRepository : IDataHubRepository
{
    private readonly ApplicationDbContext _db;

    public DataHubRepository(ApplicationDbContext db) => _db = db;

    public Task<DataHubImportJob?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        => _db.DataHubImportJobs
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);

    public Task<DataHubImportJob?> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _db.DataHubImportJobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

    public Task<IReadOnlyList<DataHubImportJob>> ListJobsAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default)
        => _db.DataHubImportJobs
            .Where(j => j.TenantId == tenantId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubImportJob>)t.Result, cancellationToken);

    public async Task AddJobAsync(DataHubImportJob job, CancellationToken cancellationToken = default)
    {
        _db.DataHubImportJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateJobAsync(DataHubImportJob job, CancellationToken cancellationToken = default)
    {
        job.UpdatedAt = DateTime.UtcNow;
        _db.DataHubImportJobs.Update(job);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRowsAsync(IEnumerable<DataHubImportRow> rows, CancellationToken cancellationToken = default)
    {
        _db.DataHubImportRows.AddRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRowsAsync(IEnumerable<DataHubImportRow> rows, CancellationToken cancellationToken = default)
    {
        _db.DataHubImportRows.UpdateRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddErrorsAsync(IEnumerable<DataHubImportError> errors, CancellationToken cancellationToken = default)
    {
        _db.DataHubImportErrors.AddRange(errors);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddLogAsync(DataHubImportLog log, CancellationToken cancellationToken = default)
    {
        _db.DataHubImportLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMappingsAsync(IEnumerable<DataHubImportMapping> mappings, CancellationToken cancellationToken = default)
    {
        _db.DataHubImportMappings.AddRange(mappings);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceMappingsAsync(Guid tenantId, Guid jobId, IEnumerable<DataHubImportMapping> mappings, CancellationToken cancellationToken = default)
    {
        var existing = await _db.DataHubImportMappings
            .Where(m => m.TenantId == tenantId && m.JobId == jobId)
            .ToListAsync(cancellationToken);
        _db.DataHubImportMappings.RemoveRange(existing);
        _db.DataHubImportMappings.AddRange(mappings);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DataHubImportRow>> GetRowsAsync(Guid tenantId, Guid jobId, int skip, int take, CancellationToken cancellationToken = default)
        => _db.DataHubImportRows
            .Where(r => r.TenantId == tenantId && r.JobId == jobId)
            .OrderBy(r => r.RowNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubImportRow>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubImportError>> GetErrorsAsync(Guid tenantId, Guid jobId, int skip, int take, CancellationToken cancellationToken = default)
        => _db.DataHubImportErrors
            .Where(e => e.TenantId == tenantId && e.JobId == jobId)
            .OrderBy(e => e.RowNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubImportError>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubImportMapping>> GetMappingsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        => _db.DataHubImportMappings
            .Where(m => m.TenantId == tenantId && m.JobId == jobId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubImportMapping>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubImportLog>> GetLogsAsync(Guid tenantId, Guid jobId, int take, CancellationToken cancellationToken = default)
        => _db.DataHubImportLogs
            .Where(l => l.TenantId == tenantId && l.JobId == jobId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubImportLog>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubImportJob>> GetPendingJobsAsync(int take, CancellationToken cancellationToken = default)
        => _db.DataHubImportJobs
            .Where(j => j.Status == DataHubJobStatus.ReadyToImport.ToString()
                     || j.Status == DataHubJobStatus.Importing.ToString()
                     || j.Status == DataHubJobStatus.Validating.ToString()
                     || j.Status == DataHubJobStatus.Parsing.ToString())
            .OrderBy(j => j.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubImportJob>)t.Result, cancellationToken);

    public async Task AddRollbackSnapshotsAsync(IEnumerable<DataHubRollbackSnapshot> snapshots, CancellationToken cancellationToken = default)
    {
        _db.DataHubRollbackSnapshots.AddRange(snapshots);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DataHubRollbackSnapshot>> GetRollbackSnapshotsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        => _db.DataHubRollbackSnapshots
            .Where(s => s.TenantId == tenantId && s.JobId == jobId)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubRollbackSnapshot>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubTransformationRule>> GetTransformationRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default)
        => _db.DataHubTransformationRules
            .Where(r => r.TenantId == tenantId && r.TargetEntity == targetEntity && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubTransformationRule>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubValidationRule>> GetValidationRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default)
        => _db.DataHubValidationRules
            .Where(r => r.TenantId == tenantId && r.TargetEntity == targetEntity && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubValidationRule>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubValidationRule>> GetAllValidationRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default)
        => _db.DataHubValidationRules
            .Where(r => r.TenantId == tenantId && r.TargetEntity == targetEntity)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubValidationRule>)t.Result, cancellationToken);

    public async Task ReplaceValidationRulesAsync(Guid tenantId, string targetEntity, IEnumerable<DataHubValidationRule> rules, CancellationToken cancellationToken = default)
    {
        var existing = await _db.DataHubValidationRules
            .Where(r => r.TenantId == tenantId && r.TargetEntity == targetEntity)
            .ToListAsync(cancellationToken);
        _db.DataHubValidationRules.RemoveRange(existing);
        await _db.DataHubValidationRules.AddRangeAsync(rules, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveTransformationRuleAsync(DataHubTransformationRule rule, CancellationToken cancellationToken = default)
    {
        if (rule.Id == Guid.Empty) rule.Id = Guid.NewGuid();
        _db.DataHubTransformationRules.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveValidationRuleAsync(DataHubValidationRule rule, CancellationToken cancellationToken = default)
    {
        if (rule.Id == Guid.Empty) rule.Id = Guid.NewGuid();
        _db.DataHubValidationRules.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DataHubImportTemplate>> GetTemplatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _db.DataHubImportTemplates
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubImportTemplate>)t.Result, cancellationToken);

    public async Task SaveTemplateAsync(DataHubImportTemplate template, CancellationToken cancellationToken = default)
    {
        if (template.Id == Guid.Empty) template.Id = Guid.NewGuid();
        template.UpdatedAt = DateTime.UtcNow;
        var exists = await _db.DataHubImportTemplates.AnyAsync(t => t.Id == template.Id, cancellationToken);
        if (exists)
            _db.DataHubImportTemplates.Update(template);
        else
            _db.DataHubImportTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> IncrementTemplateLatestVersionAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE "DataHubImportTemplates"
            SET "LatestVersion" = "LatestVersion" + 1, "UpdatedAt" = @updatedAt
            WHERE "Id" = @templateId
            RETURNING "LatestVersion"
            """;
        var updatedAt = command.CreateParameter();
        updatedAt.ParameterName = "updatedAt";
        updatedAt.Value = DateTime.UtcNow;
        command.Parameters.Add(updatedAt);
        var idParam = command.CreateParameter();
        idParam.ParameterName = "templateId";
        idParam.Value = templateId;
        command.Parameters.Add(idParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
            throw new InvalidOperationException("Template not found");

        return Convert.ToInt32(result);
    }

    public Task<int> BulkInsertRowsCopyAsync(IReadOnlyList<DataHubImportRow> rows, CancellationToken cancellationToken = default)
        => DataHubBulkStaging.BulkInsertRowsCopyAsync(_db, rows, cancellationToken);

    public Task<int> CountActiveJobsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _db.DataHubImportJobs
            .CountAsync(j => j.TenantId == tenantId &&
                (j.Status == DataHubJobStatus.Importing.ToString() ||
                 j.Status == DataHubJobStatus.ReadyToImport.ToString() ||
                 j.Status == DataHubJobStatus.Parsing.ToString()), cancellationToken);

    public Task<IReadOnlyList<DataHubScheduledImport>> GetScheduledImportsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _db.DataHubScheduledImports.Where(s => s.TenantId == tenantId).OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<DataHubScheduledImport>)t.Result, cancellationToken);

    public Task<DataHubScheduledImport?> GetScheduledImportAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default)
        => _db.DataHubScheduledImports.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == scheduleId, cancellationToken);

    public Task<IReadOnlyList<DataHubScheduledImport>> GetDueScheduledImportsAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
        => _db.DataHubScheduledImports
            .Where(s => s.IsEnabled && s.NextRunAt != null && s.NextRunAt <= asOfUtc &&
                (!s.IsRunning || (s.RunningLeaseUntil != null && s.RunningLeaseUntil < asOfUtc)))
            .OrderBy(s => s.NextRunAt)
            .Take(20)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubScheduledImport>)t.Result, cancellationToken);

    public async Task<DataHubScheduledImport?> TryClaimScheduledImportAsync(
        Guid scheduleId, Guid runId, DateTime leaseUntil, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "DataHubScheduledImports"
            SET "IsRunning" = TRUE,
                "ActiveRunId" = {runId},
                "RunningLeaseUntil" = {leaseUntil},
                "NextRunAt" = NULL,
                "UpdatedAt" = {now}
            WHERE "Id" = {scheduleId}
              AND "IsEnabled" = TRUE
              AND ("IsRunning" = FALSE OR "RunningLeaseUntil" IS NULL OR "RunningLeaseUntil" < {now})
              AND "NextRunAt" IS NOT NULL
              AND "NextRunAt" <= {now}
            """, cancellationToken);
        if (affected == 0) return null;
        return await _db.DataHubScheduledImports.FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
    }

    public async Task ReleaseScheduledImportLeaseAsync(Guid scheduleId, Guid runId, CancellationToken cancellationToken = default)
    {
        var schedule = await _db.DataHubScheduledImports.FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule == null || schedule.ActiveRunId != runId) return;
        schedule.IsRunning = false;
        schedule.ActiveRunId = null;
        schedule.RunningLeaseUntil = null;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecoverExpiredScheduledImportLeasesAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        var expired = await _db.DataHubScheduledImports
            .Where(s => s.IsRunning && s.RunningLeaseUntil != null && s.RunningLeaseUntil < asOfUtc)
            .ToListAsync(cancellationToken);
        foreach (var s in expired)
        {
            s.IsRunning = false;
            s.ActiveRunId = null;
            s.RunningLeaseUntil = null;
            if (s.IsEnabled && s.NextRunAt == null)
                s.NextRunAt = asOfUtc;
        }
        if (expired.Count > 0) await _db.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    public async Task SaveScheduledImportAsync(DataHubScheduledImport schedule, CancellationToken cancellationToken = default)
    {
        if (schedule.Id == Guid.Empty) schedule.Id = Guid.NewGuid();
        _db.DataHubScheduledImports.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateScheduledImportAsync(DataHubScheduledImport schedule, CancellationToken cancellationToken = default)
    {
        _db.DataHubScheduledImports.Update(schedule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteScheduledImportAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var entity = await GetScheduledImportAsync(tenantId, scheduleId, cancellationToken);
        if (entity == null) return;
        _db.DataHubScheduledImports.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddScheduledImportRunAsync(DataHubScheduledImportRun run, CancellationToken cancellationToken = default)
    {
        if (run.Id == Guid.Empty) run.Id = Guid.NewGuid();
        _db.DataHubScheduledImportRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateScheduledImportRunAsync(DataHubScheduledImportRun run, CancellationToken cancellationToken = default)
    {
        _db.DataHubScheduledImportRuns.Update(run);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DataHubScheduledImportRun>> GetScheduledImportRunsAsync(
        Guid tenantId, Guid scheduleId, int take, CancellationToken cancellationToken = default)
        => _db.DataHubScheduledImportRuns
            .Where(r => r.TenantId == tenantId && r.ScheduleId == scheduleId)
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubScheduledImportRun>)t.Result, cancellationToken);

    public Task<IReadOnlyList<DataHubTemplateVersion>> GetTemplateVersionsAsync(
        Guid tenantId, Guid templateId, CancellationToken cancellationToken = default)
        => _db.DataHubTemplateVersions
            .Where(v => v.TenantId == tenantId && v.TemplateId == templateId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubTemplateVersion>)t.Result, cancellationToken);

    public async Task AddTemplateVersionAsync(DataHubTemplateVersion version, CancellationToken cancellationToken = default)
    {
        if (version.Id == Guid.Empty) version.Id = Guid.NewGuid();
        _db.DataHubTemplateVersions.Add(version);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTemplateVersionAsync(DataHubTemplateVersion version, CancellationToken cancellationToken = default)
    {
        _db.DataHubTemplateVersions.Update(version);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateTemplateVersionsAsync(Guid tenantId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var active = await _db.DataHubTemplateVersions
            .Where(v => v.TenantId == tenantId && v.TemplateId == templateId && v.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var v in active) v.IsActive = false;
        if (active.Count > 0) await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAcquireJobProcessingLockAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var lockKey = JobAdvisoryLockKey(jobId);
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_try_advisory_lock(@p0)";
        var param = cmd.CreateParameter();
        param.ParameterName = "p0";
        param.Value = lockKey;
        cmd.Parameters.Add(param);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result switch
        {
            true => true,
            false => false,
            DBNull => false,
            long l => l != 0,
            int i => i != 0,
            _ => false
        };
    }

    public async Task ReleaseJobProcessingLockAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var lockKey = JobAdvisoryLockKey(jobId);
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_advisory_unlock(@p0)";
        var param = cmd.CreateParameter();
        param.ParameterName = "p0";
        param.Value = lockKey;
        cmd.Parameters.Add(param);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }

    private static long JobAdvisoryLockKey(Guid jobId)
    {
        Span<byte> bytes = stackalloc byte[16];
        jobId.TryWriteBytes(bytes);
        return BitConverter.ToInt64(bytes);
    }

    public async Task DeleteStagingRowsForJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        await _db.DataHubImportRows
            .Where(r => r.TenantId == tenantId && r.JobId == jobId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
