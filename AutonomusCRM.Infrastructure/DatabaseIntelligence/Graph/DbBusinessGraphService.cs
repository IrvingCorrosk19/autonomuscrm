using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Graph;

public sealed class DbBusinessGraphService : IDbBusinessGraphService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IBusinessDiscoveryService _businessDiscovery;
    private readonly IDataHealthService _health;
    private readonly IDbBusinessGraphBuilder _builder;
    private readonly IDbIntelligenceAuditService _audit;
    private readonly IDbIntelligenceGraphProgressNotifier _notifier;
    private readonly IKnowledgeGraphRepository _graphRepository;
    private readonly IBusinessMemoryRepository _memoryRepository;

    public DbBusinessGraphService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IBusinessDiscoveryService businessDiscovery,
        IDataHealthService health,
        IDbBusinessGraphBuilder builder,
        IDbIntelligenceAuditService audit,
        IDbIntelligenceGraphProgressNotifier notifier,
        IKnowledgeGraphRepository graphRepository,
        IBusinessMemoryRepository memoryRepository)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _businessDiscovery = businessDiscovery;
        _health = health;
        _builder = builder;
        _audit = audit;
        _notifier = notifier;
        _graphRepository = graphRepository;
        _memoryRepository = memoryRepository;
    }

    public async Task<DbBusinessGraphResultDto> BuildGraphAsync(
        Guid tenantId, Guid userId, Guid connectionId, BuildDbBusinessGraphRequest? request,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var connection = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Connection not found.");

        var snapshot = await _db.DbCatalogSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Run physical discovery before building the business graph.");

        var mappings = await _businessDiscovery.ListMappingsAsync(tenantId, connectionId, cancellationToken);
        if (mappings.Count == 0)
            throw new InvalidOperationException("Run business discovery before building the business graph.");

        var job = new DbBusinessGraphJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshot.Id,
            CreatedByUserId = userId,
            Status = DbBusinessGraphJobStatus.Running,
            Stage = DbBusinessGraphStages.BuildingGraph,
            ProgressPercent = 5,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.DbBusinessGraphJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.GraphBuildStarted, userId, connectionId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        await _notifier.NotifyGraphBuildStartedAsync(tenantId, job.Id, connectionId, cancellationToken);

        try
        {
            var input = await BuildInputAsync(
                tenantId, connectionId, snapshot.Id, mappings, request, cancellationToken);

            var progress = new Progress<DbBusinessGraphProgress>(p =>
            {
                job.Stage = p.Stage;
                job.ProgressPercent = p.ProgressPercent;
                _ = _notifier.NotifyGraphBuildProgressAsync(tenantId, job.Id, p, cancellationToken);
            });

            var graph = _builder.Build(input, progress);
            graph = graph with { GraphJobId = job.Id };

            await RemoveExistingGraphDataAsync(tenantId, connectionId, cancellationToken);
            await PersistGraphAsync(tenantId, connectionId, snapshot.Id, job.Id, graph, cancellationToken);
            await PersistKnowledgeGraphAsync(tenantId, connectionId, graph, cancellationToken);
            await PersistBusinessMemoryAsync(tenantId, connectionId, graph, cancellationToken);

            job.Status = DbBusinessGraphJobStatus.Completed;
            job.Stage = DbBusinessGraphStages.Completed;
            job.ProgressPercent = 100;
            job.NodeCount = graph.Nodes.Count;
            job.EdgeCount = graph.Edges.Count;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.GraphBuildCompleted, userId, connectionId,
                connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
                connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

            await _notifier.NotifyGraphBuildCompletedAsync(
                tenantId, job.Id, graph.Nodes.Count, graph.Edges.Count, cancellationToken);

            return new DbBusinessGraphResultDto(MapJob(job), graph);
        }
        catch (Exception ex)
        {
            job.Status = DbBusinessGraphJobStatus.Failed;
            job.ErrorMessage = ex.Message.Length > 512 ? ex.Message[..512] : ex.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.GraphBuildFailed, userId, connectionId,
                connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
                connection.DatabaseName, false, ipAddress, userAgent, job.ErrorMessage), cancellationToken);

            await _notifier.NotifyGraphBuildFailedAsync(tenantId, job.Id, job.ErrorMessage, cancellationToken);
            throw;
        }
    }

    public async Task<DbBusinessGraphDto?> GetGraphAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        return await LoadLatestGraphAsync(tenantId, connectionId, cancellationToken);
    }

    public async Task<IReadOnlyList<DbBusinessGraphNodeDto>> GetNodesAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var graph = await GetGraphAsync(tenantId, connectionId, cancellationToken);
        return graph?.Nodes ?? Array.Empty<DbBusinessGraphNodeDto>();
    }

    public async Task<IReadOnlyList<DbBusinessGraphEdgeDto>> GetEdgesAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var graph = await GetGraphAsync(tenantId, connectionId, cancellationToken);
        return graph?.Edges ?? Array.Empty<DbBusinessGraphEdgeDto>();
    }

    public async Task<DbBusinessGraphSummaryDto?> GetSummaryAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var graph = await GetGraphAsync(tenantId, connectionId, cancellationToken);
        return graph?.Summary;
    }

    public async Task<DbBusinessGraphJobDto?> GetGraphJobAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbBusinessGraphJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);
        return job == null ? null : MapJob(job);
    }

    public async Task<DbBusinessGraphExportResultDto> ExportGraphAsync(
        Guid tenantId, Guid userId, Guid connectionId, string format,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var graph = await LoadLatestGraphAsync(tenantId, connectionId, cancellationToken)
            ?? throw new KeyNotFoundException("No business graph available. Build the graph first.");

        var normalized = (format ?? string.Empty).Trim().ToLowerInvariant();
        var connection = await _db.DbConnectionProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.GraphExported, userId, connectionId,
            connection?.EngineType, connection != null ? DbIntelligenceMasking.MaskHost(connection.Host) : null,
            connection?.DatabaseName, true, ipAddress, userAgent,
            $"format={normalized}"), cancellationToken);

        return normalized switch
        {
            DbBusinessGraphExportFormat.Png => DbBusinessGraphExporter.ExportPng(graph),
            DbBusinessGraphExportFormat.Pdf => DbBusinessGraphExporter.ExportPdf(graph),
            DbBusinessGraphExportFormat.Snapshot => DbBusinessGraphExporter.ExportSnapshot(graph),
            _ => throw new DbIntelligenceValidationException($"Unsupported export format '{format}'.")
        };
    }

    private async Task<DbBusinessGraphBuildInput> BuildInputAsync(
        Guid tenantId,
        Guid connectionId,
        Guid snapshotId,
        IReadOnlyList<DbTableBusinessMappingDto> mappings,
        BuildDbBusinessGraphRequest? request,
        CancellationToken cancellationToken)
    {
        var tables = await _db.DbCatalogTables.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.ConnectionProfileId == connectionId && t.SnapshotId == snapshotId)
            .ToListAsync(cancellationToken);

        var rowCounts = tables.ToDictionary(
            t => $"{t.SchemaName}.{t.ObjectName}".ToLowerInvariant(),
            t => t.EstimatedRowCount ?? 0L);

        var relationships = await _db.DbCatalogRelationships.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.ConnectionProfileId == connectionId && r.SnapshotId == snapshotId)
            .Select(r => new DbBusinessGraphRelationshipContext
            {
                FromSchema = r.FromSchema,
                FromTable = r.FromTable,
                FromColumn = r.FromColumn,
                ToSchema = r.ToSchema,
                ToTable = r.ToTable,
                ToColumn = r.ToColumn,
                ConfidencePercent = r.ConfidencePercent
            })
            .ToListAsync(cancellationToken);

        var health = await _health.GetLatestHealthResultAsync(tenantId, connectionId, cancellationToken);

        return new DbBusinessGraphBuildInput
        {
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            IncludeProducts = request?.IncludeProducts ?? true,
            IncludeActivities = request?.IncludeActivities ?? true,
            Mappings = mappings
                .Where(m => m.Status != DbBusinessMappingStatus.Ignored)
                .Select(m => new DbBusinessGraphMappingContext
                {
                    MappingId = m.Id,
                    SchemaName = m.SchemaName,
                    TableName = m.TableName,
                    DisplayName = m.Reasons.FirstOrDefault() ?? DbBusinessGraphBuilder.EntityLabel(m.EffectiveEntityType),
                    EntityType = m.EffectiveEntityType,
                    ConfidencePercent = m.ConfidencePercent,
                    Status = m.Status,
                    EstimatedRowCount = rowCounts.GetValueOrDefault($"{m.SchemaName}.{m.TableName}".ToLowerInvariant())
                })
                .ToList(),
            Relationships = relationships,
            HealthScores = health?.Scores.ToList() ?? [],
            HealthFindings = health?.Findings.ToList() ?? []
        };
    }

    private async Task PersistGraphAsync(
        Guid tenantId, Guid connectionId, Guid snapshotId, Guid jobId,
        DbBusinessGraphDto graph, CancellationToken cancellationToken)
    {
        var snapshot = new DbBusinessGraphSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            GraphJobId = jobId,
            GraphJson = JsonSerializer.Serialize(graph, JsonOptions),
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.DbBusinessGraphSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistKnowledgeGraphAsync(
        Guid tenantId, Guid connectionId, DbBusinessGraphDto graph, CancellationToken cancellationToken)
    {
        foreach (var node in graph.Nodes)
        {
            foreach (var source in node.Sources)
            {
                await _graphRepository.AddEdgeAsync(
                    BusinessKnowledgeGraphEdge.Link(
                        tenantId,
                        "DatabaseTable",
                        source.MappingId,
                        DbBusinessGraphNodeTypes.DipBusinessEntity,
                        node.Id,
                        "MapsTo",
                        node.ConfidencePercent / 100m,
                        new Dictionary<string, object>
                        {
                            ["entityType"] = node.EntityType.ToString(),
                            ["connectionId"] = connectionId.ToString(),
                            ["businessLabel"] = node.Label,
                            ["confidence"] = node.ConfidencePercent
                        }),
                    cancellationToken);
            }
        }

        foreach (var edge in graph.Edges)
        {
            await _graphRepository.AddEdgeAsync(
                BusinessKnowledgeGraphEdge.Link(
                    tenantId,
                    DbBusinessGraphNodeTypes.DipBusinessEntity,
                    edge.FromNodeId,
                    DbBusinessGraphNodeTypes.DipBusinessEntity,
                    edge.ToNodeId,
                    edge.EdgeType,
                    edge.ConfidencePercent / 100m,
                    new Dictionary<string, object>
                    {
                        ["businessLabel"] = edge.BusinessLabel,
                        ["connectionId"] = connectionId.ToString(),
                        ["fromEntity"] = edge.FromEntityType.ToString(),
                        ["toEntity"] = edge.ToEntityType.ToString()
                    }),
                cancellationToken);
        }
    }

    private async Task PersistBusinessMemoryAsync(
        Guid tenantId, Guid connectionId, DbBusinessGraphDto graph, CancellationToken cancellationToken)
    {
        var episodeKey = $"dbi-graph-{connectionId:N}";
        var root = await _memoryRepository.GetByEpisodeKeyAsync(tenantId, episodeKey, cancellationToken);
        if (root == null)
        {
            root = BusinessMemoryRoot.CreateEpisode(
                tenantId,
                "DatabaseConnection",
                connectionId,
                episodeKey,
                "Business graph snapshot",
                $"Graph with {graph.Nodes.Count} business areas and {graph.Edges.Count} relationships",
                importance: 8,
                sourceChannel: "database_intelligence",
                tags: ["business_graph", connectionId.ToString()]);

            await _memoryRepository.AddMemoryAsync(root, cancellationToken);
        }

        await _memoryRepository.AddFactAsync(
            BusinessMemoryFact.Create(
                root.Id,
                tenantId,
                "BusinessGraph",
                $"{graph.Nodes.Count} nodes, {graph.Edges.Count} edges, health {graph.Summary.GlobalHealthScore}",
                graph.Summary.GlobalHealthScore / 100.0),
            cancellationToken);
    }

    private async Task RemoveExistingGraphDataAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        var existing = await _db.DbBusinessGraphSnapshots
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.DbBusinessGraphSnapshots.RemoveRange(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<DbBusinessGraphDto?> LoadLatestGraphAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        var stored = await _db.DbBusinessGraphSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (stored == null || string.IsNullOrWhiteSpace(stored.GraphJson))
            return null;

        return JsonSerializer.Deserialize<DbBusinessGraphDto>(stored.GraphJson, JsonOptions);
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    private static DbBusinessGraphJobDto MapJob(DbBusinessGraphJob job) => new(
        job.Id,
        job.TenantId,
        job.ConnectionProfileId,
        job.SnapshotId,
        job.Status,
        job.Stage,
        job.ProgressPercent,
        job.ErrorMessage,
        job.CreatedAtUtc,
        job.StartedAtUtc,
        job.CompletedAtUtc);
}
