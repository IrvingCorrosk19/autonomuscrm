using System.Text.RegularExpressions;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Insights;

public sealed class DbIntelligenceInsightEngine : IDbIntelligenceInsightEngine
{
    private static readonly Dictionary<BusinessEntityType, string[]> EntityTokens = new()
    {
        [BusinessEntityType.Customer] = ["customer", "client", "cliente", "account", "cli"],
        [BusinessEntityType.Company] = ["company", "empresa", "org", "organization"],
        [BusinessEntityType.Contact] = ["contact", "person", "contacto"],
        [BusinessEntityType.Sale] = ["sale", "order", "venta", "deal"],
        [BusinessEntityType.Invoice] = ["invoice", "factura", "billing"],
        [BusinessEntityType.Payment] = ["payment", "pago", "pay"],
        [BusinessEntityType.Product] = ["product", "sku", "item", "articulo"],
        [BusinessEntityType.Activity] = ["activity", "task", "call", "meeting"]
    };

    public IReadOnlyList<DbIntelligenceInsightDto> Generate(
        DbIntelligenceInsightBuildInput input,
        IProgress<DbIntelligenceInsightProgress>? progress = null)
    {
        progress?.Report(new DbIntelligenceInsightProgress(DbIntelligenceInsightStages.AnalyzingCatalog, 15));
        var insights = new List<DbIntelligenceInsightDto>();

        progress?.Report(new DbIntelligenceInsightProgress(DbIntelligenceInsightStages.EvaluatingHealth, 35));
        insights.AddRange(GenerateCriticalTableInsights(input));
        insights.AddRange(GenerateUnusedDataInsights(input));
        insights.AddRange(GenerateMigrationOpportunityInsights(input));

        progress?.Report(new DbIntelligenceInsightProgress(DbIntelligenceInsightStages.GeneratingInsights, 60));
        insights.AddRange(GenerateQualityRiskInsights(input));
        insights.AddRange(GenerateUnmappedEntityInsights(input));

        progress?.Report(new DbIntelligenceInsightProgress(DbIntelligenceInsightStages.EnrichingSemantic, 85));
        foreach (var insight in insights)
        {
            if (insight.SemanticMatchScore > 0 || string.IsNullOrWhiteSpace(insight.TableName))
                continue;

            var semantic = ComputeLocalSemanticMatch(insight.TableName!, insight.EntityType);
            if (semantic <= insight.SemanticMatchScore)
                continue;

            var idx = insights.FindIndex(i => i.Id == insight.Id);
            if (idx >= 0)
                insights[idx] = insight with { SemanticMatchScore = semantic };
        }

        var ranked = insights
            .OrderByDescending(i => i.PriorityScore)
            .ThenByDescending(i => i.ImpactScore)
            .ToList();

        progress?.Report(new DbIntelligenceInsightProgress(
            DbIntelligenceInsightStages.Completed, 100, $"{ranked.Count} insights"));

        return ranked;
    }

    internal static int ComputeLocalSemanticMatch(string tableName, BusinessEntityType? entityType)
    {
        if (entityType == null || entityType == BusinessEntityType.Unknown)
            return 0;

        if (!EntityTokens.TryGetValue(entityType.Value, out var tokens))
            return 0;

        var normalized = Normalize(tableName);
        var hits = tokens.Count(t => normalized.Contains(t, StringComparison.Ordinal));
        if (hits == 0) return 35;
        return Math.Min(95, 45 + hits * 18);
    }

    internal static int ComputePriority(int impact, int effort, int confidence) =>
        Math.Clamp((impact * confidence / 100) - effort / 3, 1, 100);

