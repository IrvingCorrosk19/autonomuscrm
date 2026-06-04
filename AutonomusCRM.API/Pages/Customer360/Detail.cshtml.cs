using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.Customer360;

public class DetailModel : PageModel
{
    private readonly ICustomer360EnterpriseService _enterprise;
    private readonly IDecisionIntelligenceEngine _decisionIntel;
    private readonly IAbosOutcomeLearningService _learning;
    private readonly IServiceProvider _sp;

    public DetailModel(
        ICustomer360EnterpriseService enterprise,
        IDecisionIntelligenceEngine decisionIntel,
        IAbosOutcomeLearningService learning,
        IServiceProvider sp)
    {
        _enterprise = enterprise;
        _decisionIntel = decisionIntel;
        _learning = learning;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public Customer360EnterpriseDto? View { get; set; }
    public DecisionIntelligenceResultDto? Explainability { get; set; }
    public CustomerActionLearningDto? ActionLearning { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id is not Guid customerId) return RedirectToPage("/Customer360");
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return Page();
        var ct = HttpContext.RequestAborted;
        var viewTask = _enterprise.GetEnterpriseViewAsync(tenantId, customerId, ct);
        var explainTask = SafeExplainabilityAsync(tenantId, customerId, ct);
        var learnTask = SafeLearningAsync(tenantId, customerId, ct);
        await Task.WhenAll(viewTask, explainTask, learnTask);

        View = await viewTask;
        if (View == null) return RedirectToPage("/Customer360");
        Explainability = await explainTask;
        ActionLearning = await learnTask;

        return Page();
    }

    private async Task<DecisionIntelligenceResultDto?> SafeExplainabilityAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        try
        {
            return await _decisionIntel.AnalyzeCustomerDecisionAsync(tenantId, customerId, "Customer360", ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task<CustomerActionLearningDto?> SafeLearningAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        try
        {
            return await _learning.GetCustomerLearningAsync(tenantId, customerId, ct);
        }
        catch
        {
            return null;
        }
    }
}
