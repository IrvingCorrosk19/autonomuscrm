using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class JobModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IServiceProvider _sp;

    public JobModel(IDataHubRepository repo, IDataHubOrchestrator orchestrator, IServiceProvider sp)
    {
        _repo = repo;
        _orchestrator = orchestrator;
        _sp = sp;
    }

    public DataHubJobDetailDto? Job { get; private set; }
    public DataHubImportSummaryDto? ImportSummary { get; private set; }
    public Guid TenantId { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Job = await LoadJobAsync(TenantId, id, cancellationToken);
        if (Job != null && Job.Summary.Status is "Completed" or "CompletedWithErrors" or "RolledBack")
            ImportSummary = await _orchestrator.GetImportSummaryAsync(TenantId, id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostImportAsync(Guid id, CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _orchestrator.StartImportAsync(TenantId, id, cancellationToken);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _orchestrator.CancelJobAsync(TenantId, id, cancellationToken);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRollbackAsync(Guid id, CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _orchestrator.RollbackJobAsync(TenantId, id, cancellationToken: cancellationToken);
        return RedirectToPage(new { id });
    }

    private async Task<DataHubJobDetailDto?> LoadJobAsync(Guid tenantId, Guid jobId, CancellationToken ct)
    {
        var job = await _repo.GetJobAsync(tenantId, jobId, ct);
        if (job == null) return null;

        var mappings = await _repo.GetMappingsAsync(tenantId, jobId, ct);
        var logs = await _repo.GetLogsAsync(tenantId, jobId, 20, ct);
        var errors = await _repo.GetErrorsAsync(tenantId, jobId, 0, 50, ct);
        var preview = await _repo.GetRowsAsync(tenantId, jobId, 0, DataHubConstants.MaxPreviewRows, ct);

        return new DataHubJobDetailDto(
            AutonomusCRM.Infrastructure.DataHub.DataHubOrchestrator.ToSummary(job),
            job.DetectedColumns,
            mappings.Select(m => new DataHubMappingDto(m.Id, m.SourceColumn, m.TargetField, m.IsRequired, m.DefaultValue, m.TransformRule)).ToList(),
            logs.Select(l => new DataHubLogDto(l.CreatedAt, l.Level, l.Message)).ToList(),
            errors.Select(e => new DataHubErrorDto(e.RowNumber, e.ErrorCode, e.Message, e.FieldName, e.IsRetryable)).ToList(),
            preview.Select(r => new DataHubRowPreviewDto(r.RowNumber, r.Status, r.TransformedData.Count > 0 ? r.TransformedData : r.RawData)).ToList());
    }
}
