using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class TransformationRulesModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IServiceProvider _sp;

    public TransformationRulesModel(IDataHubRepository repo, IServiceProvider sp)
    {
        _repo = repo;
        _sp = sp;
    }

    public IReadOnlyList<DataHubTransformationRule> Rules { get; private set; } = Array.Empty<DataHubTransformationRule>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Rules = await _repo.GetTransformationRulesAsync(tenantId, "Customer", cancellationToken);
    }
}
