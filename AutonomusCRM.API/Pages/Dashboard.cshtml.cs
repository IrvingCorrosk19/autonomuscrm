using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class DashboardModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");
}

