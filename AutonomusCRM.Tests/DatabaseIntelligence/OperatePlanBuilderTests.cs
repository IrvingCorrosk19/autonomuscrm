using AutonomusCRM.API.Pages.DatabaseIntelligence;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class OperatePlanBuilderTests
{
    [Fact]
    public void Build_WithAllStudiosEnabled_ProducesFullPlan()
    {
        var form = new OperateFormInput
        {
            EnableFilter = true,
            FilterField = "order_total",
            FilterOperator = DbOperationFilterOperator.GreaterThan,
            FilterValue = "1000",
            EnableClean = true,
            CleanField = "phone",
            CleanAction = DbOperationCleanAction.NormalizePhone,
            EnableMerge = true,
            MergeEntityType = "Customer",
            MergeMatchField = "email",
            MergeStrategy = DbOperationMergeStrategy.KeepNewest,
            EnableEnrich = true,
            EnrichField = "segment",
            EnrichValue = "Enterprise",
            EnableExclude = true,
            ExcludeReason = "Test record",
            ExcludeField = "email",
            ExcludeOperator = DbOperationFilterOperator.Contains,
            ExcludeValue = "test+",
            EnableTransform = true,
            TransformType = DbOperationTransformType.SplitFullName,
            TransformSourceField = "name",
            TransformTargetField = "first_name",
            TransformSecondField = "last_name",
            EnableImport = true
        };

        var plan = OperatePlanBuilder.Build(form);

        Assert.True(plan.Filter);
        Assert.True(plan.Clean);
        Assert.True(plan.Merge);
        Assert.True(plan.Enrich);
        Assert.True(plan.Exclude);
        Assert.True(plan.Transform);
        Assert.True(plan.Import);
        Assert.Single(plan.FilterRules);
        Assert.Equal(DbOperationFilterOperator.GreaterThan, plan.FilterRules[0].Operator);
        Assert.Single(plan.CleanRules);
        Assert.Equal(BusinessEntityType.Customer, plan.MergeRules[0].EntityType);
        Assert.Equal("Enterprise", plan.EnrichRules[0].Value);
        Assert.Equal("test+", plan.ExcludeRules[0].Value);
        Assert.Equal(DbOperationTransformType.SplitFullName, plan.TransformRules[0].TransformType);
    }

    [Fact]
    public void Build_WithDisabledStudios_ProducesEmptyPlan()
    {
        var plan = OperatePlanBuilder.Build(new OperateFormInput());
        Assert.False(plan.Filter);
        Assert.False(plan.Clean);
        Assert.False(plan.Merge);
        Assert.False(plan.Enrich);
        Assert.False(plan.Exclude);
        Assert.False(plan.Transform);
        Assert.False(plan.Import);
        Assert.Empty(plan.FilterRules);
    }

    [Fact]
    public void ApplyToForm_RestoresSavedPlan()
    {
        var saved = new DbOperationActionPlan(
            true, true, false, true, true, false, false, false,
            [new DbOperationFilterRule("email", DbOperationFilterOperator.Contains, "acme")],
            [new DbOperationCleanRule("email", DbOperationCleanAction.NormalizeEmail)],
            [], [new DbOperationEnrichRule("owner", "Sales")],
            [new DbOperationExcludeRule("Invalid", "phone", DbOperationFilterOperator.IsEmpty, null)],
            []);

        var form = new OperateFormInput();
        OperatePlanBuilder.ApplyToForm(form, saved);

        Assert.True(form.EnableFilter);
        Assert.Equal("email", form.FilterField);
        Assert.Equal(DbOperationFilterOperator.Contains, form.FilterOperator);
        Assert.True(form.EnableClean);
        Assert.True(form.EnableEnrich);
        Assert.Equal("Sales", form.EnrichValue);
        Assert.True(form.EnableExclude);
        Assert.Equal(DbOperationFilterOperator.IsEmpty, form.ExcludeOperator);
    }
}
