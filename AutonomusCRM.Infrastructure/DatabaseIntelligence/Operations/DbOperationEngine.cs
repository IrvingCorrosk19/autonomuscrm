using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Operations;

public sealed class DbOperationEngine : IDbOperationEngine
{
    public DbOperationExecutionResult ApplyPreview(DbOperationActionPlan plan, IReadOnlyList<DbOperationRowContext> rows)
    {
        var working = rows.Select(CloneRow).ToList();
        var result = new DbOperationExecutionResult { Rows = working };

        if (plan.Filter && plan.FilterRules.Count > 0)
            result.Filtered += ApplyFilter(plan.FilterRules, working);

        if (plan.Exclude && plan.ExcludeRules.Count > 0)
            result.Excluded += ApplyExclude(plan.ExcludeRules, working);

        if (plan.Clean && plan.CleanRules.Count > 0)
            result.Corrected += ApplyClean(plan.CleanRules, working);

        if (plan.Transform && plan.TransformRules.Count > 0)
            result.Transformed += ApplyTransform(plan.TransformRules, working);

        if (plan.Enrich && plan.EnrichRules.Count > 0)
            ApplyEnrich(plan.EnrichRules, working);

        if (plan.Merge && plan.MergeRules.Count > 0)
            result.Merged += ApplyMerge(plan.MergeRules, working);

        result.Rows = working;
        return result;
    }

    public DbOperationPreviewResultDto BuildPreview(
        Guid jobId, DbOperationActionPlan plan, IReadOnlyList<DbOperationRowContext> rows, int sampleSize = 25)
    {
        var before = rows.Select(CloneRow).ToList();
        var afterResult = ApplyPreview(plan, before);
        var samples = new List<DbOperationRowPreview>();

        foreach (var row in afterResult.Rows.Take(sampleSize))
        {
            var original = rows.FirstOrDefault(r => r.RowNumber == row.RowNumber);
            if (original == null) continue;

            var changed = !PayloadsEqual(original.Data, row.Data) ||
                          original.Status != row.Status;
            if (!changed && row.Status == DbOperationRowStatus.Active) continue;

            samples.Add(new DbOperationRowPreview(
                row.RowNumber,
                row.EntityType,
                original.Data,
                row.Data,
                DescribeImpact(original, row),
                row.Status is DbOperationRowStatus.Excluded or DbOperationRowStatus.Filtered));
        }

        return new DbOperationPreviewResultDto(
            jobId,
            rows.Count,
            afterResult.Corrected + afterResult.Transformed + afterResult.Merged,
            afterResult.Excluded + afterResult.Filtered,
            afterResult.Merged,
            samples);
    }

    internal static int ApplyFilter(IReadOnlyList<DbOperationFilterRule> rules, List<DbOperationRowContext> rows)
    {
        var count = 0;
        foreach (var row in rows.Where(r => r.Status == DbOperationRowStatus.Active))
        {
            if (!rules.All(r => MatchesFilter(row, r))) continue;
            row.Status = DbOperationRowStatus.Filtered;
            count++;
        }
        return count;
    }

    internal static int ApplyExclude(IReadOnlyList<DbOperationExcludeRule> rules, List<DbOperationRowContext> rows)
    {
        var count = 0;
        foreach (var row in rows.Where(r => r.Status == DbOperationRowStatus.Active))
        {
            foreach (var rule in rules)
            {
                if (rule.Field == null)
                {
                    row.Status = DbOperationRowStatus.Excluded;
                    row.ExclusionReason = rule.Reason;
                    count++;
                    break;
                }

                if (MatchesFilter(row, new DbOperationFilterRule(rule.Field, rule.Operator ?? DbOperationFilterOperator.IsEmpty, rule.Value)))
                {
                    row.Status = DbOperationRowStatus.Excluded;
                    row.ExclusionReason = rule.Reason;
                    count++;
                    break;
                }
            }
        }
        return count;
    }

