using System.Text.Json;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Insights;

public sealed class DbIntelligenceInsightService : IDbIntelligenceInsightService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IBusinessDiscoveryService _businessDiscovery;
    private readonly IDbIntelligenceInsightEngine _engine;
    private readonly DbIntelligenceInsightSemanticEnhancer _semantic;
    private readonly IDbIntelligenceAuditService _audit;

    public DbIntelligenceInsightService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IBusinessDiscoveryService businessDiscovery,
        IDbIntelligenceInsightEngine engine,
        DbIntelligenceInsightSemanticEnhancer semantic,
        IDbIntelligenceAuditService audit)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _businessDiscovery = businessDiscovery;
        _engine = engine;
        _semantic = semantic;
        _audit = audit;
    }

    public async Task<DbIntelligenceInsightResultDto> GenerateInsightsAsync(
        Guid tenantId, Guid userId, Guid connectionId,
        GenerateDbIntelligenceInsightsRequest? request,
        string? ipAddress, string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        request ??= new GenerateDbIntelligenceInsightsRequest();

        var connection = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Connection not found.");

        var snapshot = await _db.DbCatalogSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Run physical discovery before generating insights.");

        var mappings = await _businessDiscovery.ListMappingsAsync(tenantId, connectionId, cancellationToken);
        if (mappings.Count == 0)
            throw new InvalidOperationException("Run business discovery before generating insights.");

        var job = new DbIntelligenceInsightJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshot.Id,
            CreatedByUserId = userId,
            Status = DbIntelligenceInsightJobStatus.Running,
            Stage = DbIntelligenceInsightStages.AnalyzingCatalog,
            ProgressPercent = 5,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.DbIntelligenceInsightJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.InsightsGenerationStarted, userId, connectionId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        try
        {
            var input = await BuildInputAsync(tenantId, connectionId, snapshot.Id, mappings, cancellationToken);
            var progress = new Progress<DbIntelligenceInsightProgress>(p =>
            {
                job.Stage = p.Stage;
                job.ProgressPercent = p.ProgressPercent;
            });

            var generated = _engine.Generate(input, progress).ToList();
            if (request.IncludeSemanticEnrichment)
                generated = (await _semantic.EnrichAsync(tenantId, generated, cancellationToken)).ToList();

            await RemoveExistingInsightsAsync(tenantId, connectionId, cancellationToken);
            PersistInsights(job, generated);
            job.InsightCount = generated.Count;
            job.Status = DbIntelligenceInsightJobStatus.Completed;
            job.Stage = DbIntelligenceInsightStages.Completed;
            job.ProgressPercent = 100;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.InsightsGenerationCompleted, userId, connectionId,
                connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
                connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

            return new DbIntelligenceInsightResultDto(MapJob(job), generated);
        }
        catch (Exception ex)
        {
            job.Status = DbIntelligenceInsightJobStatus.Failed;
            job.ErrorMessage = ex.Message.Length > 512 ? ex.Message[..512] : ex.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.InsightsGenerationFailed, userId, connectionId,
                connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
                connection.DatabaseName, false, ipAddress, userAgent, ex.Message), cancellationToken);
            throw;
        }
    }

    public async Task<DbIntelligenceInsightResultDto?> GetLatestInsightsAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbIntelligenceInsightJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId &&
                        j.ConnectionProfileId == connectionId &&
                        j.Status == DbIntelligenceInsightJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job == null) return null;

        var insights = await ListInsightsForJobAsync(tenantId, job.Id, cancellationToken);
        return new DbIntelligenceInsightResultDto(MapJob(job), insights);
    }

    public async Task<DbIntelligenceInsightJobDto?> GetInsightJobAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbIntelligenceInsightJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);
        return job == null ? null : MapJob(job);
    }

    public async Task<IReadOnlyList<DbIntelligenceInsightDto>> ListInsightsAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var latest = await _db.DbIntelligenceInsightJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId &&
                        j.ConnectionProfileId == connectionId &&
                        j.Status == DbIntelligenceInsightJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAtUtc)
            .Select(j => j.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest == Guid.Empty) return Array.Empty<DbIntelligenceInsightDto>();
        return await ListInsightsForJobAsync(tenantId, latest, cancellationToken);
    }

    private async Task<DbIntelligenceInsightBuildInput> BuildInputAsync(
        Guid tenantId, Guid connectionId, Guid snapshotId,
        IReadOnlyList<DbTableBusinessMappingDto> mappings,
        CancellationToken cancellationToken)
    {
        var tables = await _db.DbCatalogTables.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.SnapshotId == snapshotId &&
                        t.ObjectType == DbCatalogObjectTypes.Table)
            .ToListAsync(cancellationToken);

        var columns = await _db.DbCatalogColumns.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.SnapshotId == snapshotId)
            .ToListAsync(cancellationToken);

        var relationships = await _db.DbCatalogRelationships.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.SnapshotId == snapshotId)
            .ToListAsync(cancellationToken);

        var confirmed = mappings
            .Where(m => m.Status == DbBusinessMappingStatus.Confirmed)
            .Select(m => new DbBusinessGraphMappingContext
            {
                MappingId = m.Id,
                SchemaName = m.SchemaName,
                TableName = m.TableName,
                DisplayName = BusinessEntityDisplayName(m.InferredEntityType),
                EntityType = m.InferredEntityType,
                ConfidencePercent = m.ConfidencePercent,
                Status = m.Status,
                EstimatedRowCount = tables.FirstOrDefault(t =>
                    t.SchemaName == m.SchemaName && t.ObjectName == m.TableName)?.EstimatedRowCount ?? 0
            }).ToList();

        var confirmedKeys = confirmed
            .Select(m => Key(m.SchemaName, m.TableName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unmapped = mappings
            .Where(m => m.Status != DbBusinessMappingStatus.Confirmed)
            .Select(m => new DbIntelligenceUnmappedTableContext
            {
                SchemaName = m.SchemaName,
                TableName = m.TableName,
                InferredEntityType = m.InferredEntityType,
                ConfidencePercent = m.ConfidencePercent,
                Status = m.Status,
                EstimatedRowCount = tables.FirstOrDefault(t =>
                    t.SchemaName == m.SchemaName && t.ObjectName == m.TableName)?.EstimatedRowCount ?? 0,
                InferenceReasons = m.Reasons.ToList()
            }).ToList();

        var fkIncoming = relationships.GroupBy(r => Key(r.ToSchema, r.ToTable))
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var fkOutgoing = relationships.GroupBy(r => Key(r.FromSchema, r.FromTable))
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var catalogTables = tables.Select(t =>
        {
            var key = Key(t.SchemaName, t.ObjectName);
            var tableColumns = columns.Where(c => c.SchemaName == t.SchemaName && c.ObjectName == t.ObjectName).ToList();
            return new DbIntelligenceCatalogTableContext
            {
                SchemaName = t.SchemaName,
                TableName = t.ObjectName,
                EstimatedRowCount = t.EstimatedRowCount ?? 0,
                IncomingFkCount = fkIncoming.GetValueOrDefault(key),
                OutgoingFkCount = fkOutgoing.GetValueOrDefault(key),
                NullableColumnCount = tableColumns.Count(c => c.IsNullable),
                TotalColumnCount = tableColumns.Count,
                HasUpdatedAtColumn = tableColumns.Any(c =>
                    c.ColumnName.Contains("updated", StringComparison.OrdinalIgnoreCase) ||
                    c.ColumnName.Contains("modified", StringComparison.OrdinalIgnoreCase)),
                IsMapped = confirmedKeys.Contains(key)
            };
        }).ToList();

        var graphRelationships = relationships.Select(r => new DbBusinessGraphRelationshipContext
        {
            FromSchema = r.FromSchema,
            FromTable = r.FromTable,
            FromColumn = r.FromColumn,
            ToSchema = r.ToSchema,
            ToTable = r.ToTable,
            ToColumn = r.ToColumn,
            ConfidencePercent = r.ConfidencePercent
        }).ToList();

        var latestHealth = await _db.DataHealthJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId &&
                        j.ConnectionProfileId == connectionId &&
                        j.SnapshotId == snapshotId &&
                        j.Status == DataHealthJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var healthScores = latestHealth == null
            ? []
            : await _db.DataHealthScores.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.HealthJobId == latestHealth.Id)
                .Select(s => new DataHealthScoreDto(
                    s.EntityType, s.Score, DataHealthScoreBand.Label(s.Score), s.CompletenessScore, s.ValidityScore,
                    s.ConsistencyScore, s.DuplicateScore))
                .ToListAsync(cancellationToken);

        var healthFindings = latestHealth == null
            ? []
            : await _db.DataHealthFindings.AsNoTracking()
                .Where(f => f.TenantId == tenantId && f.HealthJobId == latestHealth.Id)
                .Select(f => new DataHealthFindingDto(
                    f.Id, f.EntityType, f.Severity, f.Category, f.Title, f.Explanation,
                    f.BusinessImpact, f.Evidence, f.Recommendation, f.SchemaName, f.TableName, f.AffectedCount))
                .ToListAsync(cancellationToken);

        return new DbIntelligenceInsightBuildInput
        {
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            ConfirmedMappings = confirmed,
            UnmappedTables = unmapped,
            CatalogTables = catalogTables,
            Relationships = graphRelationships,
            HealthScores = healthScores,
            HealthFindings = healthFindings,
            GlobalHealthScore = latestHealth?.GlobalScore ?? 100
        };
    }

    private async Task RemoveExistingInsightsAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        var existing = await _db.DbIntelligenceInsights
            .Where(i => i.TenantId == tenantId && i.ConnectionProfileId == connectionId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.DbIntelligenceInsights.RemoveRange(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private void PersistInsights(DbIntelligenceInsightJob job, IReadOnlyList<DbIntelligenceInsightDto> insights)
    {
        foreach (var dto in insights)
        {
            _db.DbIntelligenceInsights.Add(new DbIntelligenceInsight
            {
                Id = dto.Id,
                JobId = job.Id,
                TenantId = job.TenantId,
                ConnectionProfileId = job.ConnectionProfileId,
                Type = dto.Type,
                Category = dto.Category,
                Title = dto.Title,
                Summary = dto.Summary,
                EvidenceJson = JsonSerializer.Serialize(dto.Evidence, JsonOptions),
                ExplainabilityJson = JsonSerializer.Serialize(dto.ExplainabilityReasons, JsonOptions),
                SuggestedAction = dto.SuggestedAction,
                ImpactScore = dto.ImpactScore,
                EffortScore = dto.EffortScore,
                ConfidencePercent = dto.ConfidencePercent,
                SemanticMatchScore = dto.SemanticMatchScore,
                PriorityScore = dto.PriorityScore,
                EntityType = dto.EntityType,
                SchemaName = dto.SchemaName,
                TableName = dto.TableName,
                CreatedAtUtc = dto.CreatedAtUtc
            });
        }
    }

    private async Task<IReadOnlyList<DbIntelligenceInsightDto>> ListInsightsForJobAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        var rows = await _db.DbIntelligenceInsights.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.JobId == jobId)
            .OrderByDescending(i => i.PriorityScore)
            .ToListAsync(cancellationToken);
        return rows.Select(MapInsight).ToList();
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    private static string Key(string schema, string table) => $"{schema}.{table}";

    private static string BusinessEntityDisplayName(BusinessEntityType type) => type switch
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

    internal static DbIntelligenceInsightJobDto MapJob(DbIntelligenceInsightJob job) => new(
        job.Id, job.TenantId, job.ConnectionProfileId, job.SnapshotId,
        job.Status, job.Stage, job.ProgressPercent, job.InsightCount,
        job.ErrorMessage, job.CreatedAtUtc, job.StartedAtUtc, job.CompletedAtUtc);

    internal static DbIntelligenceInsightDto MapInsight(DbIntelligenceInsight row)
    {
        var evidence = JsonSerializer.Deserialize<List<string>>(row.EvidenceJson, JsonOptions) ?? [];
        var explain = JsonSerializer.Deserialize<List<string>>(row.ExplainabilityJson, JsonOptions) ?? [];
        return new DbIntelligenceInsightDto(
            row.Id, row.Type, row.Category, row.Title, row.Summary,
            evidence, explain, row.SuggestedAction,
            row.ImpactScore, row.EffortScore, row.ConfidencePercent, row.SemanticMatchScore,
            row.PriorityScore, row.EntityType, row.SchemaName, row.TableName, row.CreatedAtUtc);
    }
}
