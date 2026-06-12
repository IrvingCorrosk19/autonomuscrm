using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.University;

[Authorize]
public class ExamModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public ExamModel(IWebHostEnvironment env) => _env = env;

    public UniversityCertification? Cert { get; private set; }
    public IReadOnlyList<UniversityQuestion> Questions { get; private set; } = [];

    public void OnGet(string id)
    {
        var catalog = UniversityCatalog.Load(_env);
        Cert = catalog.Certifications.FirstOrDefault(c => c.Id == id);
        if (Cert != null && catalog.Exams.TryGetValue(id, out var questions))
            Questions = questions;
    }
}