    internal static int ApplyClean(IReadOnlyList<DbOperationCleanRule> rules, List<DbOperationRowContext> rows)
    {
        var corrected = 0;
        foreach (var row in rows.Where(r => r.Status == DbOperationRowStatus.Active))
        {
            foreach (var rule in rules)
            {
                var key = FindKey(row.Data, rule.Field);
                if (key == null || !row.Data.TryGetValue(key, out var val) || val == null) continue;
                var cleaned = CleanValue(val, rule.Action);
                if (cleaned != val)
                {
                    row.Data[key] = cleaned;
                    corrected++;
                }
            }
        }
        return corrected;
    }

    internal static int ApplyTransform(IReadOnlyList<DbOperationTransformRule> rules, List<DbOperationRowContext> rows)
    {
        var transformed = 0;
        foreach (var row in rows.Where(r => r.Status == DbOperationRowStatus.Active))
        {
            foreach (var rule in rules)
            {
                if (ApplySingleTransform(row, rule))
                    transformed++;
            }
        }
        return transformed;
    }

    internal static void ApplyEnrich(IReadOnlyList<DbOperationEnrichRule> rules, List<DbOperationRowContext> rows)
    {
        foreach (var row in rows.Where(r => r.Status == DbOperationRowStatus.Active))
        {
            foreach (var rule in rules)
            {
                var key = FindKey(row.Data, rule.Field) ?? rule.Field;
                if (!row.Data.ContainsKey(key) || string.IsNullOrWhiteSpace(row.Data[key]))
                    row.Data[key] = rule.Value;
            }
        }
    }

