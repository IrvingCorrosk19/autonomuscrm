using System.Security.Cryptography;
using System.Text;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Graph;

public sealed class DbBusinessGraphBuilder : IDbBusinessGraphBuilder
{
    private static readonly (BusinessEntityType From, BusinessEntityType To, string EdgeType, string Label)[] CanonicalFlow =
    [
        (BusinessEntityType.Company, BusinessEntityType.Contact, DbBusinessGraphEdgeTypes.HasContacts, "has contacts"),
        (BusinessEntityType.Customer, BusinessEntityType.Contact, DbBusinessGraphEdgeTypes.HasContacts, "has contacts"),
        (BusinessEntityType.Contact, BusinessEntityType.Sale, DbBusinessGraphEdgeTypes.GeneratedSale, "generated sale"),
        (BusinessEntityType.Customer, BusinessEntityType.Sale, DbBusinessGraphEdgeTypes.GeneratedSale, "generated sale"),
        (BusinessEntityType.Sale, BusinessEntityType.Invoice, DbBusinessGraphEdgeTypes.GeneratedInvoice, "generated invoice"),
        (BusinessEntityType.Customer, BusinessEntityType.Invoice, DbBusinessGraphEdgeTypes.GeneratedInvoice, "generated invoice"),
        (BusinessEntityType.Invoice, BusinessEntityType.Payment, DbBusinessGraphEdgeTypes.GeneratedPayment, "generated payment"),
        (BusinessEntityType.Sale, BusinessEntityType.Product, DbBusinessGraphEdgeTypes.PurchasedProduct, "purchased product"),
        (BusinessEntityType.Contact, BusinessEntityType.Activity, DbBusinessGraphEdgeTypes.HasActivity, "has activity")
    ];

    public DbBusinessGraphDto Build(
        DbBusinessGraphBuildInput input,
        IProgress<DbBusinessGraphProgress>? progress = null)
    {
        progress?.Report(new DbBusinessGraphProgress(DbBusinessGraphStages.BuildingGraph, 5));

        var activeMappings = input.Mappings
            .Where(m => m.Status != DbBusinessMappingStatus.Ignored)
            .Where(m => ShouldIncludeEntity(m.EntityType, input))
            .ToList();

        var tableEntity = activeMappings.ToDictionary(
            m => TableKey(m.SchemaName, m.TableName),
            m => m.EntityType);

        progress?.Report(new DbBusinessGraphProgress(DbBusinessGraphStages.CreatingNodes, 25));

        var nodes = BuildNodes(input, activeMappings);
        var nodeByEntity = nodes.ToDictionary(n => n.EntityType);

        progress?.Report(new DbBusinessGraphProgress(DbBusinessGraphStages.CreatingRelationships, 55));

        var edges = BuildEdges(input, tableEntity, nodeByEntity, activeMappings);

        progress?.Report(new DbBusinessGraphProgress(DbBusinessGraphStages.CalculatingMetrics, 80));

        nodes = AttachRelationshipCounts(nodes, edges);
        var summary = BuildSummary(input, nodes, edges);

        progress?.Report(new DbBusinessGraphProgress(DbBusinessGraphStages.Completed, 100,
            $"{nodes.Count} nodes, {edges.Count} relationships"));

        return new DbBusinessGraphDto(
            input.ConnectionProfileId,
            input.SnapshotId,
            null,
            nodes,
            edges,
            summary,
            DateTime.UtcNow);
    }

    private static bool ShouldIncludeEntity(BusinessEntityType entityType, DbBusinessGraphBuildInput input) =>
        entityType switch
        {
            BusinessEntityType.Product => input.IncludeProducts,
            BusinessEntityType.Activity => input.IncludeActivities,
            BusinessEntityType.Unknown or BusinessEntityType.User => false,
            _ => true
        };

