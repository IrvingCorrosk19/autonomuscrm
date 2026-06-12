using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.University;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public IndexModel(IWebHostEnvironment env) => _env = env;

    public UniversityCatalog Catalog { get; private set; } = new();

    public void OnGet() => Catalog = UniversityCatalog.Load(_env);
}
