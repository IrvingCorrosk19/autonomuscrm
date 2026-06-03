namespace AutonomusCRM.Infrastructure.SemanticMemory;

internal static class SemanticMemoryVectorMath
{
    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0;

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    public static double LexicalScore(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 0;

        var t = text.ToLowerInvariant();
        var q = query.ToLowerInvariant();
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
            return 0;

        return words.Count(w => t.Contains(w, StringComparison.Ordinal)) / (double)words.Length;
    }
}
