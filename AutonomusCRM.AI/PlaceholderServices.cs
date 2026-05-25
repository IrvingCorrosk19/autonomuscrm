using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI;

public sealed class PlaceholderAgentService : IAgentService
{
    private readonly AiOptions _options;
    private readonly ILogger<PlaceholderAgentService> _logger;

    public PlaceholderAgentService(IOptions<AiOptions> options, ILogger<PlaceholderAgentService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<AgentExecutionResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AI placeholder: agent {AgentId} for tenant {TenantId}. Configure AI:Provider and AI:ApiKey to enable.",
            request.AgentId,
            request.TenantId);

        return Task.FromResult(new AgentExecutionResult(
            Success: true,
            Output: $"[AI PLACEHOLDER] Acción simulada para agente '{request.AgentId}'. Provider configurado: '{_options.Provider ?? "(vacío)"}'.",
            Provider: string.IsNullOrWhiteSpace(_options.Provider) ? null : _options.Provider,
            Model: string.IsNullOrWhiteSpace(_options.Model) ? null : _options.Model));
    }
}

public sealed class PlaceholderLlmProvider : ILLMProvider
{
    public Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LlmCompletionResult(
            $"[AI PLACEHOLDER] Respuesta simulada para: {request.UserPrompt[..Math.Min(80, request.UserPrompt.Length)]}...",
            TokensUsed: 0,
            IsPlaceholder: true));
    }
}

public sealed class PlaceholderEmbeddingService : IEmbeddingService
{
    public Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var hash = text.GetHashCode();
        var vector = new float[8];
        for (var i = 0; i < vector.Length; i++)
            vector[i] = ((hash >> i) & 0xFF) / 255f;

        return Task.FromResult(new EmbeddingResult(vector, Model: "placeholder-local", IsPlaceholder: true));
    }
}

public sealed class PlaceholderAutonomousWorkflow : IAutonomousWorkflow
{
    public Task<WorkflowRunResult> RunAsync(AutonomousWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WorkflowRunResult(
            Completed: true,
            Summary: $"[AI PLACEHOLDER] Workflow '{request.WorkflowId}' simulado para evento '{request.TriggerEvent}'.",
            StepsExecuted: new[] { "validate-input", "simulate-decision", "emit-placeholder-result" },
            IsPlaceholder: true));
    }
}
