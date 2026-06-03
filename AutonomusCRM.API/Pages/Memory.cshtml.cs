using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class MemoryModel : PageModel
{
    private readonly ISemanticMemoryService _semantic;
    private readonly IServiceProvider _sp;

    public MemoryModel(ISemanticMemoryService semantic, IServiceProvider sp)
    {
        _semantic = semantic;
        _sp = sp;
    }

    public SemanticMemoryDashboardDto? Dashboard { get; set; }
    public IReadOnlyList<MemoryTimelineItemDto> Timeline { get; set; } = Array.Empty<MemoryTimelineItemDto>();

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;

        Dashboard = await _semantic.GetDashboardAsync(tenantId);
        Timeline = await _semantic.GetTimelineAsync(tenantId, 25);
    }
}
