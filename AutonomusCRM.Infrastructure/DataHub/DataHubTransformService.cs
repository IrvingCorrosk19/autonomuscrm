using System.Globalization;
using System.Text.RegularExpressions;
using AutonomusCRM.Application.DataHub;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubTransformService : IDataHubTransformService
{
    private static readonly Regex PhoneRegex = new(@"[^\d+]", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Dictionary<string, string?> TransformRow(
        Dictionary<string, string?> raw,
        IReadOnlyList<DataHubImportMapping> mappings,
        IReadOnlyList<DataHubTransformationRule> rules)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in mappings)
        {
            var value = raw.TryGetValue(map.SourceColumn, out var v) ? v : map.DefaultValue;
            if (!string.IsNullOrWhiteSpace(map.TransformRule))
                value = ApplyTransform(value ?? "", map.TransformRule);
            result[map.TargetField] = value;
        }

        foreach (var rule in rules)
        {
            if (!result.TryGetValue(rule.TargetField, out var current) || current is null) continue;
            result[rule.TargetField] = ApplyTransform(current, rule.TransformType, rule.Parameters);
        }

        return result;
    }

    public string ApplyTransform(string value, string transformType, IReadOnlyDictionary<string, string>? parameters = null)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (!Enum.TryParse<DataHubTransformType>(transformType, true, out var type))
            return value;

        return type switch
        {
            DataHubTransformType.Trim => value.Trim(),
            DataHubTransformType.Uppercase => value.ToUpperInvariant(),
            DataHubTransformType.Lowercase => value.ToLowerInvariant(),
            DataHubTransformType.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower()),
            DataHubTransformType.NormalizePhone => NormalizePhone(value),
            DataHubTransformType.NormalizeEmail => value.Trim().ToLowerInvariant(),
            DataHubTransformType.NormalizeDate => NormalizeDate(value),
            DataHubTransformType.NormalizeCurrency => NormalizeDecimal(value),
            DataHubTransformType.ToDecimal => NormalizeDecimal(value),
            DataHubTransformType.ToInt => int.TryParse(value, out var i) ? i.ToString(CultureInfo.InvariantCulture) : value,
            DataHubTransformType.ToBool => bool.TryParse(value, out var b) ? b.ToString().ToLowerInvariant() : (value is "1" or "yes" or "si" or "sí" ? "true" : value is "0" or "no" ? "false" : value),
            DataHubTransformType.ToDate => NormalizeDate(value),
            DataHubTransformType.MapStatus or DataHubTransformType.MapPipelineStage or DataHubTransformType.MapUser
                or DataHubTransformType.MapCompany or DataHubTransformType.MapCountry or DataHubTransformType.ToEnum
                => MapValue(value, parameters),
            _ => value
        };
    }

    private static string NormalizePhone(string value)
    {
        var digits = PhoneRegex.Replace(value, "");
        return digits.StartsWith('+') ? digits : digits.TrimStart('0');
    }

    private static string NormalizeDate(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out var dt))
            return dt.ToUniversalTime().ToString("O");
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt))
            return dt.ToUniversalTime().ToString("O");
        return value;
    }

    private static string NormalizeDecimal(string value)
    {
        var cleaned = value.Replace("$", "").Replace("€", "").Trim();
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out var d))
            return d.ToString(CultureInfo.InvariantCulture);
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            return d.ToString(CultureInfo.InvariantCulture);
        return value;
    }

    private static string MapValue(string value, IReadOnlyDictionary<string, string>? parameters)
    {
        if (parameters == null) return value;
        foreach (var kv in parameters)
        {
            if (string.Equals(kv.Key, value, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return value;
    }

    public static bool IsValidEmail(string? email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
}
