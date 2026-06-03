using AutonomusCRM.AI;
using AutonomusCRM.Application.SemanticMemory;

namespace AutonomusCRM.Infrastructure.Ai;

/// <summary>Bridges production embeddings to IEmbeddingService — no placeholder vectors.</summary>
public sealed class ProductionEmbeddingServiceAdapter : IEmbeddingService
{
    private readonly IProductionEmbeddingProvider _provider;

    public ProductionEmbeddingServiceAdapter(IProductionEmbeddingProvider provider) => _provider = provider;

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await _provider.EmbedAsync(text, cancellationToken);
        return new EmbeddingResult(result.Vector, result.Model, IsPlaceholder: !result.IsProductionProvider);
    }
}
