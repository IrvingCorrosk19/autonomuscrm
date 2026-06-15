using System.Security.Claims;
using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/datahub")]
[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class DataHubController : ControllerBase
{
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IDataHubRepository _repo;
    private readonly IDataHubExportService _export;
    private readonly IDataHubValidateService _quality;
    private readonly IDataHubQualityScoreService _qualityScore;
    private readonly IDataHubQualityActionService _qualityActions;
    private readonly IDataHubRulesEngineService _rules;
    private readonly IDataHubTenantGuard _tenantGuard;
    private readonly IDataHubForensicAuditService _forensic;
    private readonly IDataHubSecurityQuotaService _quotas;
    private readonly IDataHubMigrationService _migration;
    private readonly IDataHubScheduledImportService _schedules;
    private readonly IDataHubTemplateVersionService _templateVersions;
    private readonly IDataHubIntelligenceService _intelligence;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DataHubController(
        IDataHubOrchestrator orchestrator,
        IDataHubRepository repo,
        IDataHubExportService export,
        IDataHubValidateService quality,
        IDataHubQualityScoreService qualityScore,
        IDataHubQualityActionService qualityActions,
        IDataHubRulesEngineService rules,
        IDataHubTenantGuard tenantGuard,
        IDataHubForensicAuditService forensic,
        IDataHubSecurityQuotaService quotas,
        IDataHubMigrationService migration,
        IDataHubScheduledImportService schedules,
        IDataHubTemplateVersionService templateVersions,
        IDataHubIntelligenceService intelligence,
        IHttpContextAccessor httpContextAccessor)
    {
        _orchestrator = orchestrator;
        _repo = repo;
        _export = export;
        _quality = quality;
        _qualityScore = qualityScore;
        _qualityActions = qualityActions;
        _rules = rules;
        _tenantGuard = tenantGuard;
        _forensic = forensic;
        _quotas = quotas;
        _migration = migration;
        _schedules = schedules;
        _templateVersions = templateVersions;
        _intelligence = intelligence;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(DataHubConstants.MaxFileBytes)]
    public async Task<ActionResult<DataHubUploadResultDto>> Upload(
        [FromQuery] Guid tenantId,
        IFormFile file,
        [FromQuery] string targetEntity = "Customer",
        [FromQuery] string loadMode = "InsertOnly",
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        if (file == null || file.Length == 0) return BadRequest("File required");

        try
        {
            await using var stream = file.OpenReadStream();
            var userId = GetUserId();
            var result = await _orchestrator.UploadAsync(tenantId, userId, stream, file.FileName, targetEntity, loadMode, dryRun, cancellationToken);
            return Ok(result);
        }
        catch (DataHubMalwareDetectedException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DataHubSecurityQuotaException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, ex.Message);
        }
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<IReadOnlyList<DataHubJobSummaryDto>>> ListJobs([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var jobs = await _repo.ListJobsAsync(tenantId, 100, cancellationToken);
        return Ok(jobs.Select(DataHubOrchestrator.ToSummary).ToList());
    }

    [HttpGet("jobs/{jobId:guid}")]
    public async Task<ActionResult<DataHubJobDetailDto>> GetJob([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var job = await _repo.GetJobAsync(tenantId, jobId, cancellationToken);
        if (job == null) return NotFound();

        var mappings = await _repo.GetMappingsAsync(tenantId, jobId, cancellationToken);
        var logs = await _repo.GetLogsAsync(tenantId, jobId, 20, cancellationToken);
        var errors = await _repo.GetErrorsAsync(tenantId, jobId, 0, 50, cancellationToken);
        var preview = await _repo.GetRowsAsync(tenantId, jobId, 0, DataHubConstants.MaxPreviewRows, cancellationToken);

        return Ok(new DataHubJobDetailDto(
            DataHubOrchestrator.ToSummary(job),
            job.DetectedColumns,
            mappings.Select(m => new DataHubMappingDto(m.Id, m.SourceColumn, m.TargetField, m.IsRequired, m.DefaultValue, m.TransformRule)).ToList(),
            logs.Select(l => new DataHubLogDto(l.CreatedAt, l.Level, l.Message)).ToList(),
            errors.Select(e => new DataHubErrorDto(e.RowNumber, e.ErrorCode, e.Message, e.FieldName, e.IsRetryable)).ToList(),
            preview.Select(r => new DataHubRowPreviewDto(r.RowNumber, r.Status, r.TransformedData.Count > 0 ? r.TransformedData : r.RawData)).ToList()));
    }

    [HttpPost("jobs/{jobId:guid}/automap")]
    public async Task<ActionResult<DataHubAutoMapResult>> AutoMap([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.AutoMapAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPut("jobs/{jobId:guid}/mappings")]
    public async Task<IActionResult> SaveMappings([FromQuery] Guid tenantId, Guid jobId, [FromBody] List<DataHubMappingDto> mappings, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        await _orchestrator.SaveMappingsAsync(tenantId, jobId, mappings, cancellationToken);
        return NoContent();
    }

    [HttpPost("jobs/{jobId:guid}/validate")]
    public async Task<ActionResult<DataHubValidationResultDto>> Validate([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.ValidateAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPost("jobs/{jobId:guid}/import")]
    public async Task<ActionResult<DataHubImportResultDto>> Import([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.StartImportAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPost("jobs/{jobId:guid}/cancel")]
    public async Task<IActionResult> Cancel([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        await _orchestrator.CancelJobAsync(tenantId, jobId, cancellationToken);
        return NoContent();
    }

    [HttpPost("jobs/{jobId:guid}/retry")]
    public async Task<ActionResult<DataHubImportResultDto>> Retry([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.RetryFailedRowsAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPost("jobs/{jobId:guid}/rollback")]
    public async Task<IActionResult> Rollback([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        await _orchestrator.RollbackJobAsync(tenantId, jobId, cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpGet("jobs/{jobId:guid}/errors/export")]
    public async Task<IActionResult> ExportErrors([FromQuery] Guid tenantId, Guid jobId, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var formatNorm = format.ToLowerInvariant();
        var contentType = formatNorm switch
        {
            "json" => "application/json",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv"
        };
        Response.ContentType = contentType;
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"errors-{jobId:N}.{formatNorm}\"");
        await _export.ExportErrorsToStreamAsync(tenantId, jobId, formatNorm, Response.Body, cancellationToken);

        var (ip, ua) = DataHubSecurityContext.FromHttp(_httpContextAccessor.HttpContext);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, DataHubForensicActions.ExportErrors, GetUserId(), jobId, $"errors-{jobId:N}.{formatNorm}", null,
            null, ip, ua), cancellationToken);

        return new EmptyResult();
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] Guid tenantId, [FromQuery] string entityType = "Customer", [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            await _quotas.EnsureExportAllowedAsync(tenantId, cancellationToken);
        }
        catch (DataHubSecurityQuotaException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, ex.Message);
        }

        var formatNorm = format.ToLowerInvariant();
        var contentType = formatNorm switch
        {
            "json" => "application/json",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv"
        };
        Response.ContentType = contentType;
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{entityType.ToLowerInvariant()}-export.{formatNorm}\"");
        await _export.ExportToStreamAsync(tenantId, entityType, formatNorm, Response.Body, null, cancellationToken);

        var (ip, ua) = DataHubSecurityContext.FromHttp(_httpContextAccessor.HttpContext);
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, DataHubForensicActions.Export, GetUserId(), null, $"{entityType}.{formatNorm}", null,
            null, ip, ua, new Dictionary<string, object> { ["entityType"] = entityType, ["format"] = formatNorm }),
            cancellationToken);

        return new EmptyResult();
    }

    [HttpPost("jobs/{jobId:guid}/analyze")]
    public async Task<ActionResult<DataHubAiAnalysisResultDto>> AnalyzeAi([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.AnalyzeWithAiAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPost("jobs/{jobId:guid}/autofix")]
    public async Task<ActionResult<DataHubAutoFixResultDto>> AutoFix([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.AutoFixAsync(tenantId, jobId, cancellationToken));
    }

    [HttpGet("jobs/{jobId:guid}/cleaning")]
    public async Task<ActionResult<DataHubCleaningSummaryDto>> CleaningSummary([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.GetCleaningSummaryAsync(tenantId, jobId, cancellationToken));
    }

    [HttpGet("jobs/{jobId:guid}/metrics")]
    public async Task<ActionResult<DataHubJobMetricsDto>> Metrics([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.GetJobMetricsAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPost("jobs/{jobId:guid}/templates")]
    public async Task<ActionResult<DataHubTemplateSummaryDto>> SaveTemplate([FromQuery] Guid tenantId, Guid jobId, [FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.SaveTemplateFromJobAsync(tenantId, jobId, name, cancellationToken));
    }

    [HttpGet("jobs/{jobId:guid}/duplicates")]
    public async Task<ActionResult<DataHubDuplicateScanResultDto>> ScanDuplicates([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.ScanDuplicatesAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPut("jobs/{jobId:guid}/staging")]
    public async Task<IActionResult> UpdateStaging([FromQuery] Guid tenantId, Guid jobId, [FromBody] List<DataHubStagingRowUpdateDto> updates, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        await _orchestrator.UpdateStagingRowsAsync(tenantId, jobId, updates, cancellationToken);
        return NoContent();
    }

    [HttpGet("jobs/{jobId:guid}/summary")]
    public async Task<ActionResult<DataHubImportSummaryDto>> ImportSummary([FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _orchestrator.GetImportSummaryAsync(tenantId, jobId, cancellationToken));
    }

    [HttpPost("quality/actions/merge")]
    public async Task<ActionResult<DataHubQualityActionResultDto>> MergeCustomers(
        [FromQuery] Guid tenantId, [FromQuery] Guid keepId, [FromQuery] string mergeIds, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var ids = mergeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
        return Ok(await _qualityActions.MergeCustomersAsync(tenantId, keepId, ids, cancellationToken));
    }

    [HttpPost("quality/actions/auto-assign")]
    public async Task<ActionResult<DataHubQualityActionResultDto>> AutoAssignLeads([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _qualityActions.AutoAssignLeadsAsync(tenantId, cancellationToken));
    }

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<DataHubVisualRuleDto>>> GetRules(
        [FromQuery] Guid tenantId, [FromQuery] string targetEntity = "Customer", CancellationToken cancellationToken = default)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _rules.GetRulesAsync(tenantId, targetEntity, cancellationToken));
    }

    [HttpPut("rules")]
    public async Task<ActionResult<DataHubRuleSetVersionDto>> SaveRules(
        [FromQuery] Guid tenantId, [FromQuery] string targetEntity, [FromBody] List<DataHubVisualRuleDto> rules, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _rules.SaveRulesAsync(tenantId, targetEntity, rules, cancellationToken));
    }

    [HttpGet("quality/score")]
    public async Task<ActionResult<DataHubQualityScoreDto>> QualityScore([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _qualityScore.CalculateScoreAsync(tenantId, cancellationToken));
    }

    [HttpGet("quality")]
    public async Task<ActionResult<IReadOnlyList<DataHubQualityIssueDto>>> Quality([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _quality.ScanQualityAsync(tenantId, cancellationToken));
    }

    [HttpGet("migration/sources")]
    public async Task<ActionResult<IReadOnlyList<DataHubMigrationSourceDto>>> MigrationSources(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _migration.ListSourcesAsync(tenantId, cancellationToken));
    }

    [HttpGet("migration/entities")]
    public ActionResult<IReadOnlyList<DataHubMigrationEntityDto>> MigrationEntities([FromQuery] string source)
        => Ok(_migration.ListEntities(source));

    [HttpGet("migration/connection")]
    public async Task<ActionResult<DataHubMigrationConnectionStatusDto>> MigrationConnection(
        [FromQuery] Guid tenantId, [FromQuery] string source, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _migration.GetConnectionStatusAsync(tenantId, source, cancellationToken));
    }

    [HttpPost("migration/start")]
    public async Task<ActionResult<DataHubMigrationStartResultDto>> StartMigration(
        [FromQuery] Guid tenantId,
        [FromQuery] string source,
        [FromQuery] string sourceEntity,
        [FromQuery] string mode = "Full",
        [FromQuery] string loadMode = "Upsert",
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        if (!Enum.TryParse<DataHubMigrationImportMode>(mode, true, out var importMode))
            return BadRequest("Invalid mode. Use Full or Delta.");

        try
        {
            var result = await _migration.StartMigrationAsync(new DataHubMigrationRequestDto(
                tenantId, GetUserId(), source, sourceEntity, importMode, loadMode, dryRun), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("migration/jobs/{jobId:guid}/quality")]
    public async Task<ActionResult<DataHubMigrationQualityReportDto>> MigrationQuality(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _migration.ValidateMigrationQualityAsync(tenantId, jobId, cancellationToken));
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<IReadOnlyList<DataHubScheduledImportDto>>> ListSchedules(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _schedules.ListAsync(tenantId, cancellationToken));
    }

    [HttpPost("schedules")]
    public async Task<ActionResult<DataHubScheduledImportDto>> CreateSchedule(
        [FromQuery] Guid tenantId, [FromBody] DataHubScheduledImportCreateDto dto, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _schedules.CreateAsync(tenantId, GetUserId(), dto, cancellationToken));
    }

    [HttpPut("schedules/{scheduleId:guid}")]
    public async Task<ActionResult<DataHubScheduledImportDto>> UpdateSchedule(
        [FromQuery] Guid tenantId, Guid scheduleId, [FromBody] DataHubScheduledImportUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var result = await _schedules.UpdateAsync(tenantId, scheduleId, dto, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> DeleteSchedule(
        [FromQuery] Guid tenantId, Guid scheduleId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        await _schedules.DeleteAsync(tenantId, scheduleId, cancellationToken);
        return NoContent();
    }

    [HttpGet("schedules/{scheduleId:guid}/runs")]
    public async Task<ActionResult<IReadOnlyList<DataHubScheduledImportRunDto>>> ListScheduleRuns(
        [FromQuery] Guid tenantId, Guid scheduleId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _schedules.ListRunsAsync(tenantId, scheduleId, 20, cancellationToken));
    }

    [HttpPost("schedules/{scheduleId:guid}/run")]
    public async Task<IActionResult> RunScheduleNow(
        [FromQuery] Guid tenantId, Guid scheduleId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        await _schedules.ExecuteNowAsync(tenantId, GetUserId(), scheduleId, cancellationToken);
        return Accepted();
    }

    [HttpGet("templates/{templateId:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<DataHubTemplateVersionDto>>> ListTemplateVersions(
        [FromQuery] Guid tenantId, Guid templateId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _templateVersions.ListVersionsAsync(tenantId, templateId, cancellationToken));
    }

    [HttpPost("templates/{templateId:guid}/versions")]
    public async Task<ActionResult<DataHubTemplateVersionDto>> CreateTemplateVersion(
        [FromQuery] Guid tenantId, Guid templateId, [FromQuery] string? summary, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _templateVersions.CreateVersionAsync(tenantId, GetUserId(), templateId, summary, cancellationToken));
    }

    [HttpGet("templates/{templateId:guid}/versions/compare")]
    public async Task<ActionResult<DataHubTemplateVersionCompareDto>> CompareTemplateVersions(
        [FromQuery] Guid tenantId, Guid templateId, [FromQuery] int versionA, [FromQuery] int versionB, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _templateVersions.CompareVersionsAsync(tenantId, templateId, versionA, versionB, cancellationToken));
    }

    [HttpPost("templates/{templateId:guid}/versions/{versionNumber:int}/restore")]
    public async Task<ActionResult<DataHubTemplateVersionDto>> RestoreTemplateVersion(
        [FromQuery] Guid tenantId, Guid templateId, int versionNumber, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _templateVersions.RestoreVersionAsync(tenantId, GetUserId(), templateId, versionNumber, cancellationToken));
    }

    [HttpPost("templates/{templateId:guid}/versions/{versionNumber:int}/activate")]
    public async Task<ActionResult<DataHubTemplateVersionDto>> ActivateTemplateVersion(
        [FromQuery] Guid tenantId, Guid templateId, int versionNumber, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _templateVersions.ActivateVersionAsync(tenantId, GetUserId(), templateId, versionNumber, cancellationToken));
    }

    [HttpPost("matching/v2")]
    public ActionResult<IReadOnlyList<DataHubSmartMatchResult>> MatchColumnsV2(
        [FromQuery] string targetEntity,
        [FromBody] DataHubMatchColumnsRequest request)
    {
        return Ok(_intelligence.MatchColumnsV2(targetEntity, request.Columns, request.SampleRows));
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
