using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class JobsModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IServiceProvider _sp;

    public JobsModel(IDataHubRepository repo, IServiceProvider sp)
    {
        _repo = repo;
        _sp = sp;
    }

    public IReadOnlyList<DataHubJobSummaryDto> Jobs { get; private set; } = Array.Empty<DataHubJobSummaryDto>();
    public Guid TenantId { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var jobs = await _repo.ListJobsAsync(TenantId, 100, cancellationToken);
        Jobs = jobs.Select(DataHubOrchestrator.ToSummary).ToList();
    }
}
