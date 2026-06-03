using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class MemoryModel : PageModel
{
    private readonly ISemanticMemoryService _semantic;
    private readonly IProductionEmbeddingProvider _embeddings;
    private readonly IServiceProvider _sp;

    public MemoryModel(ISemanticMemoryService semantic, IProductionEmbeddingProvider embeddings, IServiceProvider sp)
    {
        _semantic = semantic;
        _embeddings = embeddings;
        _sp = sp;
    }

    public SemanticMemoryDashboardDto? Dashboard { get; set; }
    public IReadOnlyList<MemoryTimelineItemDto> Timeline { get; set; } = Array.Empty<MemoryTimelineItemDto>();
    public ProductionEmbeddingStatus? EmbeddingStatus { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;

        Dashboard = await _semantic.GetDashboardAsync(tenantId);
        Timeline = await _semantic.GetTimelineAsync(tenantId, 25);
        EmbeddingStatus = _embeddings.GetStatus();
    }
}
