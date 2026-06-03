using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.Command;

public class OutcomesModel : PageModel
{
    private readonly IAiCommandCenterService _commandCenter;
    private readonly IServiceProvider _sp;

    public OutcomesModel(IAiCommandCenterService commandCenter, IServiceProvider sp)
    {
        _commandCenter = commandCenter;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)] public int Period { get; set; } = 30;

    public FlowOutcomesSummaryDto? Summary { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Period = Period is 7 or 30 ? Period : 30;
        Summary = await _commandCenter.GetOutcomesSummaryAsync(tenantId, Period);
    }
}
