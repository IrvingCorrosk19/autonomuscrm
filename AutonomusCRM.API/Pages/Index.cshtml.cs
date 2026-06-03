using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class IndexModel : PageModel
{
    private readonly IAiCommandCenterService _commandCenter;
    private readonly IServiceProvider _serviceProvider;

    public IndexModel(IAiCommandCenterService commandCenter, IServiceProvider serviceProvider)
    {
        _commandCenter = commandCenter;
        _serviceProvider = serviceProvider;
    }

    public FlowCommandViewDto? Flow { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Period { get; set; } = 7;

    public async Task<IActionResult> OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_serviceProvider);
        if (tenantId == Guid.Empty)
            return Page();

        Period = Period is 7 or 30 ? Period : 7;
        Flow = await _commandCenter.GetFlowCommandAsync(tenantId, Period);
        return Page();
    }
}
