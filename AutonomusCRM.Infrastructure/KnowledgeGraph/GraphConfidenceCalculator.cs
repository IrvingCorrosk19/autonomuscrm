namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

/// <summary>Evidence-based confidence — no hardcoded literals.</summary>
public static class GraphConfidenceCalculator
{
    public static double Calculate(GraphConfidenceInput input)
    {
        if (input.EvidenceCount <= 0 && input.EdgeCount <= 0)
            return Math.Round(Math.Clamp(input.BasePrior, 0.05, 0.25), 4);

        var evidenceFactor = Math.Min(0.30, input.EvidenceCount * 0.045);
        var edgeFactor = Math.Min(0.18, input.EdgeCount * 0.025);
        var relationshipFactor = Math.Min(0.12, input.RelationshipStrength * 0.12);

        var outcomeTotal = input.PositiveOutcomes + input.NegativeOutcomes;
        var outcomeFactor = outcomeTotal > 0
            ? 0.22 * (input.PositiveOutcomes / (double)outcomeTotal)
            : 0.08;

        var recencyFactor = input.LastSignalUtc.HasValue
            ? (DateTime.UtcNow - input.LastSignalUtc.Value).TotalDays switch
            {
                <= 7 => 0.12,
                <= 30 => 0.08,
                <= 90 => 0.04,
                _ => 0.01
            }
            : 0.02;

        var temporalFactor = Math.Min(0.08, input.TemporalRelevanceScore * 0.08);
        var semanticFactor = Math.Min(0.06, input.SemanticMatchScore * 0.06);

        var raw = input.BasePrior + evidenceFactor + edgeFactor + relationshipFactor +
                  outcomeFactor + recencyFactor + temporalFactor + semanticFactor;

        return Math.Round(Math.Clamp(raw, 0.05, 0.98), 4);
    }
}

public readonly record struct GraphConfidenceInput(
    int EvidenceCount,
    int EdgeCount,
    int PositiveOutcomes,
    int NegativeOutcomes,
    double RelationshipStrength,
    double SemanticMatchScore,
    double TemporalRelevanceScore,
    DateTime? LastSignalUtc,
    double BasePrior = 0.15);
