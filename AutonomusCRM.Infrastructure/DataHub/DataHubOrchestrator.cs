using System.Diagnostics;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubBackgroundProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDataHubJobQueue _queue;
    private readonly DataHubProcessingOptions _options;
    private readonly ILogger<DataHubBackgroundProcessor> _logger;

    public DataHubBackgroundProcessor(
        IServiceScopeFactory scopeFactory,
        IDataHubJobQueue queue,
        Microsoft.Extensions.Options.IOptions<DataHubProcessingOptions> options,
        ILogger<DataHubBackgroundProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.ProcessingMode == DataHubProcessingMode.RabbitMQ)
        {
            _logger.LogInformation("Data Hub in-process processor disabled (ProcessingMode=RabbitMQ)");
            return;
        }

        _logger.LogInformation("Data Hub job processor started (queue + poll)");
        var pollTask = PollPendingJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _queue.DequeueAsync(stoppingToken);
                await ProcessJobSafeAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Data Hub queue processor error"); }
        }

        await pollTask;
    }

    private async Task PollPendingJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
                accessor.BypassTenantFilter = true;
                var repo = scope.ServiceProvider.GetRequiredService<IDataHubRepository>();
                var pending = await repo.GetPendingJobsAsync(5, stoppingToken);
                foreach (var job in pending.Where(j =>
                    j.Status == DataHubJobStatus.ReadyToImport.ToString()))
                {
                    _queue.Enqueue(job.Id);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Data Hub poll error"); }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessJobSafeAsync(Guid jobId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var orchestrator = scope.ServiceProvider.GetRequiredService<IDataHubOrchestrator>();
        await orchestrator.ProcessJobAsync(jobId, stoppingToken);
    }
}

public sealed class DataHubOrchestrator : IDataHubOrchestrator
{
    private readonly IDataHubRepository _repo;
    private readonly IDataHubExtractService _extract;
    private readonly IDataHubTransformService _transform;
    private readonly IDataHubValidateService _validate;
    private readonly IDataHubLoadService _load;
    private readonly IDataHubSecurityService _security;
    private readonly IDataHubFieldCatalog _fields;
    private readonly DataHubFileStore _fileStore;
    private readonly IDataHubIntelligenceService _intelligence;
    private readonly IDataHubAutoFixService _autoFix;
    private readonly IDataHubRulesEngineService _rulesEngine;
    private readonly IDataHubJobQueue _jobQueue;
    private readonly IDataHubRollbackService _rollback;
    private readonly IDataHubDuplicateEngine _duplicates;
    private readonly IDataHubProgressNotifier _progress;
    private readonly IDataHubImportDispatcher _dispatcher;
    private readonly IDataHubSecurityQuotaService _quotas;
    private readonly IDataHubMalwareScanner _malwareScanner;
    private readonly IDataHubForensicAuditService _forensic;
    private readonly IDataHubRequestContext _requestContext;
    private readonly IDataHubTemplateVersionService _templateVersions;
    private readonly IMigrationSyncCompleter _migrationSync;
    private readonly ILogger<DataHubOrchestrator> _logger;

    public DataHubOrchestrator(
        IDataHubRepository repo,
        IDataHubExtractService extract,
        IDataHubTransformService transform,
        IDataHubValidateService validate,
        IDataHubLoadService load,
        IDataHubSecurityService security,
        IDataHubFieldCatalog fields,
        DataHubFileStore fileStore,
        IDataHubIntelligenceService intelligence,
        IDataHubAutoFixService autoFix,
        IDataHubRulesEngineService rulesEngine,
        IDataHubJobQueue jobQueue,
        IDataHubRollbackService rollback,
        IDataHubDuplicateEngine duplicates,
        IDataHubProgressNotifier progress,
        IDataHubImportDispatcher dispatcher,
        IDataHubSecurityQuotaService quotas,
        IDataHubMalwareScanner malwareScanner,
        IDataHubForensicAuditService forensic,
        IDataHubRequestContext requestContext,
        IDataHubTemplateVersionService templateVersions,
        IMigrationSyncCompleter migrationSync,
        ILogger<DataHubOrchestrator> logger)
    {
        _repo = repo;
        _extract = extract;
        _transform = transform;
        _validate = validate;
        _load = load;
        _security = security;
        _fields = fields;
        _fileStore = fileStore;
        _intelligence = intelligence;
        _autoFix = autoFix;
        _rulesEngine = rulesEngine;
        _jobQueue = jobQueue;
        _rollback = rollback;
        _duplicates = duplicates;
        _progress = progress;
        _dispatcher = dispatcher;
        _quotas = quotas;
        _malwareScanner = malwareScanner;
        _forensic = forensic;
        _requestContext = requestContext;
        _templateVersions = templateVersions;
        _migrationSync = migrationSync;
        _logger = logger;
    }

