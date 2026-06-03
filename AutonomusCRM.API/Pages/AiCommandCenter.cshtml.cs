using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class AiCommandCenterModel : PageModel
{
    private readonly IAiCommandCenterService _commandCenter;
    private readonly IServiceProvider _sp;

    public AiCommandCenterModel(IAiCommandCenterService commandCenter, IServiceProvider sp)
    {
        _commandCenter = commandCenter;
        _sp = sp;
    }

    public AiCommandCenterDto? Dashboard { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        Dashboard = await _commandCenter.GetDashboardAsync(tenantId);
    }
}