    private IEnumerable<DbIntelligenceInsightDto> GenerateCriticalTableInsights(DbIntelligenceInsightBuildInput input)
    {
        var healthByEntity = input.HealthScores.ToDictionary(h => h.EntityType, h => h.Score);

        foreach (var mapping in input.ConfirmedMappings)
        {
            var catalog = input.CatalogTables.FirstOrDefault(c =>
                c.SchemaName.Equals(mapping.SchemaName, StringComparison.OrdinalIgnoreCase) &&
                c.TableName.Equals(mapping.TableName, StringComparison.OrdinalIgnoreCase));
            if (catalog == null) continue;

            var fanOut = catalog.IncomingFkCount + catalog.OutgoingFkCount;
            var health = healthByEntity.GetValueOrDefault(mapping.EntityType, input.GlobalHealthScore);
            if (fanOut < 3 || catalog.EstimatedRowCount < 500 || health >= 75)
                continue;

            var impact = Math.Clamp(40 + fanOut * 8 + (100 - health) / 2, 50, 98);
            var confidence = Math.Clamp(mapping.ConfidencePercent, 60, 99);
            var effort = Math.Clamp(30 + fanOut * 3, 25, 80);

            yield return CreateInsight(
                DbIntelligenceInsightType.CriticalTable,
                DbIntelligenceInsightCategory.Risk,
                $"Critical table: {mapping.DisplayName}",
                $"{mapping.DisplayName} connects many business areas ({fanOut} relationships, {catalog.EstimatedRowCount:N0} records) and has a health score of {health}. Changes here affect revenue and customer data.",
                [
                    $"{fanOut} foreign-key relationships",
                    $"{catalog.EstimatedRowCount:N0} estimated records",
                    $"Health score {health}/100"
                ],
                [
                    $"Table {mapping.SchemaName}.{mapping.TableName} is referenced by multiple business flows",
                    $"Low health score ({health}) increases operational risk",
                    $"Confirmed as {mapping.EntityType} with {mapping.ConfidencePercent}% confidence"
                ],
                "Review data quality on this table first, then schedule a controlled sync or cleanup.",
                impact, effort, confidence,
                mapping.EntityType, mapping.SchemaName, mapping.TableName,
                ComputeLocalSemanticMatch(mapping.TableName, mapping.EntityType));
        }
    }

    private IEnumerable<DbIntelligenceInsightDto> GenerateUnusedDataInsights(DbIntelligenceInsightBuildInput input)
    {
        foreach (var table in input.CatalogTables.Where(t => !t.IsMapped))
        {
            if (table.IncomingFkCount + table.OutgoingFkCount > 0 || table.EstimatedRowCount == 0)
                continue;

            yield return CreateInsight(
                DbIntelligenceInsightType.UnusedData,
                DbIntelligenceInsightCategory.Recommendation,
                $"Unused table: {table.TableName}",
                $"Table {table.TableName} has {table.EstimatedRowCount:N0} rows but no mapped business meaning and no relationships to confirmed entities.",
                [
                    "0 foreign-key relationships",
                    $"{table.EstimatedRowCount:N0} rows",
                    "Not mapped to a business entity"
                ],
                [
                    "Orphan tables often indicate legacy modules or abandoned integrations",
                    "No inbound or outbound relationships were detected in the catalog"
                ],
                "Confirm with the business owner whether this table can be archived or excluded from sync.",
                Math.Clamp(35 + (int)Math.Min(table.EstimatedRowCount / 100, 40), 35, 75),
                20, 82,
                null, table.SchemaName, table.TableName,
                ComputeLocalSemanticMatch(table.TableName, null));
        }

        foreach (var table in input.CatalogTables.Where(t => t.IsMapped && t.TotalColumnCount > 0))
        {
            var nullRatio = table.NullableColumnCount * 100 / table.TotalColumnCount;
            if (nullRatio < 80 || table.EstimatedRowCount < 100)
                continue;

            yield return CreateInsight(
                DbIntelligenceInsightType.UnusedData,
                DbIntelligenceInsightCategory.Risk,
                $"Sparse data in {table.TableName}",
                $"{nullRatio}% of columns in {table.TableName} allow null values, suggesting incomplete or unused fields.",
                [
                    $"{table.NullableColumnCount}/{table.TotalColumnCount} nullable columns",
                    $"{table.EstimatedRowCount:N0} rows"
                ],
                [
                    "High nullable-column ratio often means fields are never populated",
                    "Incomplete records reduce CRM sync quality"
                ],
                "Identify mandatory business fields and enforce them before syncing to AutonomusCRM.",
                Math.Clamp(45 + nullRatio / 4, 45, 85),
                35, 78,
                null, table.SchemaName, table.TableName,
                ComputeLocalSemanticMatch(table.TableName, null));
        }

        foreach (var table in input.CatalogTables.Where(t => t.IsMapped && !t.HasUpdatedAtColumn && t.EstimatedRowCount > 1000))
        {
            yield return CreateInsight(
                DbIntelligenceInsightType.UnusedData,
                DbIntelligenceInsightCategory.Recommendation,
                $"Stale-data risk: {table.TableName}",
                $"Table {table.TableName} has no updated_at/modified_at column, making delta sync and freshness checks unreliable.",
                [
                    "No timestamp column detected",
                    $"{table.EstimatedRowCount:N0} rows"
                ],
                [
                    "Delta sync relies on modification timestamps or watermarks",
                    "Without change tracking, only full sync is safe"
                ],
                "Add a modification timestamp column or enable CDC before scheduling delta sync.",
                55, 50, 80,
                null, table.SchemaName, table.TableName, 0);
        }
    }

