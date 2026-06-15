using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class WizardModel : PageModel
{
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IDataHubRepository _repo;
    private readonly IDataHubIntelligenceService _intelligence;
    private readonly IDataHubRulesEngineService _rules;
    private readonly IDataHubFieldCatalog _fields;
    private readonly IServiceProvider _sp;

    public WizardModel(
        IDataHubOrchestrator orchestrator,
        IDataHubRepository repo,
        IDataHubIntelligenceService intelligence,
        IDataHubRulesEngineService rules,
        IDataHubFieldCatalog fields,
        IServiceProvider sp)
    {
        _orchestrator = orchestrator;
        _repo = repo;
        _intelligence = intelligence;
        _rules = rules;
        _fields = fields;
        _sp = sp;
    }

    public int CurrentStep { get; set; } = 1;
    public Guid? JobId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string[] Entities { get; } = ["Customer", "Lead", "Deal", "User"];
    public string[] LoadModes { get; } = ["InsertOnly", "Upsert", "SkipDuplicates", "DryRun"];
    public string SelectedLoadMode { get; set; } = "InsertOnly";
    public bool IsDryRun { get; set; }
    public DataHubAiAnalysisResultDto? AiAnalysis { get; set; }
    public IReadOnlyList<DataHubColumnDetectionDto> ColumnDetections { get; set; } = Array.Empty<DataHubColumnDetectionDto>();
    public List<DataHubMappingDto> Mappings { get; set; } = new();
    public IReadOnlyList<DataHubFieldDefinition> TargetFields { get; set; } = Array.Empty<DataHubFieldDefinition>();
    public IReadOnlyList<DataHubVisualRuleDto> VisualRules { get; set; } = Array.Empty<DataHubVisualRuleDto>();
    public DataHubCleaningSummaryDto? CleaningSummary { get; set; }
    public DataHubJobMetricsDto? Metrics { get; set; }
    public IReadOnlyList<DataHubImportRow> PreviewRows { get; set; } = Array.Empty<DataHubImportRow>();
    public IReadOnlyList<string> PreviewColumns { get; set; } = Array.Empty<string>();
    public DataHubDuplicateScanResultDto? DuplicateScan { get; set; }
    public DataHubImportSummaryDto? ImportSummary { get; set; }
    public Guid TenantId { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? jobId, int step = 1, CancellationToken cancellationToken = default)
    {
        CurrentStep = step;
        JobId = jobId;
        if (!jobId.HasValue) return Page();

        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await LoadJobContextAsync(tenantId, jobId.Value, step, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        int step, Guid? jobId, IFormFile? file, string? targetEntity,
        string? loadMode, bool dryRun,
        List<DataHubMappingDto>? mappings,
        List<DataHubStagingRowUpdateDto>? previewUpdates,
        string? handler, CancellationToken cancellationToken)
    {
        CurrentStep = step;
        SelectedLoadMode = loadMode ?? "InsertOnly";
        IsDryRun = dryRun || SelectedLoadMode == "DryRun";
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

        try
        {
            if (step == 1 && file != null)
            {
                await using var stream = file.OpenReadStream();
                var result = await _orchestrator.UploadAsync(
                    tenantId, userId, stream, file.FileName,
                    targetEntity ?? "Customer", SelectedLoadMode, IsDryRun, cancellationToken);
                return RedirectToPage(new { jobId = result.JobId, step = 2 });
            }

            if (!jobId.HasValue) { Error = "Job not found."; return Page(); }
            JobId = jobId;

            if (step == 2)
            {
                AiAnalysis = await _orchestrator.AnalyzeWithAiAsync(tenantId, jobId.Value, cancellationToken);
                return RedirectToPage(new { jobId, step = 3 });
            }

            if (step == 4 && mappings != null)
            {
                await _orchestrator.SaveMappingsAsync(tenantId, jobId.Value, mappings, cancellationToken);
                return RedirectToPage(new { jobId, step = 5 });
            }

            if (step == 6)
            {
                if (handler == "AutoFix")
                {
                    var fix = await _orchestrator.AutoFixAsync(tenantId, jobId.Value, cancellationToken);
                    Message = $"Auto-fixed {fix.RowsFixed} rows.";
                }
                await _orchestrator.ValidateExtendedAsync(tenantId, jobId.Value, cancellationToken);
                return RedirectToPage(new { jobId, step = 7 });
            }

            if (step == 7)
            {
                if (handler == "SavePreview" && previewUpdates != null && previewUpdates.Count > 0)
                {
                    await _orchestrator.UpdateStagingRowsAsync(tenantId, jobId.Value, previewUpdates, cancellationToken);
                    Message = $"Saved {previewUpdates.Count} row edits.";
                }
                if (handler == "Revalidate" || handler == "SavePreview")
                {
                    await _orchestrator.ValidateExtendedAsync(tenantId, jobId.Value, cancellationToken);
                    if (handler == "Revalidate") Message = "Revalidation complete.";
                }
                return RedirectToPage(new { jobId, step = 7 });
            }

            if (step == 8)
            {
                var job = await _repo.GetJobAsync(tenantId, jobId.Value, cancellationToken);
                if (job != null)
                {
                    job.LoadMode = SelectedLoadMode;
                    job.IsDryRun = IsDryRun;
                    await _repo.UpdateJobAsync(job, cancellationToken);
                }
                await _orchestrator.StartImportAsync(tenantId, jobId.Value, cancellationToken);
                return RedirectToPage(new { jobId, step = 9 });
            }

            if (step == 9 && handler == "Retry")
            {
                await _orchestrator.StartImportAsync(tenantId, jobId.Value, cancellationToken);
                return RedirectToPage(new { jobId, step = 9 });
            }

            await LoadJobContextAsync(tenantId, jobId.Value, step, cancellationToken);
            return Page();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            if (jobId.HasValue) await LoadJobContextAsync(tenantId, jobId.Value, step, cancellationToken);
            return Page();
        }
    }

    private async Task LoadJobContextAsync(Guid tenantId, Guid jobId, int step, CancellationToken ct)
    {
        TenantId = tenantId;
        var job = await _repo.GetJobAsync(tenantId, jobId, ct);
        if (job == null) return;

        SelectedLoadMode = job.LoadMode;
        IsDryRun = job.IsDryRun;

        TargetFields = _fields.GetFields(job.TargetEntity);
        var sample = await _repo.GetRowsAsync(tenantId, jobId, 0, 50, ct);
        ColumnDetections = _intelligence.DetectColumns(job.TargetEntity, job.DetectedColumns, sample.Select(r => r.RawData).ToList());
        AiAnalysis = _intelligence.AnalyzeFile(job.FileName, job.DetectedColumns, sample.Select(r => r.RawData).ToList(), job.TargetEntity);
        VisualRules = await _rules.GetRulesAsync(tenantId, job.TargetEntity, ct);
        CleaningSummary = await _orchestrator.GetCleaningSummaryAsync(tenantId, jobId, ct);
        Metrics = await _orchestrator.GetJobMetricsAsync(tenantId, jobId, ct);

        if (step >= 7)
        {
            PreviewRows = await _repo.GetRowsAsync(tenantId, jobId, 0, Math.Min(job.TotalRows, 100), ct);
            PreviewColumns = job.DetectedColumns.Any()
                ? job.DetectedColumns
                : PreviewRows.SelectMany(r => r.RawData.Keys).Distinct().ToList();
            DuplicateScan = await _orchestrator.ScanDuplicatesAsync(tenantId, jobId, ct);
        }

        if (step >= 10 && job.Status is nameof(DataHubJobStatus.Completed) or nameof(DataHubJobStatus.CompletedWithErrors))
            ImportSummary = await _orchestrator.GetImportSummaryAsync(tenantId, jobId, ct);

        var maps = await _repo.GetMappingsAsync(tenantId, jobId, ct);
        Mappings = maps.Select(m => new DataHubMappingDto(m.Id, m.SourceColumn, m.TargetField, m.IsRequired, m.DefaultValue, m.TransformRule)).ToList();
        if (Mappings.Count == 0)
            Mappings = AiAnalysis.SuggestedMappings.ToList();
    }
}
