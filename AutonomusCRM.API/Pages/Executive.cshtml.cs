using System.Text;
using AutonomusCRM.Application.Executive;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class ExecutiveModel : PageModel
{
    private readonly IExecutiveOsService _executiveOs;
    private readonly IServiceProvider _sp;

    public ExecutiveModel(IExecutiveOsService executiveOs, IServiceProvider sp)
    {
        _executiveOs = executiveOs;
        _sp = sp;
    }

    public ExecutiveOsDashboardDto? Os { get; set; }

    [BindProperty(SupportsGet = true)]
    public string QbrPeriod { get; set; } = "quarterly";

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return;
        Os = await _executiveOs.GetDashboardAsync(tenantId);
    }

    public async Task<IActionResult> OnGetExportAsync(string type)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return BadRequest();

        var exportType = string.Equals(type, "board", StringComparison.OrdinalIgnoreCase) ? "board" : "executive";
        var html = await _executiveOs.BuildExportHtmlAsync(tenantId, exportType, HttpContext.RequestAborted);
        var fileName = exportType == "board" ? "board-summary.html" : "executive-summary.html";
        return File(Encoding.UTF8.GetBytes(html), "text/html", fileName);
    }
}
