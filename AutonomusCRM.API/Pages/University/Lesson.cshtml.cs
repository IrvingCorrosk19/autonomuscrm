using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace AutonomusCRM.API.Pages.University;

[Authorize]
public class LessonModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public LessonModel(IWebHostEnvironment env) => _env = env;

    public UniversityLesson? Lesson { get; private set; }
    public string RenderedContent { get; private set; } = "";

    public IActionResult OnGet(string id)
    {
        var catalog = UniversityCatalog.Load(_env);
        if (!catalog.Lessons.TryGetValue(id, out var lesson))
            return Page();

        Lesson = lesson;
        RenderedContent = Regex.Replace(
            lesson.Content.Replace("\n", "<br/>"),
            @"\*\*(.+?)\*\*",
            "<strong>$1</strong>");
        return Page();
    }
}
