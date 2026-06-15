using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DataHub.Migration;

public sealed class MigrationSyncCompleter : IMigrationSyncCompleter
{
    private readonly IDataHubRepository _repo;
    private readonly ITenantIntegrationRepository _integrations;
    private readonly IDataHubDuplicateEngine _duplicates;
    private readonly ILogger<MigrationSyncCompleter> _logger;

    public MigrationSyncCompleter(
        IDataHubRepository repo,
        ITenantIntegrationRepository integrations,
        IDataHubDuplicateEngine duplicates,
        ILogger<MigrationSyncCompleter> logger)
    {
        _repo = repo;
        _integrations = integrations;
        _duplicates = duplicates;
        _logger = logger;
    }

    public async Task TryCompleteMigrationSyncAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _repo.GetJobAsync(tenantId, jobId, cancellationToken);
        if (job == null) return;
        if (job.Status != DataHubJobStatus.Completed.ToString()) return;
        if (!job.Metadata.TryGetValue("migrationSource", out var srcObj) || srcObj == null) return;
        if (!job.Metadata.TryGetValue("migrationEntity", out var entObj) || entObj == null) return;
        if (job.Metadata.TryGetValue("migrationSyncCompleted", out var done) && done is true) return;

        var errors = await _repo.GetErrorsAsync(tenantId, jobId, 0, 1000, cancellationToken);
        var dupes = await _duplicates.ScanJobAsync(tenantId, jobId, cancellationToken);
        var quality = MigrationQualityGate.Evaluate(errors, dupes);
        if (!quality.Passed)
        {
            job.Metadata = new Dictionary<string, object>(job.Metadata)
            {
                ["migrationSyncBlocked"] = true,
                ["migrationSyncBlockReason"] = string.Join("; ", quality.Issues)
            };
            await _repo.UpdateJobAsync(job, cancellationToken);
            _logger.LogWarning(
                "Migration sync blocked for job {JobId}: {Issues}",
                jobId, string.Join("; ", quality.Issues));
            return;
        }

        var source = srcObj.ToString()!;
        var entity = entObj.ToString()!;
        var conn = await _integrations.GetAsync(tenantId, source, cancellationToken);
        if (conn == null) return;

        var mode = job.Metadata.GetValueOrDefault("migrationMode")?.ToString() ?? "Full";
        var rowCount = job.Metadata.GetValueOrDefault("migrationRowCount")?.ToString() ?? job.TotalRows.ToString();
        conn.MarkSync($"Migration {mode} {entity}: imported {rowCount} rows");
        await _integrations.UpsertAsync(conn, cancellationToken);

        var completedMetadata = new Dictionary<string, object>(job.Metadata)
        {
            ["migrationSyncCompleted"] = true
        };
        completedMetadata.Remove("migrationSyncBlocked");
        completedMetadata.Remove("migrationSyncBlockReason");
        job.Metadata = completedMetadata;
        await _repo.UpdateJobAsync(job, cancellationToken);
        _logger.LogInformation("Migration sync timestamp updated for {Source}/{Entity} job {JobId}", source, entity, jobId);
    }
}
