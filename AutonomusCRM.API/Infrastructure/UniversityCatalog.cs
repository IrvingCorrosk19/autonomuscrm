using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutonomusCRM.API.Infrastructure;

public sealed class UniversityCatalog
{
    public string Version { get; set; } = "1.0";
    public string QaUrl { get; set; } = "";
    public List<UniversityPath> Paths { get; set; } = [];
    public List<UniversityBadge> Badges { get; set; } = [];
    public List<UniversityCertification> Certifications { get; set; } = [];
    public Dictionary<string, List<UniversityQuestion>> Exams { get; set; } = new();
    public Dictionary<string, UniversityLesson> Lessons { get; set; } = new();
    public List<UniversityPlaybook> Playbooks { get; set; } = [];

    public static UniversityCatalog Load(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "data", "university-catalog.json");
        if (!File.Exists(path))
            return new UniversityCatalog();

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<UniversityCatalog>(json, options) ?? new UniversityCatalog();
    }
}

public sealed class UniversityPath
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Badge { get; set; } = "";
    public List<UniversityUnit> Units { get; set; } = [];
}

public sealed class UniversityUnit
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Module { get; set; }
    public int Mins { get; set; }
    public int Points { get; set; }
}

public sealed class UniversityBadge
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Points { get; set; }
}

public sealed class UniversityCertification
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Path { get; set; } = "";
    public int MinScore { get; set; }
    public List<string> PathsRequired { get; set; } = [];
}

public sealed class UniversityLesson
{
    public string Id { get; set; } = "";
    public string PathId { get; set; } = "";
    public string Title { get; set; } = "";
    public int DurationMins { get; set; }
    public int Points { get; set; }
    public string? Route { get; set; }
    public string Content { get; set; } = "";
}

public sealed class UniversityQuestion
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = [];
    public int Correct { get; set; }
    public string Explanation { get; set; } = "";
    public string Type { get; set; } = "multiple_choice";
}

public sealed class UniversityPlaybook
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public List<string> Steps { get; set; } = [];
}
