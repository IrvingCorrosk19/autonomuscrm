namespace AutonomusCRM.AI;

/// <summary>
/// Proveedor LLM (OpenAI, Claude, Gemini, etc.) — placeholder configurable.
/// </summary>
public interface ILLMProvider
{
    Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken = default);
}

public record LlmCompletionRequest(
    string SystemPrompt,
    string UserPrompt,
    string? Model = null,
    int? MaxTokens = null);

public record LlmCompletionResult(
    string Content,
    int TokensUsed,
    bool IsPlaceholder);
