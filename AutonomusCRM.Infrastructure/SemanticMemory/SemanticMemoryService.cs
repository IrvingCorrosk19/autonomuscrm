using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.SemanticMemory;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.SemanticMemory;

public sealed class SemanticMemoryService : ISemanticMemoryService
{
    private readonly ISemanticMemoryRepository _repo;
    private readonly IBusinessMemoryRepository _businessMemory;
    private readonly IProductionEmbeddingProvider _embeddings;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SemanticMemoryService> _logger;

    public SemanticMemoryService(
        ISemanticMemoryRepository repo,
        IBusinessMemoryRepository businessMemory,
        IProductionEmbeddingProvider embeddings,
        IUnitOfWork uow,
        ILogger<SemanticMemoryService> logger)
    {
        _repo = repo;
        _businessMemory = businessMemory;
        _embeddings = embeddings;
        _uow = uow;
        _logger = logger;
    }

    public async Task<MemoryEmbedding> StoreMemoryAsync(
        Guid tenantId, string sourceType, Guid sourceId, string text,
        double confidence = 0.7, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text required for semantic memory.", nameof(text));

        var result = await _embeddings.EmbedAsync(text, cancellationToken);
        var model = TruncateEmbeddingModel($"{result.Model}|{result.Provider}|{result.Badge}");
        var existing = await _repo.GetBySourceAsync(tenantId, sourceType, sourceId, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateVector(result.Vector, model);
            existing.SetScores(existing.RelevanceScore, confidence);
            await _repo.UpdateEmbeddingAsync(existing, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var embedding = MemoryEmbedding.Create(
            tenantId, sourceType, sourceId, text, result.Vector, model, confidence);
        await _repo.AddEmbeddingAsync(embedding, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return embedding;
    }

    public async Task<IReadOnlyList<SemanticMemoryHitDto>> SearchAsync(
        Guid tenantId, string query, int take = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SemanticMemoryHitDto>();

        var queryVector = (await _embeddings.EmbedAsync(query, cancellationToken)).Vector;
        var candidateLimit = Math.Clamp(take * 3, 15, 40);
        var candidates = await _repo.GetByTenantAsync(tenantId, candidateLimit, cancellationToken);
        var results = RankAndMap(candidates, queryVector, query, take, recordUsage: true);
        if (results.Count > 0)
            await _uow.SaveChangesAsync(cancellationToken);
        return results;
    }

    public Task<IReadOnlyList<SemanticMemoryHitDto>> FindSimilarMemoriesAsync(
        Guid tenantId, string text, int take = 15, CancellationToken cancellationToken = default)
        => SearchAsync(tenantId, text, take, cancellationToken);

    public async Task<IReadOnlyList<string>> GetRelatedLearningsAsync(
        Guid tenantId, string contextQuery, int take = 10, CancellationToken cancellationToken = default)
    {
        var hits = await SearchAsync(tenantId, contextQuery, take, cancellationToken);
        var learnings = await _repo.GetLearningsAsync(tenantId, take * 2, cancellationToken);

        var related = learnings
            .Where(l => hits.Any(h => h.Text.Contains(l.StrategyKey, StringComparison.OrdinalIgnoreCase)
                || l.ActionTaken.Contains(contextQuery, StringComparison.OrdinalIgnoreCase)
                || contextQuery.Contains(l.StrategyKey, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(l => l.SuccessRate)
            .Take(take)
            .Select(l => $"{l.StrategyKey}: {l.ActionTaken} ({l.SuccessRate:F0}% éxito, n={l.SuccessCount + l.FailureCount})")
            .ToList();

        if (related.Count == 0)
        {
            related = learnings
                .OrderByDescending(l => l.SuccessRate)
                .Take(take)
                .Select(l => $"{l.StrategyKey}: {l.ActionTaken} ({l.SuccessRate:F0}% éxito)")
                .ToList();
        }

        return related;
    }

    public async Task<SemanticBusinessContextDto> GetBusinessContextAsync(
        Guid tenantId, string query, CancellationToken cancellationToken = default)
    {
        var similar = await FindSimilarMemoriesAsync(tenantId, query, SemanticMemoryConstants.DefaultSimilarTake, cancellationToken);
        var learnings = await GetRelatedLearningsAsync(tenantId, query, 8, cancellationToken);
        var insights = await _repo.GetInsightsAsync(tenantId, 5, cancellationToken);
        var topInsightTexts = insights
            .Select(i => i.Content)
            .Concat(similar.Where(h => h.SourceType == SemanticMemoryConstants.SourceCustomerInsight).Select(h => h.Text))
            .Distinct()
            .Take(5)
            .ToList();

        var narrative = similar.Count > 0
            ? $"Contexto semántico ({similar.Count} memorias): " + string.Join("; ", similar.Take(3).Select(s => s.Text.Length > 120 ? s.Text[..120] + "…" : s.Text))
            : "Sin memorias semánticas previas para esta consulta.";

        if (learnings.Count > 0)
            narrative += " | Aprendizajes: " + string.Join("; ", learnings.Take(3));

        return new SemanticBusinessContextDto(query, similar, learnings, topInsightTexts, narrative);
    }

    public Task<IReadOnlyList<MemoryTimelineItemDto>> GetTimelineAsync(
        Guid tenantId, int take = 100, CancellationToken cancellationToken = default)
        => _repo.GetTimelineAsync(tenantId, take, cancellationToken);

    public async Task<SemanticMemoryDashboardDto> GetDashboardAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var total = await _repo.CountEmbeddingsAsync(tenantId, cancellationToken);
        var learnings = await _repo.GetLearningsAsync(tenantId, 200, cancellationToken);
        var patterns = learnings.Count(l => l.SuccessCount + l.FailureCount >= 3);

        var insightHits = await _repo.GetBySourceTypesAsync(
            tenantId,
            new[] { SemanticMemoryConstants.SourceCustomerInsight, SemanticMemoryConstants.SourceRevenueInsight, SemanticMemoryConstants.SourceLearning },
            10,
            cancellationToken);

        var decisionEmbeddings = await _repo.GetBySourceTypesAsync(
            tenantId, new[] { SemanticMemoryConstants.SourceDecision }, 30, cancellationToken);
        var successful = decisionEmbeddings
            .Where(e => e.Text.Contains("success", StringComparison.OrdinalIgnoreCase) || e.ConfidenceScore >= 0.8)
            .OrderByDescending(e => e.RelevanceScore)
            .Take(5)
            .Select(ToHitFromStored)
            .ToList();

        var playbookEmbeddings = await _repo.GetBySourceTypesAsync(
            tenantId, new[] { SemanticMemoryConstants.SourceLearning, SemanticMemoryConstants.SourceEpisode }, 30, cancellationToken);
        var effective = playbookEmbeddings
            .OrderByDescending(e => e.RelevanceScore * e.ConfidenceScore)
            .Take(5)
            .Select(ToHitFromStored)
            .ToList();

        return new SemanticMemoryDashboardDto(
            total,
            patterns,
            insightHits.Select(ToHitFromStored).ToList(),
            successful,
            effective);
    }

    public async Task<CustomerMemoryProfileDto> GetOrBuildCustomerProfileAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var profile = await _repo.GetCustomerProfileAsync(tenantId, customerId, cancellationToken);
        if (profile is not null && profile.LastRefreshedAt > DateTime.UtcNow.AddHours(-6))
            return ToProfileDto(profile);

        var episodes = await _businessMemory.GetBySubjectAsync(
            tenantId, BusinessMemoryConstants.SubjectCustomer, customerId, 30, cancellationToken);
        var insights = await _businessMemory.GetInsightsForCustomerAsync(tenantId, customerId, 15, cancellationToken);
        var memoryIds = episodes.Select(e => e.Id).ToHashSet();
        var decisions = (await _repo.GetDecisionsAsync(tenantId, 100, cancellationToken))
            .Where(d => memoryIds.Contains(d.MemoryId))
            .ToList();

        var similar = await FindSimilarMemoriesAsync(
            tenantId, $"customer {customerId} retention churn expansion", 10, cancellationToken);

        var history = episodes.Count > 0
            ? string.Join("; ", episodes.Take(5).Select(e => e.Title))
            : "Sin episodios registrados.";
        var risks = insights.Where(i => i.InsightType.Contains("risk", StringComparison.OrdinalIgnoreCase) || i.InsightType.Contains("churn", StringComparison.OrdinalIgnoreCase))
            .Select(i => i.Content).FirstOrDefault() ?? "Sin riesgos destacados en memoria.";
        var preferences = insights.Where(i => i.InsightType.Contains("preference", StringComparison.OrdinalIgnoreCase))
            .Select(i => i.Content).FirstOrDefault() ?? "Preferencias por inferir de interacciones.";
        var successful = string.Join("; ", decisions.Where(d => d.WasSuccessful == true).Take(3).Select(d => d.Action));
        if (string.IsNullOrWhiteSpace(successful))
            successful = string.Join("; ", similar.Where(s => s.ConfidenceScore >= 0.7).Take(2).Select(s => s.Text));
        var failed = string.Join("; ", decisions.Where(d => d.WasSuccessful == false).Take(3).Select(d => d.Action));
        if (string.IsNullOrWhiteSpace(failed))
            failed = "Sin decisiones fallidas registradas.";
        var channels = string.Join(", ", episodes.SelectMany(e => e.Tags).Distinct().Take(5));
        if (string.IsNullOrWhiteSpace(channels))
            channels = "Email, Phone";

        if (profile is null)
        {
            profile = CustomerMemoryProfile.Create(tenantId, customerId);
            profile.Refresh(history, risks, preferences, successful, failed, channels);
            await _repo.AddCustomerProfileAsync(profile, cancellationToken);
        }
        else
        {
            profile.Refresh(history, risks, preferences, successful, failed, channels);
            await _repo.UpdateCustomerProfileAsync(profile, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return ToProfileDto(profile);
    }

    public async Task<int> ConsolidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var observations = await _repo.GetObservationsForConsolidationAsync(tenantId, 500, cancellationToken);
        var clusters = observations
            .GroupBy(o => $"{o.Channel}:{NormalizeClusterKey(o.Content)}")
            .Where(g => g.Count() >= SemanticMemoryConstants.ConsolidationMinClusterSize)
            .ToList();

        var created = 0;
        foreach (var cluster in clusters)
        {
            var strategyKey = $"consolidated.{cluster.Key}";
            var existing = await _businessMemory.GetLearningAsync(tenantId, strategyKey, cancellationToken);
            if (existing is not null)
                continue;

            var sample = cluster.First();
            var learning = BusinessMemoryLearning.Start(
                tenantId,
                strategyKey,
                $"auto_consolidate_{sample.Channel}",
                new Dictionary<string, object>
                {
                    ["clusterSize"] = cluster.Count(),
                    ["channel"] = sample.Channel,
                    ["sample"] = sample.Content.Length > 200 ? sample.Content[..200] : sample.Content
                });

            learning.ApplyOutcome(true, $"Consolidated from {cluster.Count()} similar observations");
            await _businessMemory.AddLearningAsync(learning, cancellationToken);

            await StoreMemoryAsync(
                tenantId,
                SemanticMemoryConstants.SourceLearning,
                learning.Id,
                $"Patrón consolidado: {cluster.Count()} observaciones en {sample.Channel} — {sample.Content[..Math.Min(150, sample.Content.Length)]}",
                0.85,
                cancellationToken);

            created++;
        }

        if (created > 0)
            await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Semantic consolidation tenant {TenantId}: {Created} learnings", tenantId, created);
        return created;
    }

    public async Task IndexBusinessMemorySourcesAsync(
        Guid tenantId, int takePerType = 50, CancellationToken cancellationToken = default)
    {
        var observations = await _repo.GetObservationsForConsolidationAsync(tenantId, takePerType, cancellationToken);
        foreach (var o in observations)
            await TryStoreMemoryAsync(tenantId, SemanticMemoryConstants.SourceObservation, o.Id, o.Content, 0.6, cancellationToken);

        foreach (var l in await _repo.GetLearningsAsync(tenantId, takePerType, cancellationToken))
            await TryStoreMemoryAsync(tenantId, SemanticMemoryConstants.SourceLearning, l.Id,
                $"{l.StrategyKey} {l.ActionTaken} success={l.SuccessRate}%", (double)l.SuccessRate / 100.0, cancellationToken);

        foreach (var d in await _repo.GetDecisionsAsync(tenantId, takePerType, cancellationToken))
            await TryStoreMemoryAsync(tenantId, SemanticMemoryConstants.SourceDecision, d.Id,
                $"{d.DecisionType} {d.Action}: {d.Reason}", d.WasSuccessful == true ? 0.9 : 0.5, cancellationToken);

        foreach (var i in await _repo.GetInsightsAsync(tenantId, takePerType, cancellationToken))
        {
            var type = i.CustomerId.HasValue
                ? SemanticMemoryConstants.SourceCustomerInsight
                : SemanticMemoryConstants.SourceRevenueInsight;
            await TryStoreMemoryAsync(tenantId, type, i.Id, i.Content, i.Confidence, cancellationToken);
        }

        var episodes = await _businessMemory.GetRecentAsync(tenantId, takePerType, cancellationToken);
        foreach (var e in episodes)
            await TryStoreMemoryAsync(tenantId, SemanticMemoryConstants.SourceEpisode, e.Id,
                $"{e.Title}. {e.Summary}", e.Importance / 10.0, cancellationToken);
    }

    private async Task TryStoreMemoryAsync(
        Guid tenantId, string sourceType, Guid sourceId, string text, double confidence,
        CancellationToken cancellationToken)
    {
        try
        {
            await StoreMemoryAsync(tenantId, sourceType, sourceId, text, confidence, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic index skipped for {SourceType}/{SourceId}", sourceType, sourceId);
        }
    }

    private List<SemanticMemoryHitDto> RankAndMap(
        IReadOnlyList<MemoryEmbedding> candidates,
        float[] queryVector,
        string query,
        int take,
        bool recordUsage)
    {
        var ranked = candidates
            .Select(e =>
            {
                var cosine = SemanticMemoryVectorMath.CosineSimilarity(queryVector, e.EmbeddingVector);
                var lexical = SemanticMemoryVectorMath.LexicalScore(e.Text, query);
                var score = cosine * 0.7 + lexical * 0.2 + e.RelevanceScore * 0.1;
                return (e, score);
            })
            .OrderByDescending(x => x.score)
            .Take(take)
            .ToList();

        var results = new List<SemanticMemoryHitDto>();
        foreach (var (e, score) in ranked)
        {
            if (recordUsage)
                e.RecordUsage();
            results.Add(new SemanticMemoryHitDto(
                e.Id, e.SourceType, e.SourceId, e.Text, score,
                e.RelevanceScore, e.ConfidenceScore, e.UsageCount, e.CreatedAt));
        }

        return results;
    }

    private static SemanticMemoryHitDto ToHitFromStored(MemoryEmbedding e) =>
        new(e.Id, e.SourceType, e.SourceId, e.Text, e.RelevanceScore, e.RelevanceScore, e.ConfidenceScore, e.UsageCount, e.CreatedAt);

    private static CustomerMemoryProfileDto ToProfileDto(CustomerMemoryProfile p) =>
        new(p.CustomerId, p.HistorySummary, p.RiskSummary, p.PreferencesSummary,
            p.SuccessfulDecisionsSummary, p.FailedDecisionsSummary, p.EffectiveChannelsSummary, p.LastRefreshedAt);

    private static string NormalizeClusterKey(string content)
    {
        var normalized = new string(content
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray());
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 4 ? string.Join(' ', words.Take(4)) : normalized.Length > 40 ? normalized[..40] : normalized;
    }

    private static string TruncateEmbeddingModel(string model) =>
        model.Length <= 80 ? model : model[..80];
}
