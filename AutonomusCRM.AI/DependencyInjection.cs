using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAiPlaceholders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddSingleton<IAgentService, PlaceholderAgentService>();
        services.AddSingleton<ILLMProvider, PlaceholderLlmProvider>();
        services.AddSingleton<IEmbeddingService, PlaceholderEmbeddingService>();
        services.AddSingleton<IAutonomousWorkflow, PlaceholderAutonomousWorkflow>();
        return services;
    }
}
