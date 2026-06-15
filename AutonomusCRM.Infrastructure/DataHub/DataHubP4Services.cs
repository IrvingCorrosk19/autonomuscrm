using System.Diagnostics;
using AutonomusCRM.Application.DataHub;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubScheduledImportService : IDataHubScheduledImportService
{
    private static readonly HashSet<string> ValidSources = new(DataHubMigrationCatalog.SupportedSources, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ValidFrequencies =
        Enum.GetNames<DataHubScheduleFrequency>().ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IDataHubRepository _repo;
    private readonly IDataHubMigrationService _migration;
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IDataHubForensicAuditService _forensic;
    private readonly ILogger<DataHubScheduledImportService> _logger;

    public DataHubScheduledImportService(
        IDataHubRepository repo,
        IDataHubMigrationService migration,
        IDataHubOrchestrator orchestrator,
        IDataHubForensicAuditService forensic,
        ILogger<DataHubScheduledImportService> logger)
    {
        _repo = repo;
        _migration = migration;
        _orchestrator = orchestrator;
        _forensic = forensic;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DataHubScheduledImportDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var items = await _repo.GetScheduledImportsAsync(tenantId, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<DataHubScheduledImportDto> CreateAsync(
        Guid tenantId, Guid userId, DataHubScheduledImportCreateDto dto, CancellationToken cancellationToken = default)
    {
        ValidateSchedule(dto.Source, dto.Frequency, dto.ImportMode);
        var now = DateTime.UtcNow;
        var entity = new DataHubScheduledImport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedByUserId = userId,
            Name = dto.Name.Trim(),
            Source = dto.Source,
            SourceEntity = dto.SourceEntity,
            Frequency = dto.Frequency,
            ImportMode = dto.ImportMode,
            LoadMode = dto.LoadMode,
            RunOnceAt = dto.RunOnceAt,
            NextRunAt = ComputeInitialNextRun(dto.Frequency, dto.RunOnceAt, now),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repo.SaveScheduledImportAsync(entity, cancellationToken);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, "ScheduledImportCreated", userId, null, null, null, null, null, null,
            new Dictionary<string, object>
            {
                ["scheduleId"] = entity.Id,
                ["source"] = entity.Source,
                ["frequency"] = entity.Frequency
            }), cancellationToken);
        return ToDto(entity);
    }

    public async Task<DataHubScheduledImportDto?> UpdateAsync(
        Guid tenantId, Guid scheduleId, DataHubScheduledImportUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetScheduledImportAsync(tenantId, scheduleId, cancellationToken);
        if (entity == null) return null;

        if (dto.Name != null) entity.Name = dto.Name.Trim();
        if (dto.IsEnabled.HasValue) entity.IsEnabled = dto.IsEnabled.Value;
        if (dto.Frequency != null)
        {
            if (!ValidFrequencies.Contains(dto.Frequency)) throw new ArgumentException("Invalid frequency");
            entity.Frequency = dto.Frequency;
        }
        if (dto.ImportMode != null) entity.ImportMode = dto.ImportMode;
        if (dto.LoadMode != null) entity.LoadMode = dto.LoadMode;
        if (dto.RunOnceAt.HasValue) entity.RunOnceAt = dto.RunOnceAt;
        entity.NextRunAt = ComputeInitialNextRun(entity.Frequency, entity.RunOnceAt, DateTime.UtcNow);
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateScheduledImportAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        await _repo.DeleteScheduledImportAsync(tenantId, scheduleId, cancellationToken);
    }

    public async Task<IReadOnlyList<DataHubScheduledImportRunDto>> ListRunsAsync(
        Guid tenantId, Guid scheduleId, int take = 20, CancellationToken cancellationToken = default)
    {
        var runs = await _repo.GetScheduledImportRunsAsync(tenantId, scheduleId, take, cancellationToken);
        return runs.Select(r => new DataHubScheduledImportRunDto(
            r.Id, r.ScheduleId, r.JobId, r.Status, r.StartedAt, r.CompletedAt, r.DurationMs, r.ErrorSummary)).ToList();
    }

    public Task ExecuteNowAsync(Guid tenantId, Guid userId, Guid scheduleId, CancellationToken cancellationToken = default)
        => ExecuteScheduleByIdAsync(tenantId, userId, scheduleId, cancellationToken);

    public async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _repo.RecoverExpiredScheduledImportLeasesAsync(now, cancellationToken);
        var due = await _repo.GetDueScheduledImportsAsync(now, cancellationToken);
        foreach (var schedule in due)
        {
            try
            {
                await ExecuteScheduleAsync(schedule, schedule.CreatedByUserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled import {ScheduleId} failed", schedule.Id);
            }
        }
    }

    private async Task ExecuteScheduleByIdAsync(Guid tenantId, Guid userId, Guid scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await _repo.GetScheduledImportAsync(tenantId, scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Schedule not found");
        await ExecuteScheduleAsync(schedule, userId, cancellationToken);
    }

    private async Task ExecuteScheduleAsync(DataHubScheduledImport schedule, Guid userId, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid();
        var leaseUntil = DateTime.UtcNow.AddMinutes(35);
        var claimed = await _repo.TryClaimScheduledImportAsync(schedule.Id, runId, leaseUntil, cancellationToken);
        if (claimed == null) return;
        schedule = claimed;

        var run = new DataHubScheduledImportRun
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            TenantId = schedule.TenantId,
            ExecutedByUserId = userId,
            Status = "Running",
            StartedAt = DateTime.UtcNow
        };
        await _repo.AddScheduledImportRunAsync(run, cancellationToken);
        var sw = Stopwatch.StartNew();

        try
        {
            var mode = Enum.TryParse<DataHubMigrationImportMode>(schedule.ImportMode, true, out var m)
                ? m : DataHubMigrationImportMode.Full;

            var migration = await _migration.StartMigrationAsync(new DataHubMigrationRequestDto(
                schedule.TenantId, userId, schedule.Source, schedule.SourceEntity, mode, schedule.LoadMode), cancellationToken);

            if (migration.JobId == Guid.Empty && migration.Status == "NoChanges")
            {
                run.Status = "Completed";
                run.Details = new Dictionary<string, object> { ["message"] = "No delta changes" };
                return;
            }

            run.JobId = migration.JobId;

            await _orchestrator.AutoMapAsync(schedule.TenantId, migration.JobId, cancellationToken);
            var validation = await _orchestrator.ValidateAsync(schedule.TenantId, migration.JobId, cancellationToken);
            if (!validation.ReadyToImport)
                throw new InvalidOperationException($"Validation failed: {validation.InvalidRows} invalid rows");

            await _orchestrator.StartImportAsync(schedule.TenantId, migration.JobId, cancellationToken);
            await WaitForJobAsync(schedule.TenantId, migration.JobId, cancellationToken);

            var quality = await _migration.ValidateMigrationQualityAsync(schedule.TenantId, migration.JobId, cancellationToken);
            if (!quality.Passed)
                throw new InvalidOperationException($"Quality check failed: {string.Join("; ", quality.Issues)}");

            await _migration.TryCompleteMigrationSyncAsync(schedule.TenantId, migration.JobId, cancellationToken);
            run.Status = "Completed";
            run.Details = new Dictionary<string, object>
            {
                ["duplicateGroups"] = quality.DuplicateGroups,
                ["errorCount"] = quality.ErrorCount,
                ["qualityPassed"] = quality.Passed,
                ["issues"] = quality.Issues
            };
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.ErrorSummary = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            _logger.LogWarning(ex, "Scheduled import run failed for {ScheduleId}", schedule.Id);
        }
        finally
        {
            sw.Stop();
            run.DurationMs = (int)sw.ElapsedMilliseconds;
            run.CompletedAt = DateTime.UtcNow;
            await _repo.UpdateScheduledImportRunAsync(run, cancellationToken);

            await _repo.ReleaseScheduledImportLeaseAsync(schedule.Id, runId, cancellationToken);

            var fresh = await _repo.GetScheduledImportAsync(schedule.TenantId, schedule.Id, cancellationToken);
            if (fresh != null)
            {
                fresh.LastRunAt = DateTime.UtcNow;
                fresh.NextRunAt = ComputeNextRun(fresh);
                if (string.Equals(fresh.Frequency, nameof(DataHubScheduleFrequency.Once), StringComparison.OrdinalIgnoreCase))
                    fresh.IsEnabled = false;
                fresh.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateScheduledImportAsync(fresh, cancellationToken);
            }

            await _forensic.RecordAsync(new DataHubForensicAuditEntry(
                schedule.TenantId, "ScheduledImportRun", userId, run.JobId, null, null, null, null, null,
                new Dictionary<string, object>
                {
                    ["scheduleId"] = schedule.Id,
                    ["runId"] = run.Id,
                    ["status"] = run.Status,
                    ["durationMs"] = run.DurationMs,
                    ["error"] = run.ErrorSummary ?? ""
                }), cancellationToken);
        }
    }

    private async Task WaitForJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMinutes(30);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var job = await _repo.GetJobAsync(tenantId, jobId, cancellationToken);
            if (job == null) throw new InvalidOperationException("Job not found during scheduled import");
            if (job.Status == nameof(DataHubJobStatus.Completed))
                return;
            if (job.Status is nameof(DataHubJobStatus.CompletedWithErrors) or nameof(DataHubJobStatus.Failed) or nameof(DataHubJobStatus.Cancelled) or nameof(DataHubJobStatus.ValidationFailed))
                throw new InvalidOperationException($"Import job ended with status {job.Status}");
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
        throw new TimeoutException("Scheduled import timed out waiting for job completion");
    }

    private static void ValidateSchedule(string source, string frequency, string importMode)
    {
        if (!ValidSources.Contains(source)) throw new ArgumentException("Unsupported source");
        if (!ValidFrequencies.Contains(frequency)) throw new ArgumentException("Invalid frequency");
        if (!Enum.TryParse<DataHubMigrationImportMode>(importMode, true, out _))
            throw new ArgumentException("Invalid import mode");
    }

    private static DateTime? ComputeInitialNextRun(string frequency, DateTime? runOnceAt, DateTime now)
    {
        if (string.Equals(frequency, nameof(DataHubScheduleFrequency.Once), StringComparison.OrdinalIgnoreCase))
            return runOnceAt ?? now;
        if (string.Equals(frequency, nameof(DataHubScheduleFrequency.Daily), StringComparison.OrdinalIgnoreCase))
            return now.AddDays(1);
        if (string.Equals(frequency, nameof(DataHubScheduleFrequency.Weekly), StringComparison.OrdinalIgnoreCase))
            return now.AddDays(7);
        if (string.Equals(frequency, nameof(DataHubScheduleFrequency.Monthly), StringComparison.OrdinalIgnoreCase))
            return now.AddMonths(1);
        return now.AddDays(1);
    }

    private static DateTime? ComputeNextRun(DataHubScheduledImport schedule)
    {
        if (!schedule.IsEnabled) return null;
        var baseTime = schedule.LastRunAt ?? DateTime.UtcNow;
        return schedule.Frequency switch
        {
            nameof(DataHubScheduleFrequency.Once) => null,
            nameof(DataHubScheduleFrequency.Daily) => baseTime.AddDays(1),
            nameof(DataHubScheduleFrequency.Weekly) => baseTime.AddDays(7),
            nameof(DataHubScheduleFrequency.Monthly) => baseTime.AddMonths(1),
            _ => baseTime.AddDays(1)
        };
    }

    private static DataHubScheduledImportDto ToDto(DataHubScheduledImport s) => new(
        s.Id, s.Name, s.Source, s.SourceEntity, s.Frequency, s.ImportMode, s.LoadMode,
        s.IsEnabled, s.NextRunAt, s.LastRunAt, s.CreatedAt);
}

