using AutonomusCRM.AI;
using AutonomusCRM.AI.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AutonomusCRM.Tests.TruthSprint;

public class LlmSmokeServiceTests
{
    [Fact]
    public void GetHealth_lists_all_providers()
    {
        var openAi = new Mock<ILlmProviderImplementation>();
        openAi.Setup(p => p.ProviderId).Returns("openai");
        openAi.Setup(p => p.IsConfigured).Returns(false);

        var usage = new Mock<ILlmUsageTracker>();
        usage.Setup(u => u.GetHealth()).Returns(new LlmRuntimeHealthSnapshot("openai", Array.Empty<string>(), Array.Empty<string>(), 0, 0, 0));

        var svc = new LlmSmokeService(
            new[] { openAi.Object },
            usage.Object,
            Options.Create(new AiOptions { Enabled = true, Provider = "openai" }),
            NullLogger<LlmSmokeService>.Instance);

        var health = svc.GetHealth();
        Assert.True(health.Enabled);
        Assert.Single(health.Providers);
        Assert.Equal("openai", health.Providers[0].ProviderId);
        Assert.False(health.Providers[0].IsConfigured);
    }

    [Fact]
    public async Task Smoke_returns_NotConfigured_without_key()
    {
        var openAi = new Mock<ILlmProviderImplementation>();
        openAi.Setup(p => p.ProviderId).Returns("openai");
        openAi.Setup(p => p.IsConfigured).Returns(false);

        var usage = new Mock<ILlmUsageTracker>();
        usage.Setup(u => u.GetHealth()).Returns(new LlmRuntimeHealthSnapshot("openai", Array.Empty<string>(), Array.Empty<string>(), 0, 0, 0));

        var svc = new LlmSmokeService(
            new[] { openAi.Object },
            usage.Object,
            Options.Create(new AiOptions { Provider = "openai" }),
            NullLogger<LlmSmokeService>.Instance);

        var result = await svc.SmokeAsync("openai");
        Assert.Equal(LlmSmokeStatus.NotConfigured, result.Status);
        Assert.False(result.LiveAttempted);
    }

    [Fact]
    public async Task Smoke_returns_Configured_when_key_present_but_no_live_opt_in()
    {
        var openAi = new Mock<ILlmProviderImplementation>();
        openAi.Setup(p => p.ProviderId).Returns("openai");
        openAi.Setup(p => p.IsConfigured).Returns(true);

        var usage = new Mock<ILlmUsageTracker>();
        usage.Setup(u => u.GetHealth()).Returns(new LlmRuntimeHealthSnapshot("openai", new[] { "openai" }, Array.Empty<string>(), 0, 0, 0));

        var svc = new LlmSmokeService(
            new[] { openAi.Object },
            usage.Object,
            Options.Create(new AiOptions { Provider = "openai" }),
            NullLogger<LlmSmokeService>.Instance);

        var result = await svc.SmokeAsync("openai");
        Assert.Equal(LlmSmokeStatus.Configured, result.Status);
        Assert.False(result.LiveAttempted);
    }

    [Fact]
    public async Task Smoke_returns_ProviderUnavailable_for_unknown_provider()
    {
        var usage = new Mock<ILlmUsageTracker>();
        usage.Setup(u => u.GetHealth()).Returns(new LlmRuntimeHealthSnapshot("openai", Array.Empty<string>(), Array.Empty<string>(), 0, 0, 0));

        var svc = new LlmSmokeService(
            Array.Empty<ILlmProviderImplementation>(),
            usage.Object,
            Options.Create(new AiOptions { Provider = "openai" }),
            NullLogger<LlmSmokeService>.Instance);

        var result = await svc.SmokeAsync("unknown");
        Assert.Equal(LlmSmokeStatus.ProviderUnavailable, result.Status);
    }
}
