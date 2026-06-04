using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

[AllowAnonymous]
public class LandingModel : PageModel
{
    public void OnGet() { }
}
