using System.Text.Json;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Application.SemanticMemory;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Insights;

public sealed class DbIntelligenceInsightSemanticEnhancer
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ISemanticMemoryService? _semantic;
    private readonly ILogger<DbIntelligenceInsightSemanticEnhancer> _logger;

    public DbIntelligenceInsightSemanticEnhancer(
        ILogger<DbIntelligenceInsightSemanticEnhancer> logger,
        ISemanticMemoryService? semantic = null)
    {
        _semantic = semantic;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DbIntelligenceInsightDto>> EnrichAsync(
        Guid tenantId,
        IReadOnlyList<DbIntelligenceInsightDto> insights,
        CancellationToken cancellationToken = default)
    {
        if (_semantic == null || insights.Count == 0)
            return insights;

        var enriched = new List<DbIntelligenceInsightDto>(insights.Count);
        foreach (var insight in insights)
        {
            var semanticScore = insight.SemanticMatchScore;
            try
            {
                if (!string.IsNullOrWhiteSpace(insight.TableName) && insight.EntityType.HasValue)
                {
                    var query = $"{insight.TableName} {insight.EntityType} business entity CRM mapping";
                    var hits = await _semantic.FindSimilarMemoriesAsync(tenantId, query, 5, cancellationToken);
                    if (hits.Count > 0)
                    {
                        var best = hits.Max(h => h.Similarity);
                        semanticScore = Math.Max(semanticScore, (int)Math.Round(best * 100));
                    }
                }

                var memoryText = JsonSerializer.Serialize(new
                {
                    insight.Type,
                    insight.Title,
                    insight.Summary,
                    insight.SuggestedAction,
                    insight.Evidence
                }, JsonOptions);

                await _semantic.StoreMemoryAsync(
                    tenantId,
                    DbIntelligenceSemanticSourceType.DipInsight,
                    insight.Id,
                    memoryText,
                    insight.ConfidencePercent / 100.0,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantic enrichment skipped for insight {InsightId}", insight.Id);
            }

            enriched.Add(semanticScore == insight.SemanticMatchScore
                ? insight
                : insight with
                {
                    SemanticMatchScore = semanticScore,
                    PriorityScore = DbIntelligenceInsightEngine.ComputePriority(
                        insight.ImpactScore, insight.EffortScore, insight.ConfidencePercent)
                });
        }

        return enriched.OrderByDescending(i => i.PriorityScore).ToList();
    }
}