    private static List<DbBusinessGraphNodeDto> BuildNodes(
        DbBusinessGraphBuildInput input,
        List<DbBusinessGraphMappingContext> mappings)
    {
        var grouped = mappings.GroupBy(m => m.EntityType);
        var nodes = new List<DbBusinessGraphNodeDto>();

        foreach (var group in grouped)
        {
            var entityType = group.Key;
            var sources = group
                .OrderByDescending(m => m.ConfidencePercent)
                .Select(m => new DbBusinessGraphSourceDto(
                    m.MappingId,
                    string.IsNullOrWhiteSpace(m.DisplayName) ? EntityLabel(entityType) : m.DisplayName,
                    m.ConfidencePercent,
                    m.SchemaName,
                    m.TableName))
                .ToList();

            var confidence = sources.Count > 0
                ? (int)Math.Round(sources.Average(s => s.ConfidencePercent))
                : 0;

            var health = input.HealthScores.FirstOrDefault(s => s.EntityType == entityType);
            var healthScore = health?.Score ?? 100;
            var healthBand = health?.Band ?? DataHealthScoreBand.Label(healthScore);

            var entityFindings = input.HealthFindings
                .Where(f => f.EntityType == entityType)
                .OrderByDescending(f => SeverityRank(f.Severity))
                .ThenByDescending(f => f.AffectedCount)
                .Take(5)
                .ToList();

            var duplicateCount = entityFindings
                .Count(f => f.Category == DataHealthFindingCategory.Duplicate);
            var orphanCount = entityFindings
                .Count(f => f.Category == DataHealthFindingCategory.Orphan);

            nodes.Add(new DbBusinessGraphNodeDto(
                CreateNodeId(input.ConnectionProfileId, entityType),
                entityType,
                EntityLabel(entityType),
                confidence,
                sources,
                healthScore,
                healthBand,
                DeriveRiskLevel(healthScore, entityFindings),
                group.Sum(m => m.EstimatedRowCount),
                0,
                duplicateCount,
                orphanCount,
                entityFindings));
        }

        return nodes.OrderBy(n => EntityOrder(n.EntityType)).ToList();
    }

    private static List<DbBusinessGraphEdgeDto> BuildEdges(
        DbBusinessGraphBuildInput input,
        Dictionary<string, BusinessEntityType> tableEntity,
        Dictionary<BusinessEntityType, DbBusinessGraphNodeDto> nodeByEntity,
        List<DbBusinessGraphMappingContext> mappings)
    {
        var edges = new List<DbBusinessGraphEdgeDto>();
        var edgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rel in input.Relationships)
        {
            var fromKey = TableKey(rel.FromSchema, rel.FromTable);
            var toKey = TableKey(rel.ToSchema, rel.ToTable);
            if (!tableEntity.TryGetValue(fromKey, out var fromEntity) ||
                !tableEntity.TryGetValue(toKey, out var toEntity))
                continue;
            if (!nodeByEntity.ContainsKey(fromEntity) || !nodeByEntity.ContainsKey(toEntity))
                continue;
            if (fromEntity == toEntity)
                continue;

            var edgeType = InferEdgeType(fromEntity, toEntity);
            if (edgeType == null)
                continue;

            var key = $"{fromEntity}:{toEntity}:{edgeType}";
            if (!edgeKeys.Add(key))
                continue;

            edges.Add(CreateEdge(
                input.ConnectionProfileId,
                nodeByEntity[fromEntity],
                nodeByEntity[toEntity],
                edgeType,
                EdgeLabel(edgeType),
                rel.ConfidencePercent));
        }

        foreach (var (from, to, edgeType, label) in CanonicalFlow)
        {
            if (!nodeByEntity.ContainsKey(from) || !nodeByEntity.ContainsKey(to))
                continue;

            var key = $"{from}:{to}:{edgeType}";
            if (!edgeKeys.Add(key))
                continue;

            var confidence = Math.Min(
                nodeByEntity[from].ConfidencePercent,
                nodeByEntity[to].ConfidencePercent);

            edges.Add(CreateEdge(
                input.ConnectionProfileId,
                nodeByEntity[from],
                nodeByEntity[to],
                edgeType,
                label,
                confidence));
        }

