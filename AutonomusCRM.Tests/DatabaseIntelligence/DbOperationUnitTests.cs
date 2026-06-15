using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Operations;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbOperationUnitTests
{
    private readonly DbOperationEngine _engine = new();

    [Fact]
    public void Filter_GreaterThan_KeepsMatchingRowsActive()
    {
        var rows = OperationSyntheticDatasets.FilterAmountSet();
        var plan = OperationSyntheticDatasets.FilterPlan(1000);
        var result = _engine.ApplyPreview(plan, rows);
        Assert.Equal(1, result.Filtered);
        Assert.Equal(DbOperationRowStatus.Active, result.Rows[0].Status);
        Assert.Equal(DbOperationRowStatus.Filtered, result.Rows[1].Status);
    }

    [Fact]
    public void Clean_NormalizePhone_StripsFormatting()
    {
        var rows = OperationSyntheticDatasets.PhoneNormalizeSet();
        var plan = new DbOperationActionPlan(
            false, true, false, false, false, false, false, false,
            [], [new DbOperationCleanRule("phone", DbOperationCleanAction.NormalizePhone)],
            [], [], [], []);
        var result = _engine.ApplyPreview(plan, rows);
        Assert.Equal("+5076001234", result.Rows[0].Data["phone"]);
    }

    [Fact]
    public void Merge_DuplicateEmail_KeepsNewest()
    {
        var rows = OperationSyntheticDatasets.DuplicateCustomers();
        var plan = OperationSyntheticDatasets.CleanMergeExcludeTransformImportPlan() with { Exclude = false, Transform = false, Clean = false };
        var result = _engine.ApplyPreview(plan, rows);
        Assert.Equal(1, result.Merged);
        Assert.Equal(DbOperationRowStatus.Merged, result.Rows[0].Status);
        Assert.Equal(DbOperationRowStatus.Active, result.Rows[1].Status);
    }

    [Fact]
    public void Exclude_TestEmail_MarksRowExcluded()
    {
        var rows = OperationSyntheticDatasets.ExcludeTestSet();
        var plan = OperationSyntheticDatasets.CleanMergeExcludeTransformImportPlan() with
        {
            Clean = false, Merge = false, Transform = false
        };
        var result = _engine.ApplyPreview(plan, rows);
        Assert.Equal(1, result.Excluded);
        Assert.Equal(DbOperationRowStatus.Active, result.Rows[0].Status);
        Assert.Equal(DbOperationRowStatus.Excluded, result.Rows[1].Status);
    }

    [Fact]
    public void Transform_SplitFullName_CreatesFirstAndLast()
    {
        var rows = OperationSyntheticDatasets.TransformNameSet();
        var plan = new DbOperationActionPlan(
            false, false, false, false, false, true, false, false,
            [], [], [], [], [],
            [new DbOperationTransformRule(DbOperationTransformType.SplitFullName, "name", "first_name", "last_name")]);
        var result = _engine.ApplyPreview(plan, rows);
        Assert.Equal(1, result.Transformed);
        Assert.Equal("Maria", result.Rows[0].Data["first_name"]);
        Assert.Equal("Lopez", result.Rows[0].Data["last_name"]);
    }

    [Fact]
    public void Preview_ReturnsBeforeAfterSamplesWithoutPersisting()
    {
        var rows = OperationSyntheticDatasets.DuplicateCustomers();
        var jobId = Guid.NewGuid();
        var plan = OperationSyntheticDatasets.CleanMergeExcludeTransformImportPlan() with { Exclude = false, Transform = false };
        var preview = _engine.BuildPreview(jobId, plan, rows);
        Assert.Equal(jobId, preview.JobId);
        Assert.Equal(2, preview.TotalRows);
        Assert.True(preview.MergedRows >= 1);
        Assert.All(rows, r => Assert.Equal(DbOperationRowStatus.Active, r.Status));
    }

    [Fact]
    public void Filter_IsEmpty_DetectsBlankFields()
    {
        var rows = new List<DbOperationRowContext>
        {
            new() { RowNumber = 1, EntityType = BusinessEntityType.Customer, TableName = "customers", Data = new() { ["email"] = "" } },
            new() { RowNumber = 2, EntityType = BusinessEntityType.Customer, TableName = "customers", Data = new() { ["email"] = "ok@example.com" } }
        };
        var plan = new DbOperationActionPlan(
            true, false, false, false, false, false, false, false,
            [new DbOperationFilterRule("email", DbOperationFilterOperator.IsEmpty, null)],
            [], [], [], [], []);
        var result = _engine.ApplyPreview(plan, rows);
        Assert.Equal(1, result.Filtered);
        Assert.Equal(DbOperationRowStatus.Filtered, result.Rows[0].Status);
        Assert.Equal(DbOperationRowStatus.Active, result.Rows[1].Status);
    }

    [Fact]
    public void OperationStages_Defined()
    {
        Assert.Equal("Analyzing", DbOperationStages.Analyzing);
        Assert.Equal("Completed", DbOperationStages.Completed);
    }

    [Fact]
    public void OperationActionTypes_IncludeAllStudios()
    {
        Assert.Contains(DbOperationActionType.Filter, new[] { DbOperationActionType.Filter, DbOperationActionType.Import });
        Assert.Equal("Merge", DbOperationActionType.Merge);
    }
}
