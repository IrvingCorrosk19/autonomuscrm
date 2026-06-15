using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class RollbackModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IServiceProvider _sp;

    public RollbackModel(IDataHubRepository repo, IServiceProvider sp)
    {
        _repo = repo;
        _sp = sp;
    }

    public IReadOnlyList<DataHubImportJob> RollbackJobs { get; private set; } = Array.Empty<DataHubImportJob>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var jobs = await _repo.ListJobsAsync(tenantId, 100, cancellationToken);
        RollbackJobs = jobs.Where(j => j.RollbackAvailable).ToList();
    }
}
