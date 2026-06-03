using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class RevenueModel : PageModel
{
    private readonly IRevenueOsService _revenueOs;
    private readonly IGraphReasoningEngine _reasoning;
    private readonly IServiceProvider _sp;

    public RevenueModel(IRevenueOsService revenueOs, IGraphReasoningEngine reasoning, IServiceProvider sp)
    {
        _revenueOs = revenueOs;
        _reasoning = reasoning;
        _sp = sp;
    }

    public RevenueOsDashboardDto? Dashboard { get; set; }
    public GraphReasoningResultDto? RevenueExplanation { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Dashboard = await _revenueOs.GetDashboardAsync(tenantId);
        try
        {
            RevenueExplanation = await _reasoning.DetectRevenueLeakAsync(tenantId, HttpContext.RequestAborted);
        }
        catch
        {
            RevenueExplanation = null;
        }
    }
}
