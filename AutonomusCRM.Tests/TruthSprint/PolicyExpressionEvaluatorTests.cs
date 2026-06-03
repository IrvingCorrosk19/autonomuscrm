using AutonomusCRM.Infrastructure.Policies;

namespace AutonomusCRM.Tests.TruthSprint;

public class PolicyExpressionEvaluatorTests
{
    [Fact]
    public void Allow_passes()
    {
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("allow", new Dictionary<string, object>());
        Assert.True(outcome.IsAllowed);
        Assert.False(outcome.RequiresApproval);
    }

    [Fact]
    public void Deny_blocks()
    {
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("deny", new Dictionary<string, object>());
        Assert.False(outcome.IsAllowed);
    }

    [Fact]
    public void RequireApproval_blocks_until_approved()
    {
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("requireApproval", new Dictionary<string, object>());
        Assert.False(outcome.IsAllowed);
        Assert.True(outcome.RequiresApproval);
    }

    [Fact]
    public void MaxRiskScore_violates_when_exceeded()
    {
        var ctx = new Dictionary<string, object> { ["riskScore"] = 85 };
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("maxRiskScore:70", ctx);
        Assert.False(outcome.IsAllowed);
        Assert.Contains(outcome.Violations, v => v.Contains("riskScore"));
    }

    [Fact]
    public void MinConfidence_violates_when_below_threshold()
    {
        var ctx = new Dictionary<string, object> { ["confidence"] = 0.4 };
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("minConfidence:0.7", ctx);
        Assert.False(outcome.IsAllowed);
    }

    [Fact]
    public void TenantKillSwitch_blocks_when_active()
    {
        var ctx = new Dictionary<string, object> { ["tenantKillSwitch"] = true };
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("tenantKillSwitch", ctx);
        Assert.False(outcome.IsAllowed);
    }

    [Fact]
    public void HumanInTheLoopRequired_blocks_without_flag()
    {
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("humanInTheLoopRequired", new Dictionary<string, object>());
        Assert.False(outcome.IsAllowed);
    }

    [Fact]
    public void HumanInTheLoopRequired_passes_with_flag()
    {
        var ctx = new Dictionary<string, object> { ["humanInTheLoop"] = true };
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("humanInTheLoopRequired", ctx);
        Assert.True(outcome.IsAllowed);
    }

    [Fact]
    public void Action_rule_enforces_match()
    {
        var ctx = new Dictionary<string, object> { ["action"] = "sendEmail" };
        var outcome = PolicyExpressionEvaluator.EvaluateExpression("action:deleteCustomer", ctx);
        Assert.False(outcome.IsAllowed);
    }
}
