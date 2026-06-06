using System.Security.Claims;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class CustomerSuccessModel : PageModel
{
    private readonly ICustomerSuccessOsService _csOs;
    private readonly ICustomerRepository _customers;
    private readonly IAbosOutcomeLearningService _learning;
    private readonly IServiceProvider _sp;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CustomerSuccessModel(
        ICustomerSuccessOsService csOs,
        ICustomerRepository customers,
        IAbosOutcomeLearningService learning,
        IServiceProvider sp,
        IStringLocalizer<SharedResource> localizer)
    {
        _csOs = csOs;
        _customers = customers;
        _learning = learning;
        _sp = sp;
        _localizer = localizer;
    }

    public CustomerSuccessHomeDto? Home { get; set; }
    public List<CustomerListItem> Customers { get; set; } = new();
    public string? Message { get; set; }
    public string? Error { get; set; }

    [BindProperty] public Guid? NewTicketCustomerId { get; set; }
    [BindProperty] public string? NewTicketSubject { get; set; }
    [BindProperty] public string? NewTicketPriority { get; set; } = "Normal";

    public record CustomerListItem(Guid Id, string Name);

    public async Task OnGetAsync()
    {
        var tenantId = await GetTenantIdAsync();
        if (tenantId == Guid.Empty) return;
        Home = await _csOs.GetHomeAsync(tenantId);
        await LoadCustomersAsync(tenantId);
        Message = TempData["CsMessage"] as string;
        Error = TempData["CsError"] as string;
    }

    public async Task<IActionResult> OnPostCreateTicketAsync()
    {
        var tenantId = await GetTenantIdAsync();
        if (!NewTicketCustomerId.HasValue || string.IsNullOrWhiteSpace(NewTicketSubject))
        {
            TempData["CsError"] = _localizer["Cs_Error_RequiredFields"].Value;
            return RedirectToPage();
        }
        await _csOs.CreateTicketAsync(tenantId, NewTicketCustomerId.Value, NewTicketSubject.Trim(), null, NewTicketPriority ?? "Normal", null);
        TempData["CsMessage"] = _localizer["Cs_Message_TicketCreated"].Value;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateCaseAsync(Guid customerId, string caseType, string title, string priority = "High")
    {
        var tenantId = await GetTenantIdAsync();
        await _csOs.CreateCaseAsync(tenantId, customerId, caseType, title, null, priority, null);
        TempData["CsMessage"] = _localizer["Cs_Message_CaseCreated"].Value;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCloseTicketAsync(Guid ticketId)
    {
        var tenantId = await GetTenantIdAsync();
        if (await _csOs.CloseTicketAsync(tenantId, ticketId))
            TempData["CsMessage"] = _localizer["Cs_Message_TicketClosed"].Value;
        else
            TempData["CsError"] = _localizer["Cs_Error_TicketCloseFailed"].Value;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRunPlaybookAsync(Guid customerId, string playbookType)
    {
        var tenantId = await GetTenantIdAsync();
        var result = await _csOs.RunPlaybookAsync(tenantId, customerId, playbookType, null);

        try
        {
            await _learning.RecordActionExecutedAsync(
                tenantId,
                GetUserId(),
                $"Playbook:{playbookType}",
                string.Format(_localizer["Cs_Audit_PlaybookTitle"].Value, playbookType),
                "playbook",
                string.Format(_localizer["Cs_Audit_PlaybookSummary"].Value, playbookType),
                string.Format(_localizer["Cs_Audit_PlaybookOutcome"].Value, result.TasksCreated),
                customerId,
                null,
                HttpContext.RequestAborted);
        }
        catch { /* non-blocking */ }

        TempData["CsMessage"] = string.Format(_localizer["Cs_Message_PlaybookExecuted"].Value, playbookType, result.TasksCreated);
        return RedirectToPage();
    }

    private async Task LoadCustomersAsync(Guid tenantId)
    {
        var list = await _customers.GetByTenantIdAsync(tenantId);
        Customers = list.Take(100).Select(c => new CustomerListItem(c.Id, c.Name)).ToList();
    }

    private Task<Guid> GetTenantIdAsync() => this.GetTenantIdForPageAsync(_sp);

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
