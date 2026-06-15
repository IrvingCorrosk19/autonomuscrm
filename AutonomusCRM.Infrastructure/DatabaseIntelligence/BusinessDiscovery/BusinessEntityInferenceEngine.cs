using System.Text.Json;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;

public sealed class BusinessEntityInferenceEngine : IBusinessEntityInferenceEngine
{
    public IReadOnlyList<BusinessEntityInferenceResult> InferFromCatalog(
        BusinessDiscoveryCatalogInput catalog,
        IProgress<BusinessDiscoveryProgress>? progress = null)
    {
        var tables = catalog.Tables
            .Where(t => t.ObjectType != DbCatalogObjectTypes.View)
            .ToList();

        progress?.Report(new BusinessDiscoveryProgress(BusinessDiscoveryStages.AnalyzingTables, 10,
            Message: $"Analyzing {tables.Count} tables"));

        var preliminary = new Dictionary<string, BusinessEntityType>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            var key = BusinessDiscoveryCatalogInput.TableKey(table.SchemaName, table.TableName);
            preliminary[key] = ScoreTableOnly(table.TableName);
        }

        progress?.Report(new BusinessDiscoveryProgress(BusinessDiscoveryStages.AnalyzingColumns, 35,
            Message: "Analyzing columns and relationships"));

        var results = new List<BusinessEntityInferenceResult>();
        var index = 0;
        foreach (var table in tables)
        {
            index++;
            var columns = catalog.Columns
                .Where(c => string.Equals(c.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(c.TableName, table.TableName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            catalog.SampleRowsByTableKey.TryGetValue(
                BusinessDiscoveryCatalogInput.TableKey(table.SchemaName, table.TableName),
                out var samples);

            var inference = InferTable(table.SchemaName, table.TableName, columns, catalog, preliminary, samples);
            results.Add(inference);
            preliminary[BusinessDiscoveryCatalogInput.TableKey(table.SchemaName, table.TableName)] = inference.EntityType;

            if (index % 3 == 0)
            {
                progress?.Report(new BusinessDiscoveryProgress(
                    BusinessDiscoveryStages.DetectingEntities,
                    35 + (int)(50.0 * index / Math.Max(1, tables.Count)),
                    table.TableName,
                    $"Detected {BusinessEntitySignals.DisplayName(inference.EntityType)} ({inference.ConfidencePercent}%)"));
            }
        }

        progress?.Report(new BusinessDiscoveryProgress(BusinessDiscoveryStages.CalculatingConfidence, 92,
            Message: "Calculating confidence scores"));
        progress?.Report(new BusinessDiscoveryProgress(BusinessDiscoveryStages.Completed, 100,
            Message: $"Completed — {results.Count(r => r.EntityType != BusinessEntityType.Unknown)} entities detected"));

        return results;
    }

    private static BusinessEntityType ScoreTableOnly(string tableName)
    {
        var bestType = BusinessEntityType.Unknown;
        var bestScore = 0.0;
        foreach (var profile in BusinessEntitySignals.Profiles)
        {
            var score = BusinessEntitySignals.ScoreTableName(tableName, profile);
            if (score > bestScore)
            {
                bestScore = score;
                bestType = profile.Type;
            }
        }

        return bestScore >= 55 ? bestType : BusinessEntityType.Unknown;
    }

    private static BusinessEntityInferenceResult InferTable(
        string schema,
        string table,
        IReadOnlyList<BusinessDiscoveryColumnInput> columns,
        BusinessDiscoveryCatalogInput catalog,
        Dictionary<string, BusinessEntityType> tableEntityHints,
        IReadOnlyList<IReadOnlyDictionary<string, string?>>? samples)
    {
        var candidates = new List<(BusinessEntityType Type, int Confidence, List<string> Reasons)>();

        foreach (var profile in BusinessEntitySignals.Profiles)
        {
            var reasons = new List<string>();
            var tableScore = BusinessEntitySignals.ScoreTableName(table, profile);
            if (tableScore >= 70)
                reasons.Add($"nombre de tabla coincide con {profile.DisplayName}");

            var columnScore = BusinessEntitySignals.ScoreColumns(columns, profile, reasons);
            var relScore = BusinessEntitySignals.ScoreRelationships(schema, table, catalog, tableEntityHints, profile, reasons);
            var sampleScore = BusinessEntitySignals.ScoreSamples(samples, columns, profile, reasons);

            var weighted = tableScore * 0.40 + columnScore * 0.30 + relScore * 0.15 + sampleScore * 0.15;
            if (profile.Type == BusinessEntityType.Contact &&
                BusinessEntitySignals.Tokenize(table).Contains("contact"))
                weighted += 12;
            if (tableScore >= 85)
                weighted = Math.Max(weighted, 78 + Math.Min(columnScore, 40) * 0.35);
            if (tableScore >= 95)
                weighted = Math.Max(weighted, 88);
            if (weighted < 25) continue;

            candidates.Add((profile.Type, (int)Math.Round(Math.Clamp(weighted, 40, 99)), reasons));
        }

        if (candidates.Count == 0)
        {
            return new BusinessEntityInferenceResult(
                schema, table, BusinessEntityType.Unknown, "Desconocido", 40,
                ["No hay señales suficientes — clasificado como desconocido"]);
        }

        if (BusinessEntitySignals.NormalizeIdentifier(table).Contains("contact", StringComparison.OrdinalIgnoreCase))
        {
            var contactCandidate = candidates
                .Where(c => c.Type == BusinessEntityType.Contact)
                .OrderByDescending(c => c.Confidence)
                .FirstOrDefault();
            if (contactCandidate.Reasons != null)
                return BuildResult(schema, table, contactCandidate);
        }

        var best = candidates.OrderByDescending(c => c.Confidence).ThenBy(c => c.Type).First();
        return BuildResult(schema, table, best);
    }

    private static BusinessEntityInferenceResult BuildResult(
        string schema, string table, (BusinessEntityType Type, int Confidence, List<string> Reasons) best)
    {
        var distinctReasons = best.Reasons.Distinct().Take(8).ToList();
        if (distinctReasons.Count == 0)
            distinctReasons.Add($"estructura compatible con {BusinessEntitySignals.DisplayName(best.Type)}");

        return new BusinessEntityInferenceResult(
            schema,
            table,
            best.Type,
            BusinessEntitySignals.DisplayName(best.Type),
            best.Confidence,
            distinctReasons);
    }
}

internal static class BusinessDiscoveryMappingSerializer
{
    public static string SerializeReasons(IReadOnlyList<string> reasons) =>
        JsonSerializer.Serialize(reasons);

    public static IReadOnlyList<string> DeserializeReasons(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return Array.Empty<string>(); }
    }
}
