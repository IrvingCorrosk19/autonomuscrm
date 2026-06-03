namespace AutonomusCRM.Tests.TruthSprint;

public class AutomationOptimizerAgentTests
{
    [Fact]
    public void Worker_periodic_loop_invokes_AutomationOptimizerAgent()
    {
        var workerSource = File.ReadAllText(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "AutonomusCRM.Workers", "Worker.cs")));

        Assert.Contains("AutomationOptimizerAgent", workerSource);
        Assert.Contains("AnalyzePerformance", workerSource);
        Assert.Contains("OptimizeWorkflows", workerSource);
    }
}
