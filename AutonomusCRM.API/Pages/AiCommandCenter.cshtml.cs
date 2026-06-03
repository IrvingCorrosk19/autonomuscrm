using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class AiCommandCenterModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");
}