        return edges;
    }

    private static List<DbBusinessGraphNodeDto> AttachRelationshipCounts(
        List<DbBusinessGraphNodeDto> nodes,
        List<DbBusinessGraphEdgeDto> edges)
    {
        var counts = nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in edges)
        {
            if (counts.ContainsKey(edge.FromNodeId)) counts[edge.FromNodeId]++;
            if (counts.ContainsKey(edge.ToNodeId)) counts[edge.ToNodeId]++;
        }

        return nodes.Select(n => n with { RelationshipCount = counts.GetValueOrDefault(n.Id) }).ToList();
    }

    private static DbBusinessGraphSummaryDto BuildSummary(
        DbBusinessGraphBuildInput input,
        IReadOnlyList<DbBusinessGraphNodeDto> nodes,
        IReadOnlyList<DbBusinessGraphEdgeDto> edges)
    {
        var globalHealth = nodes.Count > 0
            ? (int)Math.Round(nodes.Average(n => n.HealthScore))
            : 100;

        var critical = input.HealthFindings.Count(f =>
            f.Severity == DataHealthFindingSeverity.Critical);

        return new DbBusinessGraphSummaryDto(
            input.ConnectionProfileId,
            input.SnapshotId,
            nodes.Count,
            edges.Count,
            globalHealth,
            DataHealthScoreBand.Label(globalHealth),
            nodes.Sum(n => (int)Math.Min(n.RecordCount, int.MaxValue)),
            nodes.Sum(n => n.DuplicateCount),
            nodes.Sum(n => n.OrphanCount),
            critical,
            "We show how your business works — not database tables.");
    }

    private static DbBusinessGraphEdgeDto CreateEdge(
        Guid connectionId,
        DbBusinessGraphNodeDto from,
        DbBusinessGraphNodeDto to,
        string edgeType,
        string label,
        int confidence) =>
        new(
            CreateEdgeId(connectionId, from.EntityType, to.EntityType, edgeType),
            from.Id,
            to.Id,
            from.EntityType,
            to.EntityType,
            edgeType,
            label,
            confidence,
            1);

    private static string? InferEdgeType(BusinessEntityType from, BusinessEntityType to)
    {
        foreach (var (f, t, edgeType, _) in CanonicalFlow)
        {
            if (f == from && t == to)
                return edgeType;
        }

        return null;
    }

    internal static Guid CreateNodeId(Guid connectionId, BusinessEntityType entityType) =>
        CreateDeterministicGuid($"dip-node:{connectionId}:{(int)entityType}");

    internal static Guid CreateEdgeId(Guid connectionId, BusinessEntityType from, BusinessEntityType to, string edgeType) =>
        CreateDeterministicGuid($"dip-edge:{connectionId}:{(int)from}:{(int)to}:{edgeType}");

    private static Guid CreateDeterministicGuid(string seed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static string TableKey(string schema, string table) =>
        $"{schema}.{table}".ToLowerInvariant();

    internal static string EntityLabel(BusinessEntityType type) => type switch
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

    private static int EntityOrder(BusinessEntityType type) => type switch
    {
        BusinessEntityType.Company => 1,
        BusinessEntityType.Customer => 2,
        BusinessEntityType.Contact => 3,
        BusinessEntityType.Sale => 4,
        BusinessEntityType.Invoice => 5,
        BusinessEntityType.Payment => 6,
        BusinessEntityType.Product => 7,
        BusinessEntityType.Activity => 8,
        _ => 99
    };

    private static string EdgeLabel(string edgeType) => edgeType switch
    {
        DbBusinessGraphEdgeTypes.HasContacts => "has contacts",
        DbBusinessGraphEdgeTypes.GeneratedSale => "generated sale",
        DbBusinessGraphEdgeTypes.GeneratedInvoice => "generated invoice",
        DbBusinessGraphEdgeTypes.GeneratedPayment => "generated payment",
        DbBusinessGraphEdgeTypes.PurchasedProduct => "purchased product",
        DbBusinessGraphEdgeTypes.HasActivity => "has activity",
        _ => edgeType
    };

    private static string DeriveRiskLevel(int healthScore, IReadOnlyList<DataHealthFindingDto> findings)
    {
        if (findings.Any(f => f.Severity == DataHealthFindingSeverity.Critical) || healthScore < 50)
            return "Critical";
        if (findings.Any(f => f.Severity == DataHealthFindingSeverity.High) || healthScore < 75)
            return "High";
        if (findings.Any(f => f.Severity == DataHealthFindingSeverity.Medium) || healthScore < 90)
            return "Medium";
        return "Low";
    }

    private static int SeverityRank(string severity) => severity switch
    {
        DataHealthFindingSeverity.Critical => 4,
        DataHealthFindingSeverity.High => 3,
        DataHealthFindingSeverity.Medium => 2,
        _ => 1
    };
}