    public async Task<DataHubUploadResultDto> UploadAsync(
        Guid tenantId, Guid userId, Stream fileStream, string fileName, string targetEntity,
        string loadMode, bool dryRun, CancellationToken cancellationToken = default)
    {
        var fileSize = fileStream.CanSeek ? fileStream.Length : 0;
        var security = _security.ValidateUpload(fileName, fileSize > 0 ? fileSize : DataHubConstants.MaxFileBytes, null);
        if (!security.Ok) throw new InvalidOperationException(security.Error);

        await _quotas.EnsureUploadAllowedAsync(tenantId, fileSize > 0 ? fileSize : DataHubConstants.MaxFileBytes, cancellationToken);

        var tempPath = Path.Combine(Path.GetTempPath(), $"datahub-upload-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var tempOut = File.Create(tempPath))
                await fileStream.CopyToAsync(tempOut, cancellationToken);

            fileSize = new FileInfo(tempPath).Length;
            var sizeCheck = _security.ValidateUpload(fileName, fileSize, null);
            if (!sizeCheck.Ok) throw new InvalidOperationException(sizeCheck.Error);

            string fileHash;
            await using (var hashStream = File.OpenRead(tempPath))
                fileHash = DataHubSecurityContext.ComputeSha256(hashStream);

            await using (var scanStream = File.OpenRead(tempPath))
            {
                var scan = await _malwareScanner.ScanAsync(scanStream, fileName, cancellationToken);
                if (!scan.IsClean)
                {
                    await _forensic.RecordAsync(new DataHubForensicAuditEntry(
                        tenantId, DataHubForensicActions.MalwareBlocked, userId, null, fileName, fileSize, fileHash,
                        _requestContext.ClientIp, _requestContext.UserAgent,
                        new Dictionary<string, object> { ["threat"] = scan.ThreatName ?? "unknown", ["scanner"] = scan.Scanner }), cancellationToken);
                    throw new DataHubMalwareDetectedException(scan.ThreatName ?? "unknown");
                }
            }

            var jobId = Guid.NewGuid();
            await using (var saveStream = File.OpenRead(tempPath))
            {
                var storedPath = await _fileStore.SaveAsync(tenantId, jobId, saveStream, fileName, cancellationToken);
                return await CompleteUploadAfterSaveAsync(tenantId, userId, jobId, storedPath, fileName, fileHash, fileSize, targetEntity, loadMode, dryRun, cancellationToken);
            }
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private async Task<DataHubUploadResultDto> CompleteUploadAfterSaveAsync(
        Guid tenantId, Guid userId, Guid jobId, string storedPath, string fileName, string fileHash, long fileSize,
        string targetEntity, string loadMode, bool dryRun, CancellationToken cancellationToken)
    {
        var job = new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            CreatedByUserId = userId,
            FileName = Path.GetFileName(fileName),
            FileFormat = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant(),
            TargetEntity = targetEntity,
            LoadMode = loadMode,
            IsDryRun = dryRun,
            StoredFilePath = storedPath,
            Status = DataHubJobStatus.Parsing.ToString(),
            FileSizeBytes = fileSize
        };

        await _repo.AddJobAsync(job, cancellationToken);
        await LogAsync(tenantId, jobId, "Info", $"File uploaded: {fileName}", cancellationToken);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var useStreaming = ext is ".csv" or ".txt";

        var staging = new UploadStagingResult();

        await using var readStream = _fileStore.OpenRead(storedPath);

        try
        {
        await _repo.ExecuteInTransactionAsync(async () =>
        {
        if (useStreaming)
        {
            staging.Columns = new List<string>();
            staging.SampleForAi = new List<Dictionary<string, string?>>();
            staging.Encoding = "UTF-8";
            staging.Delimiter = null;
            staging.TotalRows = 0;

            await foreach (var chunk in _extract.ExtractInChunksAsync(readStream, fileName, DataHubConstants.CopyBatchSize, cancellationToken))
            {
                if (chunk.IsFirstChunk)
                {
                    staging.Columns = chunk.Columns.ToList();
                    staging.Encoding = chunk.Encoding;
                    staging.Delimiter = chunk.Delimiter;
                }

                var stagingBatch = new List<DataHubImportRow>(chunk.Rows.Count);
                for (var i = 0; i < chunk.Rows.Count; i++)
                {
                    var rowNum = chunk.StartRowNumber + i;
                    var sanitized = chunk.Rows[i].ToDictionary(kv => kv.Key, kv => _security.SanitizeCellValue(kv.Value));
                    stagingBatch.Add(new DataHubImportRow
                    {
                        Id = Guid.NewGuid(),
                        JobId = jobId,
                        TenantId = tenantId,
                        RowNumber = rowNum,
                        RawData = sanitized,
                        Status = DataHubRowStatus.Pending.ToString()
                    });
                    if (staging.SampleForAi.Count < 50)
                        staging.SampleForAi.Add(sanitized);
                }

                if (stagingBatch.Count >= 100)
                    await _repo.BulkInsertRowsCopyAsync(stagingBatch, cancellationToken);
                else
                    await _repo.AddRowsAsync(stagingBatch, cancellationToken);

                staging.TotalRows += chunk.Rows.Count;
            }

            job.Metadata["stagingMode"] = "COPY+Chunks";
        }
        else
        {
            var (cols, rows, enc, delim) = await _extract.ExtractAsync(readStream, fileName, cancellationToken);
            staging.Columns = cols;
            staging.Encoding = enc;
            staging.Delimiter = delim;
            staging.TotalRows = rows.Count;
            staging.SampleForAi = rows.Take(50).ToList();

            var stagingRows = new List<DataHubImportRow>();
            for (var i = 0; i < rows.Count; i++)
            {
                var sanitized = rows[i].ToDictionary(kv => kv.Key, kv => _security.SanitizeCellValue(kv.Value));
                stagingRows.Add(new DataHubImportRow
                {
                    Id = Guid.NewGuid(),
                    JobId = jobId,
                    TenantId = tenantId,
                    RowNumber = i + 1,
                    RawData = sanitized,
                    Status = DataHubRowStatus.Pending.ToString()
                });
            }

            for (var i = 0; i < stagingRows.Count; i += DataHubConstants.CopyBatchSize)
            {
                var batch = stagingRows.Skip(i).Take(DataHubConstants.CopyBatchSize).ToList();
                if (batch.Count >= 100)
                    await _repo.BulkInsertRowsCopyAsync(batch, cancellationToken);
                else
                    await _repo.AddRowsAsync(batch, cancellationToken);
            }

            job.Metadata["stagingMode"] = staging.TotalRows >= 100 ? "COPY" : "EF";
        }
        }, cancellationToken);
        }
        catch
        {
            await _repo.DeleteStagingRowsForJobAsync(tenantId, jobId, cancellationToken);
            job.Status = DataHubJobStatus.Failed.ToString();
            job.ErrorSummary = "Staging failed — rolled back partial rows";
            await _repo.UpdateJobAsync(job, cancellationToken);
            throw;
        }

        job.DetectedColumns = staging.Columns;
        job.DetectedEncoding = staging.Encoding;
        job.DetectedDelimiter = staging.Delimiter;
        job.TotalRows = staging.TotalRows;
        job.Status = staging.TotalRows == 0 ? DataHubJobStatus.Failed.ToString() : DataHubJobStatus.MappingRequired.ToString();
        if (staging.TotalRows == 0) job.ErrorSummary = "No rows detected in file";

        await _repo.UpdateJobAsync(job, cancellationToken);

        var autoMap = _fields.SuggestMappings(targetEntity, staging.Columns);
        if (autoMap.Mappings.Count > 0)
            await SaveMappingsInternalAsync(tenantId, jobId, autoMap.Mappings, cancellationToken);

        await LogAsync(tenantId, jobId, "Info", $"Parsed {staging.TotalRows} rows via {job.Metadata.GetValueOrDefault("stagingMode")}, {staging.Columns.Count} columns", cancellationToken);

        var ai = _intelligence.AnalyzeFile(fileName, staging.Columns, staging.SampleForAi, targetEntity);
        job.Metadata["aiEntity"] = ai.SuggestedTargetEntity;
        job.Metadata["aiConfidence"] = ai.OverallConfidencePercent;
        job.Metadata["aiSummary"] = ai.Summary;
        job.Metadata["wizardStep"] = (int)DataHubWizardStep.Analyze;
        if (string.IsNullOrWhiteSpace(targetEntity) || targetEntity == "Customer")
            job.TargetEntity = ai.SuggestedTargetEntity;
        await _repo.UpdateJobAsync(job, cancellationToken);

        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, DataHubForensicActions.Upload, userId, jobId, job.FileName, job.FileSizeBytes, fileHash,
            _requestContext.ClientIp, _requestContext.UserAgent,
            new Dictionary<string, object> { ["targetEntity"] = targetEntity, ["loadMode"] = loadMode, ["encrypted"] = true }),
            cancellationToken);

