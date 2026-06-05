namespace AutonomusCRM.API.Pages.Shared;

public sealed class CrmPaginationModel
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }
    public int ShownCount { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public string RecordsFormat { get; init; } = "{0} of {1} records";
    public string PageFormat { get; init; } = "Page {0} of {1}";
    public string AriaLabel { get; init; } = "Pagination";
    public string PrevLabel { get; init; } = "Previous";
    public string NextLabel { get; init; } = "Next";
    public Func<int, string> BuildUrl { get; init; } = _ => "#";
}
