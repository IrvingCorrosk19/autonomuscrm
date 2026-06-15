using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Infrastructure.DataHub.Migration;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubMigrationService : IDataHubMigrationService
{
    private readonly ITenantIntegrationRepository _integrations;
    private readonly MigrationSourceExtractorRegistry _extractors;
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IDataHubRepository _repo;
    private readonly IDataHubDuplicateEngine _duplicates;
    private readonly IDataHubForensicAuditService _forensic;
    private readonly IDataHubRequestContext _requestContext;
    private readonly IIntegrationOAuthService _oauth;
    private readonly IMigrationSyncCompleter _syncCompleter;
    private readonly ILogger<DataHubMigrationService> _logger;

    public DataHubMigrationService(
        ITenantIntegrationRepository integrations,
        MigrationSourceExtractorRegistry extractors,
        IDataHubOrchestrator orchestrator,
        IDataHubRepository repo,
        IDataHubDuplicateEngine duplicates,
        IDataHubForensicAuditService forensic,
        IDataHubRequestContext requestContext,
        IIntegrationOAuthService oauth,
        IMigrationSyncCompleter syncCompleter,
        ILogger<DataHubMigrationService> logger)
    {
        _integrations = integrations;
        _extractors = extractors;
        _orchestrator = orchestrator;
        _repo = repo;
        _duplicates = duplicates;
        _forensic = forensic;
        _requestContext = requestContext;
        _oauth = oauth;
        _syncCompleter = syncCompleter;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DataHubMigrationSourceDto>> ListSourcesAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connections = await _integrations.ListAsync(tenantId, cancellationToken);
        return _extractors.All.Select(ext =>
        {
            var conn = connections.FirstOrDefault(c =>
                string.Equals(c.Provider, ext.Source, StringComparison.OrdinalIgnoreCase));
            var snapshot = conn != null ? MigrationConnectionHelper.ToSnapshot(conn) : null;
            return new DataHubMigrationSourceDto(
                ext.Source,
                ext.Source,
                DataHubMigrationCatalog.GetSourceDescription(ext.Source),
                true,
                ext.IsConfigured(snapshot),
                conn?.LastSyncAt);
        }).ToList();
    }

    public IReadOnlyList<DataHubMigrationEntityDto> ListEntities(string source)
        => _extractors.Get(source).SupportedEntities;

    public async Task<DataHubMigrationConnectionStatusDto> GetConnectionStatusAsync(
        Guid tenantId, string source, CancellationToken cancellationToken = default)
    {
        var ext = _extractors.Get(source);
        var conn = await _integrations.GetAsync(tenantId, source, cancellationToken);
        var snapshot = conn != null ? MigrationConnectionHelper.ToSnapshot(conn) : null;
        return new DataHubMigrationConnectionStatusDto(
            source,
            ext.IsConfigured(snapshot),
            _oauth.IsOAuthConfigured(source),
            conn?.LastSyncAt,
            ext.IsConfigured(snapshot) ? "Connected" : "Connect via Integrations or API settings");
    }

    public async Task<DataHubMigrationStartResultDto> StartMigrationAsync(
        DataHubMigrationRequestDto request, CancellationToken cancellationToken = default)
    {
        var ext = _extractors.Get(request.Source);
        var conn = await _integrations.GetAsync(request.TenantId, request.Source, cancellationToken)
            ?? throw new InvalidOperationException($"Connect {request.Source} before migrating.");

        var snapshot = MigrationConnectionHelper.ToSnapshot(conn);
        if (!ext.IsConfigured(snapshot))
            throw new InvalidOperationException($"{request.Source} is not configured for migration.");

        var since = request.Mode == DataHubMigrationImportMode.Delta ? conn.LastSyncAt : null;
        var extracted = await ext.ExtractAsync(snapshot, request.SourceEntity, request.Mode, since, cancellationToken);
        if (extracted.Rows.Count == 0)
        {
            if (request.Mode == DataHubMigrationImportMode.Delta)
            {
                return new DataHubMigrationStartResultDto(
                    Guid.Empty, "NoChanges", request.Source, request.SourceEntity,
                    DataHubMigrationCatalog.MapTargetEntity(request.Source, request.SourceEntity),
                    0, request.Mode.ToString(), Array.Empty<string>());
            }
            throw new InvalidOperationException($"No records returned for {request.Source}/{request.SourceEntity}.");
        }

        await using var csv = MigrationCsvBuilder.ToCsvStream(extracted.Columns, extracted.Rows);
        var fileName = $"{request.Source}-{request.SourceEntity}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        var targetEntity = DataHubMigrationCatalog.MapTargetEntity(request.Source, request.SourceEntity);

        var upload = await _orchestrator.UploadAsync(
            request.TenantId, request.UserId, csv, fileName, targetEntity,
            request.LoadMode, request.DryRun, cancellationToken);

        var job = await _repo.GetJobAsync(request.TenantId, upload.JobId, cancellationToken);
        if (job != null)
        {
            job.Metadata["migrationSource"] = request.Source;
            job.Metadata["migrationEntity"] = request.SourceEntity;
            job.Metadata["migrationMode"] = request.Mode.ToString();
            job.Metadata["migrationRowCount"] = extracted.Rows.Count;
            await _repo.UpdateJobAsync(job, cancellationToken);
        }

        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            request.TenantId, "MigrationStart", request.UserId, upload.JobId, fileName, csv.Length,
            null, _requestContext.ClientIp, _requestContext.UserAgent,
            new Dictionary<string, object>
            {
                ["source"] = request.Source,
                ["sourceEntity"] = request.SourceEntity,
                ["mode"] = request.Mode.ToString(),
                ["rows"] = extracted.Rows.Count,
                ["targetEntity"] = targetEntity
            }), cancellationToken);

        _logger.LogInformation(
            "Migration started: {Source}/{Entity} → job {JobId}, {Rows} rows, mode {Mode}",
            request.Source, request.SourceEntity, upload.JobId, extracted.Rows.Count, request.Mode);

        return new DataHubMigrationStartResultDto(
            upload.JobId, upload.Status, request.Source, request.SourceEntity,
            targetEntity, extracted.Rows.Count, request.Mode.ToString(), upload.DetectedColumns);
    }

    public async Task<DataHubMigrationQualityReportDto> ValidateMigrationQualityAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _repo.GetJobAsync(tenantId, jobId, cancellationToken)
            ?? throw new DataHubTenantAccessException("Job not found or access denied.");

        var errors = await _repo.GetErrorsAsync(tenantId, jobId, 0, 1000, cancellationToken);
        var dupes = await _duplicates.ScanJobAsync(tenantId, jobId, cancellationToken);
        var issues = new List<string>();

        var evaluation = MigrationQualityGate.Evaluate(errors, dupes);
        issues.AddRange(evaluation.Issues);

        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, "MigrationQualityCheck", job.CreatedByUserId, jobId, job.FileName, job.FileSizeBytes,
            null, _requestContext.ClientIp, _requestContext.UserAgent,
            new Dictionary<string, object> { ["passed"] = evaluation.Passed, ["issues"] = issues }), cancellationToken);

        return new DataHubMigrationQualityReportDto(
            jobId, evaluation.DuplicateGroups, evaluation.ErrorCount, evaluation.BrokenRelations,
            evaluation.MissingOwners, issues, evaluation.Passed);
    }

    public Task TryCompleteMigrationSyncAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        => _syncCompleter.TryCompleteMigrationSyncAsync(tenantId, jobId, cancellationToken);
}
