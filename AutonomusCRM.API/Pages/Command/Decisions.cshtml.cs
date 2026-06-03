using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.Command;

public class DecisionsModel : PageModel
{
    private readonly IAiCommandCenterService _commandCenter;
    private readonly IServiceProvider _sp;

    public DecisionsModel(IAiCommandCenterService commandCenter, IServiceProvider sp)
    {
        _commandCenter = commandCenter;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Agent { get; set; }
    [BindProperty(SupportsGet = true)] public int? MinScore { get; set; }

    public IReadOnlyList<DecisionHistoryRow> Rows { get; set; } = Array.Empty<DecisionHistoryRow>();

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Rows = await _commandCenter.GetDecisionHistoryAsync(tenantId, Status, Agent, MinScore);
    }
}
