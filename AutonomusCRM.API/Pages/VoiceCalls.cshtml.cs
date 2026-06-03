using AutonomusCRM.Application.Voice;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class VoiceCallsModel : PageModel
{
    private readonly IVoiceCallService _voice;
    private readonly IServiceProvider _sp;

    public VoiceCallsModel(IVoiceCallService voice, IServiceProvider sp)
    {
        _voice = voice;
        _sp = sp;
    }

    public IReadOnlyList<VoiceCallLogDto> Calls { get; set; } = Array.Empty<VoiceCallLogDto>();
    [BindProperty] public string PhoneNumber { get; set; } = "";
    [BindProperty] public string Direction { get; set; } = "outbound";
    [BindProperty] public int DurationSeconds { get; set; }
    [BindProperty] public string Outcome { get; set; } = "connected";
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public Guid? CustomerId { get; set; }
    [BindProperty] public Guid? LeadId { get; set; }
    [BindProperty] public Guid? DealId { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        Calls = await _voice.ListAsync(tenantId);
    }

    public async Task<IActionResult> OnPostLogCallAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        var log = VoiceCallLog.Create(tenantId, PhoneNumber, Direction, DurationSeconds, Outcome,
            CustomerId, LeadId, DealId, notes: Notes, provider: "manual");
        await _voice.LogCallAsync(log);
        return RedirectToPage();
    }
}
