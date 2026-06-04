using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class SupportModel : PageModel
{
    public IActionResult OnGet() => Redirect("/customer-success");
}

