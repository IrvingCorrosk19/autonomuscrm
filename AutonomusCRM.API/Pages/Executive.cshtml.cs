using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class ExecutiveModel : PageModel
{
    private readonly IExecutiveAiDashboardService _executive;
    private readonly IRevenueOsService _revenueOs;
    private readonly IServiceProvider _sp;

    public ExecutiveModel(IExecutiveAiDashboardService executive, IRevenueOsService revenueOs, IServiceProvider sp)
    {
        _executive = executive;
        _revenueOs = revenueOs;
        _sp = sp;
    }

    public ExecutiveAiDashboardDto? AiDashboard { get; set; }
    public RevenueOsDashboardDto? Revenue { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        AiDashboard = await _executive.GetDashboardAsync(tenantId);
        Revenue = await _revenueOs.GetDashboardAsync(tenantId);
    }
}
