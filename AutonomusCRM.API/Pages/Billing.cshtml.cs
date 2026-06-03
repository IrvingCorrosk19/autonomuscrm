using AutonomusCRM.Application.Billing;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class BillingModel : PageModel
{
    private readonly IBillingDashboardService _billing;
    private readonly IServiceProvider _sp;

    public BillingModel(IBillingDashboardService billing, IServiceProvider sp)
    {
        _billing = billing;
        _sp = sp;
    }

    public BillingDashboardDto? Dashboard { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Dashboard = await _billing.GetDashboardAsync(tenantId);
    }
}
