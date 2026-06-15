using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class ErrorsModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IServiceProvider _sp;

    public ErrorsModel(IDataHubRepository repo, IDataHubOrchestrator orchestrator, IServiceProvider sp)
    {
        _repo = repo;
        _orchestrator = orchestrator;
        _sp = sp;
    }

    public Guid? JobId { get; set; }
    public Guid TenantId { get; set; }
    public IReadOnlyList<DataHubErrorDto> Errors { get; private set; } = Array.Empty<DataHubErrorDto>();

    public async Task OnGetAsync(Guid? jobId, CancellationToken cancellationToken)
    {
        JobId = jobId;
        if (!jobId.HasValue) return;
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var raw = await _repo.GetErrorsAsync(TenantId, jobId.Value, 0, 200, cancellationToken);
        Errors = raw.Select(e => new DataHubErrorDto(e.RowNumber, e.ErrorCode, e.Message, e.FieldName, e.IsRetryable)).ToList();
    }

    public async Task<IActionResult> OnPostAsync(Guid jobId, CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _orchestrator.RetryFailedRowsAsync(TenantId, jobId, cancellationToken);
        return RedirectToPage("/DataHub/Job", new { id = jobId });
    }
}
