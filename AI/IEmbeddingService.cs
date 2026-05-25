namespace AutonomusCRM.AI;

/// <summary>
/// Generación de embeddings para búsqueda semántica — placeholder.
/// </summary>
public interface IEmbeddingService
{
    Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public record EmbeddingResult(float[] Vector, string Model, bool IsPlaceholder);
