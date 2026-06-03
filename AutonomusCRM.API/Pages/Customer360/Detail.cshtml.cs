using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.Customer360;

public class DetailModel : PageModel
{
    private readonly ICustomer360EnterpriseService _enterprise;
    private readonly IServiceProvider _sp;

    public DetailModel(ICustomer360EnterpriseService enterprise, IServiceProvider sp)
    {
        _enterprise = enterprise;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public Customer360EnterpriseDto? View { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id is not Guid customerId) return RedirectToPage("/Customer360");
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        if (tenantId == Guid.Empty) return Page();
        View = await _enterprise.GetEnterpriseViewAsync(tenantId, customerId);
        if (View == null) return RedirectToPage("/Customer360");
        return Page();
    }
}
