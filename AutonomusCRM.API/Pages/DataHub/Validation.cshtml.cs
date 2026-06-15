using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class ValidationModel : PageModel
{
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IServiceProvider _sp;

    public ValidationModel(IDataHubOrchestrator orchestrator, IServiceProvider sp)
    {
        _orchestrator = orchestrator;
        _sp = sp;
    }

    public Guid? JobId { get; set; }
    public DataHubValidationResultDto? Result { get; set; }

    public void OnGet(Guid? jobId) => JobId = jobId;

    public async Task<IActionResult> OnPostAsync(Guid jobId, CancellationToken cancellationToken)
    {
        JobId = jobId;
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Result = await _orchestrator.ValidateAsync(tenantId, jobId, cancellationToken);
        return Page();
    }
}
