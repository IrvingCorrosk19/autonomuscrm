using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class AgentsModel : PageModel
{
    private readonly IAiCommandCenterService _commandCenter;
    private readonly IServiceProvider _serviceProvider;

    public AgentsModel(IAiCommandCenterService commandCenter, IServiceProvider serviceProvider)
    {
        _commandCenter = commandCenter;
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyList<WorkforceAgentDto> Workforce { get; set; } = Array.Empty<WorkforceAgentDto>();
    public IReadOnlyList<CommandCenterDecisionRow> RecentDecisions { get; set; } = Array.Empty<CommandCenterDecisionRow>();
    public bool HasData { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_serviceProvider);
        if (tenantId == Guid.Empty) return;

        var flow = await _commandCenter.GetFlowCommandAsync(tenantId);
        Workforce = flow.Workforce;
        RecentDecisions = flow.Dashboard.RecentDecisions;
        HasData = flow.HasData || Workforce.Any(w => w.Actions7d > 0);
    }
}
