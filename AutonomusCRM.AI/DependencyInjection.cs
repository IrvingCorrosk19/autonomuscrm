using AutonomusCRM.AI.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAiRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.PostConfigure<AiOptions>(opts =>
        {
            if (string.IsNullOrWhiteSpace(opts.OpenAI.ApiKey) && !string.IsNullOrWhiteSpace(opts.ApiKey))
                opts.OpenAI.ApiKey = opts.ApiKey;
            if (string.IsNullOrWhiteSpace(opts.OpenAI.Model) && !string.IsNullOrWhiteSpace(opts.Model))
                opts.OpenAI.Model = opts.Model;
        });

        services.AddHttpClient("LlmOpenAI");
        services.AddHttpClient("LlmAzure");
        services.AddHttpClient("LlmAnthropic");
        services.AddHttpClient("LlmGemini");

        services.AddSingleton<ILlmProviderImplementation, OpenAiLlmProvider>();
        services.AddSingleton<ILlmProviderImplementation, AzureOpenAiLlmProvider>();
        services.AddSingleton<ILlmProviderImplementation, AnthropicLlmProvider>();
        services.AddSingleton<ILlmProviderImplementation, GeminiLlmProvider>();

        services.AddSingleton<ResilientLlmProvider>(sp => new ResilientLlmProvider(
            sp.GetServices<ILlmProviderImplementation>(),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResilientLlmProvider>>()));
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<ResilientLlmProvider>());
        services.AddSingleton<ILlmUsageTracker>(sp => sp.GetRequiredService<ResilientLlmProvider>());

        services.AddSingleton<IAgentService, LlmAgentService>();
        services.AddSingleton<IAutonomousWorkflow, LlmAutonomousWorkflow>();
        services.AddSingleton<ILlmSmokeService, LlmSmokeService>();
        services.AddSingleton<IEmbeddingService, UnconfiguredEmbeddingService>();

        return services;
    }

    [Obsolete("Use AddAiRuntime — placeholders removed in Truth Sprint.")]
    public static IServiceCollection AddAiPlaceholders(this IServiceCollection services, IConfiguration configuration) =>
        AddAiRuntime(services, configuration);
}
