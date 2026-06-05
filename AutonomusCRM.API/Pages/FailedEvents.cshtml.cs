using AutonomusCRM.Application.Events;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages;

public class FailedEventsModel : PageModel
{
    private readonly IFailedEventReplayService _replay;
    private readonly IServiceProvider _sp;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public FailedEventsModel(IFailedEventReplayService replay, IServiceProvider sp, IStringLocalizer<SharedResource> localizer)
    {
        _replay = replay;
        _sp = sp;
        _localizer = localizer;
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
            Message = _localizer["Ops_FailedEvents_ReplayRequested", id].Value;
        return RedirectToPage();
    }
}
