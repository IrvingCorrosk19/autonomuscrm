using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

[AllowAnonymous]
public class DemoModel : PageModel
{
    public void OnGet() { }
}
