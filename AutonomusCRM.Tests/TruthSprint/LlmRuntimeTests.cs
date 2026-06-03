using AutonomusCRM.AI;
using AutonomusCRM.AI.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AutonomusCRM.Tests.TruthSprint;

public class LlmRuntimeTests
{
    [Fact]
    public async Task ResilientLlmProvider_throws_when_no_provider_configured()
    {
        var options = Options.Create(new AiOptions { Enabled = true, Provider = "openai" });
        var provider = new ResilientLlmProvider(Array.Empty<ILlmProviderImplementation>(), options, NullLogger<ResilientLlmProvider>.Instance);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            provider.CompleteAsync(new LlmCompletionRequest("sys", "user")));
    }

    [Fact]
    public async Task ResilientLlmProvider_throws_when_ai_disabled()
    {
        var options = Options.Create(new AiOptions { Enabled = false });
        var openAi = new Mock<ILlmProviderImplementation>();
        openAi.Setup(p => p.ProviderId).Returns("openai");
        openAi.Setup(p => p.IsConfigured).Returns(true);
        var provider = new ResilientLlmProvider(new[] { openAi.Object }, options, NullLogger<ResilientLlmProvider>.Instance);
        await Assert.ThrowsAsync<LlmNotConfiguredException>(() =>
            provider.CompleteAsync(new LlmCompletionRequest("sys", "user")));
    }

    [Fact]
    public async Task ResilientLlmProvider_uses_primary_when_configured()
    {
        var options = Options.Create(new AiOptions { Enabled = true, Provider = "openai", MaxRetries = 1 });
        var openAi = new Mock<ILlmProviderImplementation>();
        openAi.Setup(p => p.ProviderId).Returns("openai");
        openAi.Setup(p => p.IsConfigured).Returns(true);
        openAi.Setup(p => p.CompleteAsync(It.IsAny<LlmCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult("ok", 10, false, "openai", "gpt-4o-mini"));

        var provider = new ResilientLlmProvider(new[] { openAi.Object }, options, NullLogger<ResilientLlmProvider>.Instance);
        var result = await provider.CompleteAsync(new LlmCompletionRequest("sys", "user"));
        Assert.Equal("ok", result.Content);
        Assert.False(result.IsPlaceholder);
    }

    [Fact]
    public async Task ResilientLlmProvider_falls_back_on_primary_failure()
    {
        var options = Options.Create(new AiOptions
        {
            Enabled = true,
            Provider = "openai",
            FallbackProviders = new[] { "anthropic" },
            MaxRetries = 1
        });
        var openAi = new Mock<ILlmProviderImplementation>();
        openAi.Setup(p => p.ProviderId).Returns("openai");
        openAi.Setup(p => p.IsConfigured).Returns(true);
        openAi.Setup(p => p.CompleteAsync(It.IsAny<LlmCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("fail"));

        var anthropic = new Mock<ILlmProviderImplementation>();
        anthropic.Setup(p => p.ProviderId).Returns("anthropic");
        anthropic.Setup(p => p.IsConfigured).Returns(true);
        anthropic.Setup(p => p.CompleteAsync(It.IsAny<LlmCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult("fallback", 5, false, "anthropic", "claude"));

        var provider = new ResilientLlmProvider(new[] { openAi.Object, anthropic.Object }, options, NullLogger<ResilientLlmProvider>.Instance);
        var result = await provider.CompleteAsync(new LlmCompletionRequest("s", "u"));
        Assert.Equal("fallback", result.Content);
    }

    [Fact]
    public void OpenAi_provider_not_configured_without_key()
    {
        var factory = new Mock<IHttpClientFactory>();
        var p = new OpenAiLlmProvider(factory.Object, Options.Create(new AiOptions()), NullLogger<OpenAiLlmProvider>.Instance);
        Assert.False(p.IsConfigured);
    }

    [Fact]
    public void Azure_provider_requires_endpoint_and_key()
    {
        var factory = new Mock<IHttpClientFactory>();
        var p = new AzureOpenAiLlmProvider(factory.Object, Options.Create(new AiOptions()), NullLogger<AzureOpenAiLlmProvider>.Instance);
        Assert.False(p.IsConfigured);
    }

    [Fact]
    public void Anthropic_provider_requires_api_key()
    {
        var factory = new Mock<IHttpClientFactory>();
        var p = new AnthropicLlmProvider(factory.Object, Options.Create(new AiOptions()), NullLogger<AnthropicLlmProvider>.Instance);
        Assert.False(p.IsConfigured);
    }

    [Fact]
    public void Gemini_provider_requires_api_key()
    {
        var factory = new Mock<IHttpClientFactory>();
        var p = new GeminiLlmProvider(factory.Object, Options.Create(new AiOptions()), NullLogger<GeminiLlmProvider>.Instance);
        Assert.False(p.IsConfigured);
    }

    [Fact]
    public async Task LlmAgentService_delegates_to_llm()
    {
        var llm = new Mock<ILLMProvider>();
        llm.Setup(l => l.CompleteAsync(It.IsAny<LlmCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult("agent output", 12, false, "openai", "gpt-4o-mini"));
        var svc = new LlmAgentService(llm.Object, Options.Create(new AiOptions()), NullLogger<LlmAgentService>.Instance);
        var result = await svc.ExecuteAsync(new AgentRequest("test-agent", Guid.NewGuid().ToString(), "hello"));
        Assert.Equal("agent output", result.Output);
        Assert.False(string.IsNullOrEmpty(result.Provider));
    }

    [Fact]
    public void Usage_tracker_records_requests()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(new LlmUsageRecord { Provider = "openai", Model = "gpt", PromptTokens = 5, CompletionTokens = 5, Success = true });
        var recent = tracker.GetRecent();
        Assert.Single(recent);
        Assert.Equal(10, recent[0].TotalTokens);
    }

    [Fact]
    public void Placeholder_services_removed_from_assembly()
    {
        var placeholderType = Type.GetType("AutonomusCRM.AI.PlaceholderLlmProvider, AutonomusCRM.AI");
        Assert.Null(placeholderType);
    }
}