    internal static int ApplyMerge(IReadOnlyList<DbOperationMergeRule> rules, List<DbOperationRowContext> rows)
    {
        var merged = 0;
        foreach (var rule in rules)
        {
            var groups = rows
                .Where(r => r.Status == DbOperationRowStatus.Active && r.EntityType == rule.EntityType)
                .GroupBy(r => NormalizeMatchKey(GetFieldValue(r, rule.MatchField)))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);

            foreach (var group in groups)
            {
                var keeper = rule.Strategy switch
                {
                    DbOperationMergeStrategy.KeepNewest => group.OrderByDescending(r => r.SourceModifiedAtUtc ?? DateTime.MinValue).First(),
                    DbOperationMergeStrategy.KeepOldest => group.OrderBy(r => r.SourceModifiedAtUtc ?? DateTime.MaxValue).First(),
                    _ => group.OrderBy(r => r.RowNumber).First()
                };

                foreach (var dup in group.Where(r => r.RowNumber != keeper.RowNumber))
                {
                    dup.Status = DbOperationRowStatus.Merged;
                    dup.ExclusionReason = $"Merged into row {keeper.RowNumber}";
                    merged++;
                }
            }
        }
        return merged;
    }

    private static bool ApplySingleTransform(DbOperationRowContext row, DbOperationTransformRule rule)
    {
        return rule.TransformType switch
        {
            DbOperationTransformType.SplitFullName => SplitFullName(row, rule),
            DbOperationTransformType.CombineColumns => CombineColumns(row, rule),
            DbOperationTransformType.RenameField => RenameField(row, rule),
            DbOperationTransformType.MapCategory => MapCategory(row, rule),
            _ => false
        };
    }

    private static bool SplitFullName(DbOperationRowContext row, DbOperationTransformRule rule)
    {
        var sourceKey = FindKey(row.Data, rule.SourceField);
        if (sourceKey == null || !row.Data.TryGetValue(sourceKey, out var full) || string.IsNullOrWhiteSpace(full))
            return false;

        var parts = full.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstKey = FindKey(row.Data, rule.TargetField ?? "first_name") ?? "first_name";
        var lastKey = FindKey(row.Data, rule.SecondField ?? "last_name") ?? "last_name";
        row.Data[firstKey] = parts[0];
        row.Data[lastKey] = parts.Length > 1 ? parts[1] : string.Empty;
        return true;
    }

    private static bool CombineColumns(DbOperationRowContext row, DbOperationTransformRule rule)
    {
        var sep = rule.Separator ?? " ";
        var first = GetFieldValue(row, rule.SourceField);
        var second = rule.SecondField != null ? GetFieldValue(row, rule.SecondField) : null;
        if (string.IsNullOrWhiteSpace(first)) return false;
        var combined = second != null ? $"{first}{sep}{second}" : first;
        var target = FindKey(row.Data, rule.TargetField ?? rule.SourceField) ?? rule.TargetField ?? rule.SourceField;
        row.Data[target] = combined.Trim();
        return true;
    }

    private static bool RenameField(DbOperationRowContext row, DbOperationTransformRule rule)
    {
        var sourceKey = FindKey(row.Data, rule.SourceField);
        if (sourceKey == null || rule.TargetField == null) return false;
        if (!row.Data.TryGetValue(sourceKey, out var val)) return false;
        row.Data.Remove(sourceKey);
        row.Data[rule.TargetField] = val;
        return true;
    }

    private static bool MapCategory(DbOperationRowContext row, DbOperationTransformRule rule)
    {
        if (rule.CategoryMap == null || rule.CategoryMap.Count == 0) return false;
        var sourceKey = FindKey(row.Data, rule.SourceField);
        if (sourceKey == null || !row.Data.TryGetValue(sourceKey, out var val) || val == null) return false;
        var targetKey = FindKey(row.Data, rule.TargetField ?? "category") ?? rule.TargetField ?? "category";
        row.Data[targetKey] = rule.CategoryMap.GetValueOrDefault(val, val);
        return true;
    }

    internal static bool MatchesFilter(DbOperationRowContext row, DbOperationFilterRule rule)
    {
        var value = GetFieldValue(row, rule.Field);
        return rule.Operator switch
        {
            DbOperationFilterOperator.Equals => string.Equals(value, rule.Value, StringComparison.OrdinalIgnoreCase),
            DbOperationFilterOperator.Contains => value != null && rule.Value != null &&
                value.Contains(rule.Value, StringComparison.OrdinalIgnoreCase),
            DbOperationFilterOperator.IsEmpty => string.IsNullOrWhiteSpace(value),
            DbOperationFilterOperator.GreaterThan => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) &&
                decimal.TryParse(rule.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) && n > t,
            DbOperationFilterOperator.Between => TryParseDate(value, out var d) &&
                TryParseDate(rule.Value, out var from) && TryParseDate(rule.ValueTo, out var to) &&
                d >= from && d <= to,
            _ => true
        };
    }

    internal static string? CleanValue(string value, string action) => action switch
    {
        DbOperationCleanAction.Trim => value.Trim(),
        DbOperationCleanAction.Lowercase => value.ToLowerInvariant(),
        DbOperationCleanAction.Uppercase => value.ToUpperInvariant(),
        DbOperationCleanAction.RemoveSpaces => Regex.Replace(value, @"\s+", ""),
        DbOperationCleanAction.NormalizeEmail => value.Trim().ToLowerInvariant(),
        DbOperationCleanAction.NormalizePhone => Regex.Replace(value, @"[^\d+]", ""),
        _ => value
    };

    private static string? GetFieldValue(DbOperationRowContext row, string field)
    {
        var key = FindKey(row.Data, field);
        return key != null && row.Data.TryGetValue(key, out var val) ? val : null;
    }

    private static string? FindKey(Dictionary<string, string?> data, string field)
    {
        if (data.ContainsKey(field)) return field;
        return data.Keys.FirstOrDefault(k => k.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeMatchKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static bool TryParseDate(string? value, out DateTime dt)
    {
        dt = default;
        return !string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt);
    }

    private static DbOperationRowContext CloneRow(DbOperationRowContext row) => new()
    {
        RowNumber = row.RowNumber,
        EntityType = row.EntityType,
        SchemaName = row.SchemaName,
        TableName = row.TableName,
        Data = row.Data.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
        Status = row.Status,
        ExclusionReason = row.ExclusionReason,
        SourceModifiedAtUtc = row.SourceModifiedAtUtc
    };

    private static bool PayloadsEqual(Dictionary<string, string?> a, Dictionary<string, string?> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var other)) return false;
            if (!string.Equals(kv.Value, other, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static string DescribeImpact(DbOperationRowContext before, DbOperationRowContext after)
    {
        if (after.Status == DbOperationRowStatus.Excluded) return after.ExclusionReason ?? "Excluded";
        if (after.Status == DbOperationRowStatus.Filtered) return "Filtered out by rule";
        if (after.Status == DbOperationRowStatus.Merged) return after.ExclusionReason ?? "Merged duplicate";
        return "Fields updated";
    }
}