public sealed class DataHubTemplateVersionService : IDataHubTemplateVersionService
{
    private readonly IDataHubRepository _repo;
    private readonly IDataHubForensicAuditService _forensic;

    public DataHubTemplateVersionService(IDataHubRepository repo, IDataHubForensicAuditService forensic)
    {
        _repo = repo;
        _forensic = forensic;
    }

    public async Task<IReadOnlyList<DataHubTemplateVersionDto>> ListVersionsAsync(
        Guid tenantId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var versions = await _repo.GetTemplateVersionsAsync(tenantId, templateId, cancellationToken);
        return versions.Select(v => new DataHubTemplateVersionDto(
            v.Id, v.TemplateId, v.VersionNumber, v.IsActive, v.Mappings.Count,
            v.ChangeSummary, v.CreatedByUserId, v.CreatedAt)).ToList();
    }

    public async Task<DataHubTemplateVersionDto> CreateVersionAsync(
        Guid tenantId, Guid userId, Guid templateId, string? changeSummary, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateOrThrow(tenantId, templateId, cancellationToken);
        var next = await _repo.IncrementTemplateLatestVersionAsync(templateId, cancellationToken);
        template.LatestVersion = next;
        template.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveTemplateAsync(template, cancellationToken);
        var version = new DataHubTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            TenantId = tenantId,
            VersionNumber = next,
            IsActive = false,
            Mappings = CloneMappings(template.Mappings),
            CreatedByUserId = userId,
            ChangeSummary = changeSummary ?? "Manual version snapshot",
            CreatedAt = DateTime.UtcNow
        };
        await _repo.AddTemplateVersionAsync(version, cancellationToken);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, "TemplateVersionCreated", userId, null, null, null, null, null, null,
            new Dictionary<string, object> { ["templateId"] = templateId, ["version"] = next }), cancellationToken);
        return ToDto(version);
    }

    public async Task<DataHubTemplateVersionCompareDto> CompareVersionsAsync(
        Guid tenantId, Guid templateId, int versionA, int versionB, CancellationToken cancellationToken = default)
    {
        var versions = await _repo.GetTemplateVersionsAsync(tenantId, templateId, cancellationToken);
        var a = versions.FirstOrDefault(v => v.VersionNumber == versionA)
            ?? throw new InvalidOperationException($"Version {versionA} not found");
        var b = versions.FirstOrDefault(v => v.VersionNumber == versionB)
            ?? throw new InvalidOperationException($"Version {versionB} not found");

        var mapA = ToMappingDictionary(a.Mappings);
        var mapB = ToMappingDictionary(b.Mappings);

        var added = mapB.Keys.Except(mapA.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(k => $"{k} → {mapB[k]}").ToList();
        var removed = mapA.Keys.Except(mapB.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(k => $"{k} → {mapA[k]}").ToList();
        var changed = mapA.Keys.Intersect(mapB.Keys, StringComparer.OrdinalIgnoreCase)
            .Where(k => !string.Equals(mapA[k], mapB[k], StringComparison.OrdinalIgnoreCase))
            .Select(k => $"{k}: {mapA[k]} → {mapB[k]}").ToList();

        return new DataHubTemplateVersionCompareDto(versionA, versionB, added, removed, changed);
    }

    public async Task<DataHubTemplateVersionDto> RestoreVersionAsync(
        Guid tenantId, Guid userId, Guid templateId, int versionNumber, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateOrThrow(tenantId, templateId, cancellationToken);
        var source = await GetVersionOrThrow(tenantId, templateId, versionNumber, cancellationToken);
        await _repo.DeactivateTemplateVersionsAsync(tenantId, templateId, cancellationToken);
        template.Mappings = CloneMappings(source.Mappings);
        var next = template.LatestVersion + 1;
        template.ActiveVersion = next;
        template.LatestVersion = next;
        template.UpdatedAt = DateTime.UtcNow;
        var version = new DataHubTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            TenantId = tenantId,
            VersionNumber = next,
            IsActive = true,
            Mappings = CloneMappings(source.Mappings),
            CreatedByUserId = userId,
            ChangeSummary = $"Restored from v{versionNumber}",
            CreatedAt = DateTime.UtcNow
        };
        await _repo.SaveTemplateAsync(template, cancellationToken);
        await _repo.AddTemplateVersionAsync(version, cancellationToken);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, "TemplateVersionRestored", userId, null, null, null, null, null, null,
            new Dictionary<string, object> { ["templateId"] = templateId, ["fromVersion"] = versionNumber, ["newVersion"] = next }),
            cancellationToken);
        return ToDto(version);
    }

    public async Task<DataHubTemplateVersionDto> ActivateVersionAsync(
        Guid tenantId, Guid userId, Guid templateId, int versionNumber, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateOrThrow(tenantId, templateId, cancellationToken);
        var source = await GetVersionOrThrow(tenantId, templateId, versionNumber, cancellationToken);
        await _repo.DeactivateTemplateVersionsAsync(tenantId, templateId, cancellationToken);
        source.IsActive = true;
        template.Mappings = CloneMappings(source.Mappings);
        template.ActiveVersion = versionNumber;
        template.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveTemplateAsync(template, cancellationToken);
        await _repo.UpdateTemplateVersionAsync(source, cancellationToken);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, "TemplateVersionActivated", userId, null, null, null, null, null, null,
            new Dictionary<string, object> { ["templateId"] = templateId, ["version"] = versionNumber }), cancellationToken);
        return ToDto(source);
    }

    public async Task EnsureInitialVersionAsync(
        Guid tenantId, Guid userId, DataHubImportTemplate template, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetTemplateVersionsAsync(tenantId, template.Id, cancellationToken);
        if (existing.Count > 0) return;

        template.ActiveVersion = 1;
        template.LatestVersion = 1;
        template.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveTemplateAsync(template, cancellationToken);
        await _repo.AddTemplateVersionAsync(new DataHubTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            TenantId = tenantId,
            VersionNumber = 1,
            IsActive = true,
            Mappings = CloneMappings(template.Mappings),
            CreatedByUserId = userId,
            ChangeSummary = "Initial version",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task<DataHubImportTemplate> GetTemplateOrThrow(Guid tenantId, Guid templateId, CancellationToken cancellationToken)
    {
        var templates = await _repo.GetTemplatesAsync(tenantId, cancellationToken);
        return templates.FirstOrDefault(t => t.Id == templateId)
            ?? throw new InvalidOperationException("Template not found");
    }

    private async Task<DataHubTemplateVersion> GetVersionOrThrow(
        Guid tenantId, Guid templateId, int versionNumber, CancellationToken cancellationToken)
    {
        var versions = await _repo.GetTemplateVersionsAsync(tenantId, templateId, cancellationToken);
        return versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
            ?? throw new InvalidOperationException($"Version {versionNumber} not found");
    }

    private static List<DataHubTemplateMapping> CloneMappings(IEnumerable<DataHubTemplateMapping> mappings)
        => mappings.Select(m => new DataHubTemplateMapping
        {
            SourceColumn = m.SourceColumn,
            TargetField = m.TargetField,
            IsRequired = m.IsRequired,
            DefaultValue = m.DefaultValue,
            TransformRule = m.TransformRule
        }).ToList();

    private static Dictionary<string, string> ToMappingDictionary(IEnumerable<DataHubTemplateMapping> mappings)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in mappings)
            dict[m.SourceColumn] = m.TargetField;
        return dict;
    }

    private static DataHubTemplateVersionDto ToDto(DataHubTemplateVersion v) => new(
        v.Id, v.TemplateId, v.VersionNumber, v.IsActive, v.Mappings.Count,
        v.ChangeSummary, v.CreatedByUserId, v.CreatedAt);
}
