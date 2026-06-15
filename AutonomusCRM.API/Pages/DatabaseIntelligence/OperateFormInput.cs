namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

public sealed class OperateFormInput
{
    public bool EnableFilter { get; set; }
    public bool EnableClean { get; set; }
    public bool EnableMerge { get; set; }
    public bool EnableEnrich { get; set; }
    public bool EnableExclude { get; set; }
    public bool EnableTransform { get; set; }
    public bool EnableImport { get; set; }

    public string? FilterField { get; set; }
    public string? FilterOperator { get; set; }
    public string? FilterValue { get; set; }
    public string? FilterValueTo { get; set; }

    public string? CleanField { get; set; }
    public string? CleanAction { get; set; }

    public string? MergeEntityType { get; set; }
    public string? MergeMatchField { get; set; }
    public string? MergeStrategy { get; set; }

    public string? EnrichField { get; set; }
    public string? EnrichValue { get; set; }

    public string? ExcludeReason { get; set; }
    public string? ExcludeField { get; set; }
    public string? ExcludeOperator { get; set; }
    public string? ExcludeValue { get; set; }

    public string? TransformType { get; set; }
    public string? TransformSourceField { get; set; }
    public string? TransformTargetField { get; set; }
    public string? TransformSecondField { get; set; }
    public string? TransformSeparator { get; set; }
    public string? TransformMapFrom { get; set; }
    public string? TransformMapTo { get; set; }
}
