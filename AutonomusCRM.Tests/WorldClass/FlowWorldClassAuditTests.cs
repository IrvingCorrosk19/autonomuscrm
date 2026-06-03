namespace AutonomusCRM.Tests.WorldClass;

public class FlowWorldClassAuditTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Theory]
    [InlineData("AutonomusCRM.API/wwwroot/css/flow-worldclass.css")]
    [InlineData("AutonomusCRM.API/wwwroot/js/flow-worldclass.js")]
    [InlineData("AutonomusCRM.API/Controllers/FlowSearchController.cs")]
    [InlineData("AutonomusCRM.API/Pages/Shared/Flow/_FlowDrawer.cshtml")]
    [InlineData("AutonomusCRM.API/Pages/Shared/Flow/_FlowRelationshipGraph.cshtml")]
    [InlineData("AutonomusCRM.API/Pages/Flow/Components.cshtml")]
    public void World_class_assets_exist(string relativePath)
    {
        var path = Path.Combine(Root, relativePath);
        Assert.True(File.Exists(path), $"Missing: {relativePath}");
    }
}
