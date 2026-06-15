using System.Globalization;
using System.Text.RegularExpressions;
using AutonomusCRM.Application.DataHub;

namespace AutonomusCRM.Infrastructure.DataHub;

/// <summary>Confidence Engine V2 — context, patterns, data types, and sample validation.</summary>
public static class DataHubSmartMatchingEngine
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRx = new(@"^\+?[\d\s\-().]{7,20}$", RegexOptions.Compiled);
    private static readonly Regex AmountRx = new(@"^[\d.,$€£\s-]+$", RegexOptions.Compiled);

    private static readonly Dictionary<string, FieldMatchProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Email"] = new("Email", "Email",
            ["email", "correo", "mail", "e-mail", "emailaddress", "email address",
                "business email", "corporate email", "client email", "primary email",
                "work email", "contact email", "company email", "account email"],
            s => EmailRx.IsMatch(s)),
        ["Phone"] = new("Phone", "Phone",
            ["phone", "telefono", "tel", "mobile", "movil", "cell", "cellular", "celular",
                "telefono movil", "whatsapp", "whatsapp number", "contact phone", "work phone", "mobile phone"],
            s => PhoneRx.IsMatch(s) && s.Count(char.IsDigit) >= 7),
        ["Company"] = new("Company", "Company",
            ["company", "empresa", "organization", "organisation", "business name",
                "org name", "account name", "firm", "employer", "company name"],
            _ => false),
        ["Name"] = new("Name", "Name",
            ["name", "nombre", "fullname", "full name", "contact name", "customer name", "lead name"],
            _ => false),
        ["FirstName"] = new("FirstName", "Name",
            ["firstname", "first name", "given name", "nombre"],
            _ => false),
        ["LastName"] = new("LastName", "Name",
            ["lastname", "last name", "surname", "apellido", "family name"],
            _ => false),
        ["Amount"] = new("Amount", "Amount",
            ["amount", "monto", "value", "revenue", "deal value", "dealvalue", "price", "total"],
            s => AmountRx.IsMatch(s) && decimal.TryParse(NormalizeAmount(s), NumberStyles.Any, CultureInfo.InvariantCulture, out _)),
        ["Date"] = new("Date", null,
            ["date", "fecha", "fecha de cierre", "cierre", "created", "closed", "close date", "created date"],
            s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)),
        ["Title"] = new("Title", "Title",
            ["title", "subject", "opportunity name", "deal name", "opportunity title"],
            _ => false),
        ["Source"] = new("Source", "Source",
            ["source", "origen", "leadsource", "lead source", "origin"],
            _ => false),
        ["Pipeline"] = new("Pipeline", "Stage",
            ["pipeline", "stage", "etapa", "fase", "deal stage"],
            _ => false),
        ["Owner"] = new("Owner", "AssignedToUserId",
            ["owner", "assigned", "responsable", "agent", "assigned to", "owner id"],
            _ => false),
        ["City"] = new("City", "City",
            ["city", "ciudad", "town", "locality"],
            _ => false),
        ["Country"] = new("Country", "Country",
            ["country", "pais", "país", "nacion", "nation"],
            _ => false)
    };

    public static DataHubSmartMatchResult MatchColumn(
        string targetEntity,
        string sourceColumn,
        IReadOnlyList<string?> samples)
    {
        var normalizedHeader = NormalizeHeader(sourceColumn);
        var tokens = Tokenize(sourceColumn);
        var fieldDefs = DataHubFieldCatalogImpl.Instance.GetFields(targetEntity);
        var validFields = fieldDefs.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = new List<(string Type, string? Field, double Score, List<string> Reasons)>();

        foreach (var (type, profile) in Profiles)
        {
            var reasons = new List<string>();
            var headerScore = ScoreHeaderMatch(normalizedHeader, tokens, profile, reasons);
            var sampleScore = ScoreSamples(samples, profile, reasons);
            var field = profile.TargetField;
            if (field != null && !validFields.Contains(field))
                field = MapTypeToFieldFallback(type, validFields);
            if (field == null)
                field = MapTypeToFieldFallback(type, validFields);

            var score = headerScore * 0.55 + sampleScore * 0.45;
            if (score < 5) continue;
            candidates.Add((type, field, score, reasons));
        }

        if (candidates.Count == 0)
        {
            return new DataHubSmartMatchResult(
                sourceColumn, null, "Text", 40,
                "No strong header or sample signals — treated as generic text.");
        }

        var best = candidates.OrderByDescending(c => c.Score).First();
        var confidence = Math.Clamp(best.Score, 40, 100);
        var explanation = BuildExplanation(sourceColumn, best.Type, best.Field, confidence, best.Reasons);
        return new DataHubSmartMatchResult(sourceColumn, best.Field, best.Type, Math.Round(confidence, 1), explanation);
    }

    public static IReadOnlyList<DataHubSmartMatchResult> MatchColumns(
        string targetEntity,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> sampleRows)
    {
        var results = new List<DataHubSmartMatchResult>();
        foreach (var col in columns)
        {
            var samples = sampleRows.Take(20)
                .Select(r => r.TryGetValue(col, out var v) ? v : null)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Take(8)
                .ToList();
            results.Add(MatchColumn(targetEntity, col, samples));
        }
        return results;
    }

    private static double ScoreHeaderMatch(string normalized, IReadOnlyList<string> tokens, FieldMatchProfile profile, List<string> reasons)
    {
        if (IsNegativeMatch(normalized, profile.Type))
            return 0;

        double best = 0;
        foreach (var synonym in profile.Synonyms)
        {
            var synNorm = NormalizeHeader(synonym);
            if (normalized == synNorm)
            {
                best = Math.Max(best, 98);
                reasons.Add($"Exact header match to synonym '{synonym}'");
                continue;
            }

            if (normalized.Contains(synNorm, StringComparison.OrdinalIgnoreCase) ||
                synNorm.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            {
                best = Math.Max(best, synNorm.Length > 6 ? 94 : 88);
                reasons.Add($"Header contains synonym '{synonym}'");
            }

            var synTokens = Tokenize(synonym);
            var overlap = tokens.Intersect(synTokens, StringComparer.OrdinalIgnoreCase).Count();
            if (overlap > 0 && synTokens.Count > 0)
            {
                var ratio = (double)overlap / synTokens.Count;
                if (ratio >= 0.5)
                {
                    best = Math.Max(best, 70 + ratio * 20);
                    reasons.Add($"Token overlap with '{synonym}' ({overlap}/{synTokens.Count})");
                }
            }
        }
        return best;
    }

    private static double ScoreSamples(IReadOnlyList<string?> samples, FieldMatchProfile profile, List<string> reasons)
    {
        if (samples.Count == 0 || profile.SampleValidator == null) return 50;
        var nonEmpty = samples.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
        if (nonEmpty.Count == 0) return 50;

        var hits = nonEmpty.Count(s => profile.SampleValidator!(s));
        var rate = hits / (double)nonEmpty.Count;
        if (rate >= 0.8)
        {
            reasons.Add($"{hits}/{nonEmpty.Count} samples match {profile.Type} pattern");
            return 95;
        }
        if (rate >= 0.5)
        {
            reasons.Add($"{hits}/{nonEmpty.Count} samples partially match {profile.Type} pattern");
            return 70;
        }
        return 45;
    }

    private static string BuildExplanation(string column, string type, string? field, double confidence, IReadOnlyList<string> reasons)
    {
        var parts = reasons.Count > 0 ? string.Join("; ", reasons.Distinct()) : "Heuristic header analysis";
        var target = field != null ? $" → mapped to '{field}'" : string.Empty;
        return $"Confidence Engine V2: column '{column}' detected as {type}{target} ({confidence:F0}%). {parts}.";
    }

    private static string? MapTypeToFieldFallback(string type, HashSet<string> validFields)
    {
        var mapped = type switch
        {
            "Email" => "Email",
            "Phone" => "Phone",
            "Company" => "Company",
            "Name" or "FirstName" or "LastName" => "Name",
            "Amount" => "Amount",
            "Title" => "Title",
            "Source" => "Source",
            "Pipeline" => "Stage",
            "Owner" => "AssignedToUserId",
            "Date" => validFields.FirstOrDefault(f =>
                f.Contains("Date", StringComparison.OrdinalIgnoreCase) ||
                f.Equals("ExpectedCloseDate", StringComparison.OrdinalIgnoreCase)),
            "City" => "City",
            "Country" => "Country",
            _ => null
        };
        return mapped != null && validFields.Contains(mapped) ? mapped : null;
    }

    private static bool IsNegativeMatch(string normalized, string type)
    {
        if (type == "Company" && (normalized.Contains("account id") || normalized.Contains("account owner") || normalized.Contains("account number")))
            return true;
        if (type == "Title" && normalized.Contains("stage"))
            return true;
        if (type == "Name" && (normalized.Contains("email") || normalized.Contains("phone")))
            return true;
        if (type == "Email" && normalized.Contains("name") && !normalized.Contains("email"))
            return true;
        return false;
    }

    private static string NormalizeHeader(string s)
    {
        var normalized = s.ToLowerInvariant().Replace("_", " ").Replace("-", " ").Trim();
        return RemoveDiacritics(normalized);
    }

    private static string RemoveDiacritics(string text)
    {
        var formD = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static List<string> Tokenize(string s)
    {
        var norm = NormalizeHeader(s);
        var split = Regex.Split(norm, @"[\s_\-]+").Where(t => t.Length > 1).ToList();
        var camel = Regex.Matches(s, @"[A-Z][a-z]+|[a-z]+")
            .Select(m => m.Value.ToLowerInvariant())
            .Where(t => t.Length > 1);
        return split.Concat(camel).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string NormalizeAmount(string s)
        => s.Replace("$", "").Replace("€", "").Replace("£", "").Replace(",", "").Trim();

    private sealed record FieldMatchProfile(
        string Type,
        string? TargetField,
        string[] Synonyms,
        Func<string, bool>? SampleValidator);
}
