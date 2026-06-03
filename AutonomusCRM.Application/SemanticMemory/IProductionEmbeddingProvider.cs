namespace AutonomusCRM.Application.SemanticMemory;

public record ProductionEmbeddingResult(
    float[] Vector,
    string Provider,
    string Model,
    bool IsProductionProvider,
    string Badge);

public interface IProductionEmbeddingProvider
{
    Task<ProductionEmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default);
    ProductionEmbeddingStatus GetStatus();
}

public record ProductionEmbeddingStatus(
    string ActiveProvider,
    bool IsProductionProvider,
    string Badge,
    bool OpenAiConfigured,
    bool AzureOpenAiConfigured);
