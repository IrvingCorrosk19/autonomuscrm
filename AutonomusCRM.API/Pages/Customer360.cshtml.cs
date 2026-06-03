using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class Customer360Model : PageModel
{
    private readonly ICustomer360Service _c360;
    private readonly IIdentityResolutionService _identity;
    private readonly IServiceProvider _sp;

    public Customer360Model(ICustomer360Service c360, IIdentityResolutionService identity, IServiceProvider sp)
    {
        _c360 = c360;
        _identity = identity;
        _sp = sp;
    }

    public IReadOnlyList<Customer360Dto> Results { get; set; } = Array.Empty<Customer360Dto>();
    public IReadOnlyList<IdentityDuplicateGroupDto> Duplicates { get; set; } = Array.Empty<IdentityDuplicateGroupDto>();
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp);
        Results = await _c360.SearchAsync(tenantId, Q, 25);
        Duplicates = await _identity.FindDuplicatesByEmailAsync(tenantId);
    }
}
