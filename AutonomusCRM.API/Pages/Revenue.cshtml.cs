using AutonomusCRM.Application.Revenue;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class RevenueModel : PageModel
{
    private readonly IRevenueOsService _revenueOs;
    private readonly IServiceProvider _sp;

    public RevenueModel(IRevenueOsService revenueOs, IServiceProvider sp)
    {
        _revenueOs = revenueOs;
        _sp = sp;
    }

    public RevenueOsDashboardDto? Dashboard { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Dashboard = await _revenueOs.GetDashboardAsync(tenantId);
    }
}
