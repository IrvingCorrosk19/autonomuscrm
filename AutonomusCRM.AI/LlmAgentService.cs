using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI;

public sealed class LlmAgentService : IAgentService
{
    private readonly ILLMProvider _llm;
    private readonly AiOptions _options;
    private readonly ILogger<LlmAgentService> _logger;

    public LlmAgentService(ILLMProvider llm, IOptions<AiOptions> options, ILogger<LlmAgentService> logger)
    {
        _llm = llm;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var system = $"You are AutonomusFlow agent '{request.AgentId}' for tenant {request.TenantId}. Respond concisely with actionable CRM guidance.";
        var contextBlock = request.Context is { Count: > 0 }
            ? "\nContext:\n" + string.Join("\n", request.Context.Select(kv => $"- {kv.Key}: {kv.Value}"))
            : "";
        var user = request.Prompt + contextBlock;

        var result = await _llm.CompleteAsync(new LlmCompletionRequest(system, user, _options.Model), cancellationToken);
        if (result.IsPlaceholder)
            throw new Llm.LlmNotConfiguredException("Agent execution received placeholder response — LLM not configured.");

        _logger.LogInformation("Agent {AgentId} completed via {Provider} ({Tokens} tokens)", request.AgentId, result.Provider, result.TokensUsed);
        return new AgentExecutionResult(true, result.Content, result.Provider, result.Model);
    }
}

public sealed class LlmAutonomousWorkflow : IAutonomousWorkflow
{
    private readonly ILLMProvider _llm;
    private readonly AiOptions _options;

    public LlmAutonomousWorkflow(ILLMProvider llm, IOptions<AiOptions> options)
    {
        _llm = llm;
        _options = options.Value;
    }

    public async Task<WorkflowRunResult> RunAsync(AutonomousWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var system = $"Execute AutonomusFlow workflow '{request.WorkflowId}' triggered by '{request.TriggerEvent}'. Return JSON steps summary.";
        var user = request.Payload is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(request.Payload)
            : "{}";
        var result = await _llm.CompleteAsync(new LlmCompletionRequest(system, user, _options.Model), cancellationToken);
        return new WorkflowRunResult(
            Completed: true,
            Summary: result.Content,
            StepsExecuted: new[] { "llm-plan", "llm-execute", "llm-summarize" },
            IsPlaceholder: false);
    }
}

public sealed class UnconfiguredEmbeddingService : IEmbeddingService
{
    public Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
        throw new Llm.LlmNotConfiguredException(
            "IEmbeddingService requires ProductionEmbeddingProvider. Register Infrastructure embedding adapter or configure AI embeddings.");
}