    private IEnumerable<DbIntelligenceInsightDto> GenerateMigrationOpportunityInsights(DbIntelligenceInsightBuildInput input)
    {
        var customerLike = input.ConfirmedMappings
            .Where(m => m.EntityType is BusinessEntityType.Customer or BusinessEntityType.Company)
            .ToList();
        if (customerLike.Count < 2)
            yield break;

        var tableNames = string.Join(", ", customerLike.Select(m => m.TableName));
        yield return CreateInsight(
            DbIntelligenceInsightType.MigrationOpportunity,
            DbIntelligenceInsightCategory.Opportunity,
            "Unify customer tables",
            $"We detected {customerLike.Count} tables mapped to customers or companies ({tableNames}). Consolidating them would simplify CRM sync.",
            customerLike.Select(m => $"{m.TableName} → {m.EntityType} ({m.ConfidencePercent}%)").ToList(),
            [
                "Multiple customer sources create duplicate CRM records",
                "AutonomusCRM works best with one canonical customer entity",
                $"Tables involved: {tableNames}"
            ],
            "Pick a master customer table, map others as legacy sources, and plan a merge in Database Sync.",
            Math.Clamp(60 + customerLike.Count * 10, 65, 95),
            Math.Clamp(40 + customerLike.Count * 8, 45, 90),
            Math.Clamp((int)Math.Round(customerLike.Average(m => m.ConfidencePercent)), 70, 95),
            BusinessEntityType.Customer, null, null,
            ComputeLocalSemanticMatch(customerLike[0].TableName, BusinessEntityType.Customer));
    }

