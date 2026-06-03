using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.Trust;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.ViewComponents;

public sealed class FlowNavBadgesViewComponent : ViewComponent
{
    private readonly ITrustMetricsService _trustMetrics;
    private readonly ITenantContext _tenant;

    public FlowNavBadgesViewComponent(ITrustMetricsService trustMetrics, ITenantContext tenant)
    {
        _trustMetrics = trustMetrics;
        _tenant = tenant;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var pending = 0;
        if (_tenant.TenantId is Guid tenantId)
        {
            try
            {
                var metrics = await _trustMetrics.GetMetricsAsync(tenantId);
                pending = metrics.PendingApprovals;
            }
            catch
            {
                pending = 0;
            }
        }

        return View(pending);
    }
}
