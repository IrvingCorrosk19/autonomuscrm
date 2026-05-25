namespace AutonomusCRM.AI;

/// <summary>
/// Orquestación de agentes autónomos (placeholder — sin conexión LLM real).
/// </summary>
public interface IAgentService
{
    Task<AgentExecutionResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

public record AgentRequest(
    string AgentId,
    string TenantId,
    string Prompt,
    IReadOnlyDictionary<string, string>? Context = null);

public record AgentExecutionResult(
    bool Success,
    string Output,
    string? Provider = null,
    string? Model = null);
