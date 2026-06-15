using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

public static class OperatePlanBuilder
{
    public static DbOperationActionPlan Build(OperateFormInput form)
    {
        var filterRules = form.EnableFilter && !string.IsNullOrWhiteSpace(form.FilterField)
            ? [new DbOperationFilterRule(
                form.FilterField.Trim(),
                string.IsNullOrWhiteSpace(form.FilterOperator) ? DbOperationFilterOperator.Equals : form.FilterOperator.Trim(),
                form.FilterValue,
                form.FilterValueTo)]
            : Array.Empty<DbOperationFilterRule>();

        var cleanRules = form.EnableClean && !string.IsNullOrWhiteSpace(form.CleanField)
            ? [new DbOperationCleanRule(
                form.CleanField.Trim(),
                string.IsNullOrWhiteSpace(form.CleanAction) ? DbOperationCleanAction.Trim : form.CleanAction.Trim())]
            : Array.Empty<DbOperationCleanRule>();

        var mergeRules = form.EnableMerge && !string.IsNullOrWhiteSpace(form.MergeMatchField)
            ? [new DbOperationMergeRule(
                ParseEntityType(form.MergeEntityType),
                form.MergeMatchField.Trim(),
                string.IsNullOrWhiteSpace(form.MergeStrategy) ? DbOperationMergeStrategy.KeepNewest : form.MergeStrategy.Trim())]
            : Array.Empty<DbOperationMergeRule>();

        var enrichRules = form.EnableEnrich && !string.IsNullOrWhiteSpace(form.EnrichField)
            ? [new DbOperationEnrichRule(form.EnrichField.Trim(), form.EnrichValue ?? string.Empty)]
            : Array.Empty<DbOperationEnrichRule>();

        var excludeRules = form.EnableExclude
            ? [new DbOperationExcludeRule(
                string.IsNullOrWhiteSpace(form.ExcludeReason) ? "Excluded by rule" : form.ExcludeReason.Trim(),
                string.IsNullOrWhiteSpace(form.ExcludeField) ? null : form.ExcludeField.Trim(),
                string.IsNullOrWhiteSpace(form.ExcludeOperator) ? null : form.ExcludeOperator.Trim(),
                form.ExcludeValue)]
            : Array.Empty<DbOperationExcludeRule>();

        var transformRules = BuildTransformRules(form);

        return new DbOperationActionPlan(
            form.EnableFilter && filterRules.Length > 0,
            form.EnableClean && cleanRules.Length > 0,
            form.EnableMerge && mergeRules.Length > 0,
            form.EnableEnrich && enrichRules.Length > 0,
            form.EnableExclude && excludeRules.Length > 0,
            form.EnableTransform && transformRules.Count > 0,
            Sync: false,
            form.EnableImport,
            filterRules,
            cleanRules,
            mergeRules,
            enrichRules,
            excludeRules,
            transformRules);
    }

    public static void ApplyToForm(OperateFormInput form, DbOperationActionPlan plan)
    {
        form.EnableFilter = plan.Filter;
        form.EnableClean = plan.Clean;
        form.EnableMerge = plan.Merge;
        form.EnableEnrich = plan.Enrich;
        form.EnableExclude = plan.Exclude;
        form.EnableTransform = plan.Transform;
        form.EnableImport = plan.Import;

        if (plan.FilterRules.FirstOrDefault() is { } f)
        {
            form.FilterField = f.Field;
            form.FilterOperator = f.Operator;
            form.FilterValue = f.Value;
            form.FilterValueTo = f.ValueTo;
        }

        if (plan.CleanRules.FirstOrDefault() is { } c)
        {
            form.CleanField = c.Field;
            form.CleanAction = c.Action;
        }

        if (plan.MergeRules.FirstOrDefault() is { } m)
        {
            form.MergeEntityType = m.EntityType.ToString();
            form.MergeMatchField = m.MatchField;
            form.MergeStrategy = m.Strategy;
        }

        if (plan.EnrichRules.FirstOrDefault() is { } e)
        {
            form.EnrichField = e.Field;
            form.EnrichValue = e.Value;
        }

        if (plan.ExcludeRules.FirstOrDefault() is { } x)
        {
            form.ExcludeReason = x.Reason;
            form.ExcludeField = x.Field;
            form.ExcludeOperator = x.Operator;
            form.ExcludeValue = x.Value;
        }

        if (plan.TransformRules.FirstOrDefault() is { } t)
        {
            form.TransformType = t.TransformType;
            form.TransformSourceField = t.SourceField;
            form.TransformTargetField = t.TargetField;
            form.TransformSecondField = t.SecondField;
            form.TransformSeparator = t.Separator;
            if (t.CategoryMap?.Count > 0)
            {
                var first = t.CategoryMap.First();
                form.TransformMapFrom = first.Key;
                form.TransformMapTo = first.Value;
            }
        }
    }

    private static IReadOnlyList<DbOperationTransformRule> BuildTransformRules(OperateFormInput form)
    {
        if (!form.EnableTransform || string.IsNullOrWhiteSpace(form.TransformType) ||
            string.IsNullOrWhiteSpace(form.TransformSourceField))
            return [];

        var type = form.TransformType.Trim();
        if (type == DbOperationTransformType.MapCategory &&
            !string.IsNullOrWhiteSpace(form.TransformMapFrom) &&
            !string.IsNullOrWhiteSpace(form.TransformMapTo))
        {
            return
            [
                new DbOperationTransformRule(
                    type,
                    form.TransformSourceField.Trim(),
                    string.IsNullOrWhiteSpace(form.TransformTargetField) ? "category" : form.TransformTargetField.Trim(),
                    CategoryMap: new Dictionary<string, string>
                    {
                        [form.TransformMapFrom.Trim()] = form.TransformMapTo.Trim()
                    })
            ];
        }

        return
        [
            new DbOperationTransformRule(
                type,
                form.TransformSourceField.Trim(),
                form.TransformTargetField,
                form.TransformSecondField,
                form.TransformSeparator)
        ];
    }

    private static BusinessEntityType ParseEntityType(string? value) =>
        Enum.TryParse<BusinessEntityType>(value, true, out var parsed) ? parsed : BusinessEntityType.Customer;
}