        return new DataHubUploadResultDto(jobId, job.Status, staging.Columns, Math.Min(staging.TotalRows, DataHubConstants.MaxPreviewRows));
    }

    public async Task<DataHubAiAnalysisResultDto> AnalyzeWithAiAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var sample = await _repo.GetRowsAsync(tenantId, jobId, 0, 50, cancellationToken);
        var sampleData = sample.Select(r => r.RawData).ToList();
        var result = _intelligence.AnalyzeFile(job.FileName, job.DetectedColumns, sampleData, job.TargetEntity);
        job.Metadata["aiEntity"] = result.SuggestedTargetEntity;
        job.Metadata["aiConfidence"] = result.OverallConfidencePercent;
        job.Metadata["aiSummary"] = result.Summary;
        job.Metadata["wizardStep"] = (int)DataHubWizardStep.DetectType;
        job.TargetEntity = result.SuggestedTargetEntity;
        await SaveMappingsInternalAsync(tenantId, jobId, result.SuggestedMappings, cancellationToken);
        await _repo.UpdateJobAsync(job, cancellationToken);
        await LogAsync(tenantId, jobId, "Info", $"AI analysis: {result.SuggestedTargetEntity} ({result.OverallConfidencePercent}%)", cancellationToken);
        return result;
    }

    public async Task<DataHubAutoMapResult> AutoMapAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var result = _fields.SuggestMappings(job.TargetEntity, job.DetectedColumns);
        await SaveMappingsInternalAsync(tenantId, jobId, result.Mappings, cancellationToken);
        return result;
    }

    public async Task SaveMappingsAsync(Guid tenantId, Guid jobId, IReadOnlyList<DataHubMappingDto> mappings, CancellationToken cancellationToken = default)
    {
        await RequireJobAsync(tenantId, jobId, cancellationToken);
        await SaveMappingsInternalAsync(tenantId, jobId, mappings, cancellationToken);
    }

    public async Task<DataHubValidationResultDto> ValidateAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        job.Status = DataHubJobStatus.Validating.ToString();
        await _repo.UpdateJobAsync(job, cancellationToken);

        var mappings = await _repo.GetMappingsAsync(tenantId, jobId, cancellationToken);
        if (mappings.Count == 0)
        {
            var auto = _fields.SuggestMappings(job.TargetEntity, job.DetectedColumns);
            await SaveMappingsInternalAsync(tenantId, jobId, auto.Mappings, cancellationToken);
            mappings = await _repo.GetMappingsAsync(tenantId, jobId, cancellationToken);
        }

        var rules = await _repo.GetValidationRulesAsync(tenantId, job.TargetEntity, cancellationToken);
        var transformRules = await _repo.GetTransformationRulesAsync(tenantId, job.TargetEntity, cancellationToken);
        var allErrors = new List<DataHubImportError>();
        var valid = 0;
        var invalid = 0;
        var skip = 0;

        while (true)
        {
            var batch = await _repo.GetRowsAsync(tenantId, jobId, skip, DataHubConstants.DefaultBatchSize, cancellationToken);
            if (batch.Count == 0) break;

            foreach (var row in batch)
            {
                var transformed = _transform.TransformRow(row.RawData, mappings, transformRules);
                row.TransformedData = transformed;
                var rowErrors = await _validate.ValidateRowAsync(tenantId, job.TargetEntity, row.RowNumber, transformed, rules, cancellationToken);
                foreach (var err in rowErrors) err.JobId = jobId;
                if (rowErrors.Count > 0)
                {
                    row.Status = DataHubRowStatus.Invalid.ToString();
                    invalid++;
                    allErrors.AddRange(rowErrors);
                }
                else
                {
                    row.Status = DataHubRowStatus.Valid.ToString();
                    valid++;
                }
            }

            if (allErrors.Count > 0)
                await _repo.AddErrorsAsync(allErrors.TakeLast(500), cancellationToken);

            await _repo.UpdateRowsAsync(batch, cancellationToken);

            skip += batch.Count;
            job.ProcessedRows = skip;
            await _repo.UpdateJobAsync(job, cancellationToken);
        }

        job.FailedRows = invalid;
        job.SuccessRows = valid;
        var ready = invalid == 0;
        job.Status = ready
            ? DataHubJobStatus.ReadyToImport.ToString()
            : DataHubJobStatus.ValidationFailed.ToString();
        job.ErrorSummary = invalid > 0 ? $"{invalid} rows failed validation" : null;
        await _repo.UpdateJobAsync(job, cancellationToken);
        await LogAsync(tenantId, jobId, "Info", $"Validation complete: {valid} valid, {invalid} invalid", cancellationToken);

        return new DataHubValidationResultDto(
            jobId, job.TotalRows, valid, invalid,
            allErrors.Take(100).Select(e => new DataHubErrorDto(e.RowNumber, e.ErrorCode, e.Message, e.FieldName, e.IsRetryable)).ToList(),
            ready);
    }

    public async Task<DataHubImportResultDto> StartImportAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        if (job.Status == DataHubJobStatus.Cancelled.ToString())
            throw new InvalidOperationException("Job was cancelled");
        if (job.Status == DataHubJobStatus.ValidationFailed.ToString())
            throw new InvalidOperationException("Job failed validation and cannot be imported");
        if (job.Status != DataHubJobStatus.ReadyToImport.ToString())
            throw new InvalidOperationException($"Job is not ready to import (status: {job.Status})");

        await _quotas.EnsureImportStartAllowedAsync(tenantId, cancellationToken);

        job.Status = DataHubJobStatus.Importing.ToString();
        job.StartedAt ??= DateTime.UtcNow;
        await _repo.UpdateJobAsync(job, cancellationToken);
        await NotifyProgressAsync(job, cancellationToken);

        await _dispatcher.EnqueueImportJobAsync(tenantId, jobId, cancellationToken);

        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, DataHubForensicActions.ImportStart, job.CreatedByUserId, jobId, job.FileName, job.FileSizeBytes,
            null, _requestContext.ClientIp, _requestContext.UserAgent), cancellationToken);

        return new DataHubImportResultDto(jobId, job.Status, job.TotalRows, 0, 0, 0, 0, Array.Empty<DataHubErrorDto>());
    }

    public Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        => ProcessJobCoreAsync(jobId, acquireLock: true, cancellationToken);

    internal async Task ProcessJobCoreAsync(Guid jobId, bool acquireLock, CancellationToken cancellationToken = default)
    {
        if (acquireLock && !await _repo.TryAcquireJobProcessingLockAsync(jobId, cancellationToken))
            return;

        try
        {
        var job = await _repo.GetJobByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException("Job not found");

        if (job.Status != DataHubJobStatus.Importing.ToString() && job.Status != DataHubJobStatus.ReadyToImport.ToString())
            return;

        job.Status = DataHubJobStatus.Importing.ToString();
        job.StartedAt ??= DateTime.UtcNow;
        await _repo.UpdateJobAsync(job, cancellationToken);

        var sw = Stopwatch.StartNew();
        var mappings = await _repo.GetMappingsAsync(job.TenantId, jobId, cancellationToken);
        var transformRules = await _repo.GetTransformationRulesAsync(job.TenantId, job.TargetEntity, cancellationToken);
        var created = 0;
        var updated = 0;
        var failed = 0;
        var skipped = 0;
        var skip = 0;
        var snapshots = new List<DataHubRollbackSnapshot>();
        var batchNumber = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var refreshed = await _repo.GetJobAsync(job.TenantId, jobId, cancellationToken);
            if (refreshed?.Status == DataHubJobStatus.Cancelled.ToString()) break;

            var batch = await _repo.GetRowsAsync(job.TenantId, jobId, skip, DataHubConstants.DefaultBatchSize, cancellationToken);
            if (batch.Count == 0) break;

            batchNumber++;
            foreach (var row in batch)
                row.BatchNumber = batchNumber;

            foreach (var row in batch.Where(r => r.Status == DataHubRowStatus.Valid.ToString()))
            {
                if (row.Status == DataHubRowStatus.Imported.ToString()) continue;

                var data = row.TransformedData.Count > 0
                    ? new Dictionary<string, string?>(row.TransformedData)
                    : _transform.TransformRow(row.RawData, mappings, transformRules);

                data["_jobId"] = jobId.ToString();

                var result = await _load.LoadRowAsync(
                    job.TenantId, job.TargetEntity, job.LoadMode, data, job.IsDryRun,
                    row.RowNumber, batchNumber, cancellationToken);

                if (result.Error != null)
                {
                    failed++;
                    row.Status = DataHubRowStatus.Failed.ToString();
                    await _repo.AddErrorsAsync([new DataHubImportError
                    {
                        Id = Guid.NewGuid(), JobId = jobId, TenantId = job.TenantId,
                        RowNumber = row.RowNumber, ErrorCode = "LoadFailed", Message = result.Error
                    }], cancellationToken);
                }
                else if (result.Skipped > 0)
                {
                    skipped++;
                    row.Status = DataHubRowStatus.Skipped.ToString();
                }
                else
                {
                    created += result.Created;
                    updated += result.Updated;
                    row.Status = DataHubRowStatus.Imported.ToString();
                    row.CreatedEntityId = result.EntityId;
                    row.EntityType = job.TargetEntity;
                    if (result.RollbackSnapshot != null && !job.IsDryRun)
                        snapshots.Add(result.RollbackSnapshot);
                }
            }

            await _repo.UpdateRowsAsync(batch, cancellationToken);

            if (snapshots.Count >= 50)
            {
                await _repo.AddRollbackSnapshotsAsync(snapshots, cancellationToken);
                job.RollbackAvailable = true;
                snapshots.Clear();
            }

            skip += batch.Count;
            job.ProcessedRows = skip;
            job.CreatedRecords = created;
            job.UpdatedRecords = updated;
            job.FailedRows = failed;
            job.SkippedRows = skipped;
            job.SuccessRows = created + updated;
            await _repo.UpdateJobAsync(job, cancellationToken);
            await NotifyProgressAsync(job, cancellationToken);
        }

        if (snapshots.Count > 0)
        {
            await _repo.AddRollbackSnapshotsAsync(snapshots, cancellationToken);
            job.RollbackAvailable = true;
        }

        job.CompletedAt = DateTime.UtcNow;
        job.Status = failed > 0
            ? DataHubJobStatus.CompletedWithErrors.ToString()
            : DataHubJobStatus.Completed.ToString();
        await _repo.UpdateJobAsync(job, cancellationToken);
        await NotifyProgressAsync(job, cancellationToken);
        await LogAsync(job.TenantId, jobId, "Info", $"Import completed in {sw.Elapsed.TotalSeconds:F1}s — created={created}, updated={updated}, failed={failed}", cancellationToken);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            job.TenantId, DataHubForensicActions.ImportComplete, job.CreatedByUserId, jobId, job.FileName, job.FileSizeBytes,
            null, null, null,
            new Dictionary<string, object> { ["created"] = created, ["updated"] = updated, ["failed"] = failed, ["skipped"] = skipped }),
            cancellationToken);

        if (job.Status == DataHubJobStatus.Completed.ToString())
            await _migrationSync.TryCompleteMigrationSyncAsync(job.TenantId, jobId, cancellationToken);
        }
        finally
        {
            if (acquireLock)
                await _repo.ReleaseJobProcessingLockAsync(jobId, cancellationToken);
        }
    }

    public async Task CancelJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        job.Status = DataHubJobStatus.Cancelled.ToString();
        job.CancelledAt = DateTime.UtcNow;
        await _repo.UpdateJobAsync(job, cancellationToken);
        await LogAsync(tenantId, jobId, "Warning", "Job cancelled by user", cancellationToken);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, DataHubForensicActions.Cancel, job.CreatedByUserId, jobId, job.FileName, job.FileSizeBytes,
            null, _requestContext.ClientIp, _requestContext.UserAgent), cancellationToken);
    }

    public async Task<DataHubImportResultDto> RetryFailedRowsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var rows = await _repo.GetRowsAsync(tenantId, jobId, 0, job.TotalRows, cancellationToken);
        var failedRows = rows.Where(r => r.Status == DataHubRowStatus.Failed.ToString()).ToList();
        if (failedRows.Count == 0)
            throw new InvalidOperationException("No failed rows to retry");

        foreach (var row in failedRows)
        {
            row.Status = DataHubRowStatus.Pending.ToString();
            row.TransformedData = new Dictionary<string, string?>();
        }
        await _repo.UpdateRowsAsync(failedRows, cancellationToken);

        job.ProcessedRows = 0;
        job.FailedRows = 0;
        job.Status = DataHubJobStatus.Validating.ToString();
        await _repo.UpdateJobAsync(job, cancellationToken);

        var validation = await ValidateAsync(tenantId, jobId, cancellationToken);
        if (!validation.ReadyToImport)
            throw new InvalidOperationException($"Retry validation failed: {validation.InvalidRows} invalid rows remain");

        return await StartImportAsync(tenantId, jobId, cancellationToken);
    }

    public async Task RecoverOrphanJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _repo.GetJobByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException("Job not found");

        if (job.Status != DataHubJobStatus.Importing.ToString())
            return;

        job.Status = DataHubJobStatus.Validating.ToString();
        job.ProcessedRows = 0;
        await _repo.UpdateJobAsync(job, cancellationToken);

        var validation = await ValidateAsync(job.TenantId, jobId, cancellationToken);
        if (!validation.ReadyToImport)
        {
            await LogAsync(job.TenantId, jobId, "Warning",
                $"Orphan recovery blocked: {validation.InvalidRows} invalid rows after re-validation", cancellationToken);
            return;
        }

        await _dispatcher.EnqueueImportJobAsync(job.TenantId, jobId, cancellationToken);
        await LogAsync(job.TenantId, jobId, "Info", "Orphan job re-validated and re-queued", cancellationToken);
    }

    public async Task RollbackJobAsync(Guid tenantId, Guid jobId, int? batchNumber = null, int? rowNumber = null, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        await _rollback.ExecuteRollbackAsync(tenantId, jobId, batchNumber, rowNumber, cancellationToken);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, DataHubForensicActions.Rollback, job.CreatedByUserId, jobId, job.FileName, job.FileSizeBytes,
            null, _requestContext.ClientIp, _requestContext.UserAgent,
            new Dictionary<string, object> { ["batchNumber"] = batchNumber ?? 0, ["rowNumber"] = rowNumber ?? 0 }),
            cancellationToken);
    }

    public Task<DataHubDuplicateScanResultDto> ScanDuplicatesAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        => _duplicates.ScanJobAsync(tenantId, jobId, cancellationToken);

    public async Task UpdateStagingRowsAsync(Guid tenantId, Guid jobId, IReadOnlyList<DataHubStagingRowUpdateDto> updates, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var rows = await _repo.GetRowsAsync(tenantId, jobId, 0, job.TotalRows, cancellationToken);
        foreach (var update in updates)
        {
            var row = rows.FirstOrDefault(r => r.RowNumber == update.RowNumber);
            if (row == null) continue;
            foreach (var kv in update.Data)
                row.RawData[kv.Key] = kv.Value;
            row.TransformedData = new Dictionary<string, string?>(row.RawData);
            if (row.Status == DataHubRowStatus.Invalid.ToString())
                row.Status = DataHubRowStatus.Pending.ToString();
        }
        await _repo.UpdateRowsAsync(rows, cancellationToken);
        await LogAsync(tenantId, jobId, "Info", $"Updated {updates.Count} staging rows from preview editor", cancellationToken);
    }

    public async Task<DataHubImportSummaryDto> GetImportSummaryAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var snapshots = await _repo.GetRollbackSnapshotsAsync(tenantId, jobId, cancellationToken);
        var createdLinks = snapshots
            .Where(s => s.Action == "Created")
            .Take(50)
            .Select(s => new DataHubImportSummaryLinkDto(s.EntityType, s.EntityId, s.EntityType, CrmLink(s.EntityType, s.EntityId)))
            .ToList();
        var updatedLinks = snapshots
            .Where(s => s.Action == "Updated")
            .Take(50)
            .Select(s => new DataHubImportSummaryLinkDto(s.EntityType, s.EntityId, $"{s.EntityType} (updated)", CrmLink(s.EntityType, s.EntityId)))
            .ToList();

        if (createdLinks.Count == 0)
        {
            var rows = await _repo.GetRowsAsync(tenantId, jobId, 0, job.TotalRows, cancellationToken);
            createdLinks = rows
                .Where(r => r.CreatedEntityId.HasValue && r.Status == DataHubRowStatus.Imported.ToString())
                .Take(50)
                .Select(r => new DataHubImportSummaryLinkDto(
                    r.EntityType ?? job.TargetEntity, r.CreatedEntityId!.Value,
                    $"Row {r.RowNumber}", CrmLink(r.EntityType ?? job.TargetEntity, r.CreatedEntityId.Value)))
                .ToList();
        }

        var dupScan = await _duplicates.ScanJobAsync(tenantId, jobId, cancellationToken);
        var cleaning = await GetCleaningSummaryAsync(tenantId, jobId, cancellationToken);
        var qualityScore = cleaning.TotalRows > 0
            ? (int)Math.Round(cleaning.ValidRows / (double)cleaning.TotalRows * 100)
            : 100;
        var grade = qualityScore >= 90 ? "Excellent" : qualityScore >= 75 ? "Good" : qualityScore >= 60 ? "Fair" : "Needs work";
        TimeSpan? duration = job.StartedAt.HasValue && job.CompletedAt.HasValue
            ? job.CompletedAt.Value - job.StartedAt.Value
            : job.StartedAt.HasValue ? DateTime.UtcNow - job.StartedAt.Value : null;

        return new DataHubImportSummaryDto(
            jobId, job.TargetEntity, job.FileName,
            job.CreatedRecords, job.UpdatedRecords, job.SkippedRows, job.FailedRows,
            dupScan.TotalDuplicateRows, qualityScore, grade, duration, job.RollbackAvailable,
            createdLinks, updatedLinks);
    }

    private static string CrmLink(string entityType, Guid id) => entityType switch
    {
        "Lead" => $"/Leads/Detail?id={id}",
        "Customer" => $"/Customers/Detail?id={id}",
        "Deal" => $"/Deals/Detail?id={id}",
        _ => $"/DataHub/Job/{id}"
    };

    public async Task<DataHubExtendedValidationResultDto> ValidateExtendedAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var basic = await ValidateAsync(tenantId, jobId, cancellationToken);
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        await _duplicates.ApplyDuplicatePolicyAsync(tenantId, jobId, job.TargetEntity, job.LoadMode, cancellationToken);
        var summary = await GetCleaningSummaryAsync(tenantId, jobId, cancellationToken);
        var issues = basic.Errors.Select(e => new DataHubValidationIssueDto(
            e.RowNumber, e.ErrorCode, e.Message, e.FieldName,
            DataHubIssueSeverity.Error, e.IsRetryable,
            e.ErrorCode is "InvalidEmail" or "InvalidPhone" or "MaxLength")).ToList();

        var warningCount = summary.WarningRows;
        return new DataHubExtendedValidationResultDto(jobId, summary, issues, summary.ReadyToImport);
    }

    public async Task<DataHubCleaningSummaryDto> GetCleaningSummaryAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var rows = await _repo.GetRowsAsync(tenantId, jobId, 0, Math.Min(job.TotalRows, 5000), cancellationToken);
        var valid = rows.Count(r => r.Status == DataHubRowStatus.Valid.ToString() || r.Status == DataHubRowStatus.Imported.ToString());
        var errors = rows.Count(r => r.Status == DataHubRowStatus.Invalid.ToString() || r.Status == DataHubRowStatus.Failed.ToString());
        var warnings = rows.Count(r => r.Status == DataHubRowStatus.Pending.ToString());
        var emails = rows.Select(r => r.TransformedData.GetValueOrDefault("Email") ?? r.RawData.GetValueOrDefault("Email"))
            .Where(e => !string.IsNullOrWhiteSpace(e)).GroupBy(e => e!.ToLower()).Count(g => g.Count() > 1);

        var total = job.TotalRows;
        var validPct = total > 0 ? (double)valid / total * 100 : 0;
        job.Metadata["cleaningValid"] = valid;
        job.Metadata["cleaningWarnings"] = warnings;
        job.Metadata["cleaningErrors"] = errors;
        job.Metadata["cleaningDuplicates"] = emails;
        await _repo.UpdateJobAsync(job, cancellationToken);

        return new DataHubCleaningSummaryDto(jobId, total, valid, warnings, errors, emails, Math.Round(validPct, 1), errors == 0);
    }

    public Task<DataHubAutoFixResultDto> AutoFixAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        => _autoFix.AutoFixJobAsync(tenantId, jobId, cancellationToken);

    public async Task<DataHubTemplateSummaryDto> SaveTemplateFromJobAsync(Guid tenantId, Guid jobId, string templateName, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var mappings = await _repo.GetMappingsAsync(tenantId, jobId, cancellationToken);
        var template = new DataHubImportTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = templateName,
            TargetEntity = job.TargetEntity,
            FileFormat = job.FileFormat,
            Mappings = mappings.Select(m => new DataHubTemplateMapping
            {
                SourceColumn = m.SourceColumn,
                TargetField = m.TargetField,
                IsRequired = m.IsRequired,
                DefaultValue = m.DefaultValue,
                TransformRule = m.TransformRule
            }).ToList()
        };
        await _repo.SaveTemplateAsync(template, cancellationToken);
        await _templateVersions.EnsureInitialVersionAsync(tenantId, job.CreatedByUserId, template, cancellationToken);
        return new DataHubTemplateSummaryDto(
            template.Id, template.Name, template.TargetEntity, template.Mappings.Count, template.UpdatedAt,
            template.ActiveVersion, template.LatestVersion);
    }

    public async Task<DataHubJobMetricsDto> GetJobMetricsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await RequireJobAsync(tenantId, jobId, cancellationToken);
        var pct = job.TotalRows > 0 ? (double)job.ProcessedRows / job.TotalRows * 100 : 0;
        int rpm = 0;
        TimeSpan? eta = null;
        if (job.StartedAt.HasValue && job.ProcessedRows > 0)
        {
            var elapsed = DateTime.UtcNow - job.StartedAt.Value;
            if (elapsed.TotalMinutes > 0.1)
            {
                rpm = (int)(job.ProcessedRows / elapsed.TotalMinutes);
                var remaining = job.TotalRows - job.ProcessedRows;
                if (rpm > 0) eta = TimeSpan.FromMinutes(remaining / (double)rpm);
            }
        }
        return new DataHubJobMetricsDto(jobId, job.Status, Math.Round(pct, 1), rpm, eta, job.StartedAt, null);
    }

    private async Task SaveMappingsInternalAsync(Guid tenantId, Guid jobId, IReadOnlyList<DataHubMappingDto> mappings, CancellationToken ct)
    {
        var entities = mappings.Select((m, i) => new DataHubImportMapping
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            SourceColumn = m.SourceColumn,
            TargetField = m.TargetField,
            IsRequired = m.IsRequired,
            DefaultValue = m.DefaultValue,
            TransformRule = m.TransformRule,
            SortOrder = i
        }).ToList();
        await _repo.ReplaceMappingsAsync(tenantId, jobId, entities, ct);
    }

    private async Task<DataHubImportJob> RequireJobAsync(Guid tenantId, Guid jobId, CancellationToken ct)
    {
        var job = await _repo.GetJobAsync(tenantId, jobId, ct);
        if (job == null) throw new DataHubTenantAccessException("Job not found or access denied");
        return job;
    }

    private Task LogAsync(Guid tenantId, Guid jobId, string level, string message, CancellationToken ct)
        => _repo.AddLogAsync(new DataHubImportLog
        {
            Id = Guid.NewGuid(), JobId = jobId, TenantId = tenantId, Level = level, Message = message
        }, ct);

    private Task NotifyProgressAsync(DataHubImportJob job, CancellationToken cancellationToken)
    {
        var pct = job.TotalRows > 0 ? Math.Round((double)job.ProcessedRows / job.TotalRows * 100, 1) : 0;
        var pending = Math.Max(0, job.TotalRows - job.ProcessedRows);
        var rpm = 0;
        string? eta = null;
        if (job.StartedAt.HasValue && job.ProcessedRows > 0)
        {
            var elapsed = DateTime.UtcNow - job.StartedAt.Value;
            if (elapsed.TotalMinutes > 0.1)
            {
                rpm = (int)(job.ProcessedRows / elapsed.TotalMinutes);
                if (rpm > 0) eta = TimeSpan.FromMinutes(pending / (double)rpm).ToString(@"mm\mss\s");
            }
        }

        return _progress.NotifyProgressAsync(new DataHubProgressUpdateDto(
            job.Id, job.TenantId, job.Status, pct, job.TotalRows,
            job.ProcessedRows, pending, job.SuccessRows, job.FailedRows, job.SkippedRows,
            job.CreatedRecords, job.UpdatedRecords, rpm, eta), cancellationToken);
    }

    public static DataHubJobSummaryDto ToSummary(DataHubImportJob job)
    {
        var pct = job.TotalRows > 0 ? (double)job.ProcessedRows / job.TotalRows * 100 : 0;
        return new DataHubJobSummaryDto(
            job.Id, job.FileName, job.TargetEntity, job.Status, job.LoadMode,
            job.TotalRows, job.ProcessedRows, job.SuccessRows, job.FailedRows, job.SkippedRows,
            job.CreatedRecords, job.UpdatedRecords,
            Math.Round(pct, 1), job.CreatedAt, job.CompletedAt, job.RollbackAvailable, job.ErrorSummary);
    }
}

file sealed class UploadStagingResult
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, string?>> SampleForAi { get; set; } = [];
    public string Encoding { get; set; } = "UTF-8";
    public string? Delimiter { get; set; }
    public int TotalRows { get; set; }
}
