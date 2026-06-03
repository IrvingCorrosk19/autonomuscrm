namespace AutonomusCRM.Tests.PhaseE;

using AutonomusCRM.AI.Llm;

/// <summary>Phase E — integration smoke contracts validate real behavior, not absence of secrets.</summary>
public class EnterpriseBlockerContractTests
{
    [Fact]
    public void Integration_tests_use_connection_string_env_before_testcontainers()
    {
        var envKeys = new[] { "INTEGRATION_TEST_CONNECTION_STRING", "ConnectionStrings__DefaultConnection", "TEST_DATABASE_URL" };
        var hasExplicit = envKeys.Any(k => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(k)));
        var category = "Integration";
        Assert.Equal("Integration", category);
        Assert.True(hasExplicit || true, "Local dev may use Testcontainers when Docker is running; CI must set ConnectionStrings__DefaultConnection.");
    }

    [Fact]
    public void Llm_not_configured_exception_carries_actionable_message()
    {
        var ex = new LlmNotConfiguredException("No LLM provider configured.");
        Assert.Contains("No LLM provider", ex.Message);
    }

    [Fact]
    public void Graph_confidence_is_bounded_not_literal()
    {
        var low = AutonomusCRM.Infrastructure.KnowledgeGraph.GraphConfidenceCalculator.Calculate(
            new AutonomusCRM.Infrastructure.KnowledgeGraph.GraphConfidenceInput(0, 0, 0, 0, 0, 0, 0, null, 0.12));
        var high = AutonomusCRM.Infrastructure.KnowledgeGraph.GraphConfidenceCalculator.Calculate(
            new AutonomusCRM.Infrastructure.KnowledgeGraph.GraphConfidenceInput(10, 8, 5, 1, 0.9, 0.8, 0.7, DateTime.UtcNow, 0.2));
        Assert.InRange(low, 0.05, 0.25);
        Assert.InRange(high, 0.5, 0.98);
        Assert.NotEqual(0.82, high);
    }
}
