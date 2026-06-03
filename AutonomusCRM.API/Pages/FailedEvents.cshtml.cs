using AutonomusCRM.Application.Events;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class FailedEventsModel : PageModel
{
    private readonly IFailedEventReplayService _replay;
    private readonly IServiceProvider _sp;

    public FailedEventsModel(IFailedEventReplayService replay, IServiceProvider sp)
    {
        _replay = replay;
        _sp = sp;
    }

    public IReadOnlyList<FailedEventListItemDto> Items { get; set; } = Array.Empty<FailedEventListItemDto>();
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Items = await _replay.ListAsync(tenantId, 100);
    }

    public async Task<IActionResult> OnPostReplayAsync(Guid id)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return RedirectToPage();
        if (await _replay.MarkReplayRequestedAsync(tenantId, id))
            Message = "Replay solicitado (operador debe ejecutar consumer manual o job).";
        return RedirectToPage();
    }
}
