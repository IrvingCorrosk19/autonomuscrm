using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class DuplicatesModel : PageModel
{
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IDataHubRepository _repo;
    private readonly IServiceProvider _sp;

    public DuplicatesModel(IDataHubOrchestrator orchestrator, IDataHubRepository repo, IServiceProvider sp)
    {
        _orchestrator = orchestrator;
        _repo = repo;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? JobId { get; set; }

    public IReadOnlyList<DataHubJobSummaryDto> Jobs { get; private set; } = Array.Empty<DataHubJobSummaryDto>();
    public DataHubDuplicateScanResultDto? Scan { get; private set; }
    public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var jobs = await _repo.ListJobsAsync(tenantId, 50, cancellationToken);
        Jobs = jobs.Select(DataHubOrchestrator.ToSummary).ToList();

        if (JobId.HasValue)
            Scan = await _orchestrator.ScanDuplicatesAsync(tenantId, JobId.Value, cancellationToken);
    }

    public async Task<IActionResult> OnPostApplyPolicyAsync(CancellationToken cancellationToken)
    {
        if (!JobId.HasValue) return RedirectToPage();
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var job = await _repo.GetJobAsync(tenantId, JobId.Value, cancellationToken);
        if (job == null) return RedirectToPage();

        await _orchestrator.ValidateExtendedAsync(tenantId, JobId.Value, cancellationToken);
        var scan = await _orchestrator.ScanDuplicatesAsync(tenantId, JobId.Value, cancellationToken);
        Message = $"Duplicate policy applied. {scan.TotalDuplicateRows} duplicate rows in {scan.TotalGroups} groups.";
        return RedirectToPage(new { jobId = JobId });
    }
}
