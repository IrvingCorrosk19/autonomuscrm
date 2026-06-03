using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.Command;

public class PlaybooksModel : PageModel
{
    private readonly IAiCommandCenterService _commandCenter;
    private readonly IServiceProvider _sp;

    public PlaybooksModel(IAiCommandCenterService commandCenter, IServiceProvider sp)
    {
        _commandCenter = commandCenter;
        _sp = sp;
    }

    public IReadOnlyList<PlaybookStateRow> Playbooks { get; set; } = Array.Empty<PlaybookStateRow>();

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Playbooks = await _commandCenter.GetPlaybooksAsync(tenantId);
    }
}
