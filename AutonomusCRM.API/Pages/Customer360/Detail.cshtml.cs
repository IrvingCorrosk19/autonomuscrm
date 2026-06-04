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
        View = await _enterprise.GetEnterpriseViewAsync(tenantId, customerId);
        if (View == null) return RedirectToPage("/Customer360");
        try
        {
            Explainability = await _decisionIntel.AnalyzeCustomerDecisionAsync(tenantId, customerId, "Customer360", HttpContext.RequestAborted);
        }
        catch
        {
            Explainability = null;
        }

        try
        {
            ActionLearning = await _learning.GetCustomerLearningAsync(tenantId, customerId, HttpContext.RequestAborted);
        }
        catch
        {
            ActionLearning = null;
        }

        return Page();
    }
}