    private IEnumerable<DbIntelligenceInsightDto> GenerateQualityRiskInsights(DbIntelligenceInsightBuildInput input)
    {
        foreach (var finding in input.HealthFindings.OrderByDescending(f => f.Severity).ThenByDescending(f => f.AffectedCount))
        {
            var category = finding.Category switch
            {
                DataHealthFindingCategory.Duplicate => DbIntelligenceInsightCategory.Risk,
                DataHealthFindingCategory.Orphan => DbIntelligenceInsightCategory.Risk,
                DataHealthFindingCategory.BrokenRelationship => DbIntelligenceInsightCategory.Risk,
                DataHealthFindingCategory.BusinessInconsistency => DbIntelligenceInsightCategory.Risk,
                _ => DbIntelligenceInsightCategory.Recommendation
            };

            var impact = finding.Severity switch
            {
                DataHealthFindingSeverity.Critical => 92,
                DataHealthFindingSeverity.High => 80,
                DataHealthFindingSeverity.Medium => 65,
                _ => 50
            };

            yield return CreateInsight(
                DbIntelligenceInsightType.QualityRisk,
                category,
                finding.Title,
                finding.Explanation,
                [finding.Evidence, $"{finding.AffectedCount} records affected", finding.BusinessImpact],
                [
                    finding.Explanation,
                    $"Severity: {finding.Severity}",
                    $"Recommendation: {finding.Recommendation}"
                ],
                finding.Recommendation,
                impact,
                finding.Severity == DataHealthFindingSeverity.Critical ? 55 : 40,
                Math.Clamp(impact - 5, 60, 95),
                finding.EntityType,
                finding.SchemaName,
                finding.TableName,
                finding.EntityType.HasValue
                    ? ComputeLocalSemanticMatch(finding.TableName ?? finding.Title, finding.EntityType)
                    : 0);
        }
    }

    private IEnumerable<DbIntelligenceInsightDto> GenerateUnmappedEntityInsights(DbIntelligenceInsightBuildInput input)
    {
        foreach (var table in input.UnmappedTables
                     .Where(t => t.Status != DbBusinessMappingStatus.Ignored &&
                                 t.InferredEntityType is not null and not BusinessEntityType.Unknown &&
                                 t.ConfidencePercent >= 65))
        {
            var entity = table.InferredEntityType!.Value;
            yield return CreateInsight(
                DbIntelligenceInsightType.UnmappedEntity,
                DbIntelligenceInsightCategory.Opportunity,
                $"Unconfirmed {EntityLabel(entity)} table",
                $"Table {table.TableName} looks like {EntityLabel(entity)} ({table.ConfidencePercent}% confidence) but is not confirmed yet.",
                [
                    $"{table.ConfidencePercent}% inference confidence",
                    $"{table.EstimatedRowCount:N0} rows",
                    .. table.InferenceReasons.Take(3)
                ],
                table.InferenceReasons.Count > 0
                    ? table.InferenceReasons
                    :
                    [
                        $"Naming pattern matches {EntityLabel(entity)}",
                        "Business discovery inferred this entity but awaits confirmation"
                    ],
                $"Open Understand and confirm {table.TableName} as {EntityLabel(entity)} to include it in health, graph and sync.",
                Math.Clamp(50 + table.ConfidencePercent / 3, 55, 90),
                25,
                table.ConfidencePercent,
                entity,
                table.SchemaName,
                table.TableName,
                ComputeLocalSemanticMatch(table.TableName, entity));
        }
    }

    private static DbIntelligenceInsightDto CreateInsight(
        string type, string category, string title, string summary,
        IReadOnlyList<string> evidence, IReadOnlyList<string> explainability,
        string suggestedAction, int impact, int effort, int confidence,
        BusinessEntityType? entityType, string? schema, string? table,
        int semanticMatch) => new(
        Guid.NewGuid(),
        type,
        category,
        title,
        summary,
        evidence,
        explainability,
        suggestedAction,
        impact,
        effort,
        confidence,
        semanticMatch,
        ComputePriority(impact, effort, confidence),
        entityType,
        schema,
        table,
        DateTime.UtcNow);

    private static string EntityLabel(BusinessEntityType type) => type switch
    {
        BusinessEntityType.Customer => "Customers",
        BusinessEntityType.Company => "Companies",
        BusinessEntityType.Contact => "Contacts",
        BusinessEntityType.Sale => "Sales",
        BusinessEntityType.Invoice => "Invoices",
        BusinessEntityType.Payment => "Payments",
        BusinessEntityType.Product => "Products",
        BusinessEntityType.Activity => "Activities",
        _ => type.ToString()
    };

    private static string Normalize(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
}
