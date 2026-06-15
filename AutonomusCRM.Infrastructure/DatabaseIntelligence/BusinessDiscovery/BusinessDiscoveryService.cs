using System.Text.Json;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;

public sealed class BusinessDiscoveryService : IBusinessDiscoveryService
{
    private const int SampleRowLimit = 25;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IDbSchemaDiscoveryService _schemaDiscovery;
    private readonly IDbConnectionVault _vault;
    private readonly IBusinessEntityInferenceEngine _inferenceEngine;
    private readonly DbBusinessSampleReader _sampleReader;
    private readonly IDbIntelligenceAuditService _audit;
    private readonly IDbIntelligenceBusinessProgressNotifier _notifier;
    private readonly IBusinessMemoryRepository _memoryRepository;
    private readonly IKnowledgeGraphRepository _graphRepository;

    public BusinessDiscoveryService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IDbSchemaDiscoveryService schemaDiscovery,
        IDbConnectionVault vault,
        IBusinessEntityInferenceEngine inferenceEngine,
        DbBusinessSampleReader sampleReader,
        IDbIntelligenceAuditService audit,
        IDbIntelligenceBusinessProgressNotifier notifier,
        IBusinessMemoryRepository memoryRepository,
        IKnowledgeGraphRepository graphRepository)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _schemaDiscovery = schemaDiscovery;
        _vault = vault;
        _inferenceEngine = inferenceEngine;
        _sampleReader = sampleReader;
        _audit = audit;
        _notifier = notifier;
        _memoryRepository = memoryRepository;
        _graphRepository = graphRepository;
    }

    public async Task<BusinessDiscoveryResultDto> RunBusinessDiscoveryAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);

        var connection = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Connection not found.");

        var snapshot = await _db.DbCatalogSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Run physical discovery before business discovery.");

        var job = new DbBusinessDiscoveryJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshot.Id,
            CreatedByUserId = userId,
            Status = DbDiscoveryJobStatus.Running,
            Stage = BusinessDiscoveryStages.AnalyzingTables,
            ProgressPercent = 5,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.DbBusinessDiscoveryJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.BusinessDiscoveryStarted, userId, connectionId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        await _notifier.NotifyBusinessDiscoveryStartedAsync(tenantId, job.Id, connectionId, cancellationToken);

        try
        {
            var catalog = await BuildCatalogInputAsync(tenantId, connectionId, snapshot.Id, connection, cancellationToken);
            var progress = new Progress<BusinessDiscoveryProgress>(p =>
            {
                job.Stage = p.Stage;
                job.ProgressPercent = p.ProgressPercent;
                _ = _notifier.NotifyBusinessDiscoveryProgressAsync(tenantId, job.Id, p, cancellationToken);
            });

            var inferences = _inferenceEngine.InferFromCatalog(catalog, progress);

            await RemoveExistingMappingsAsync(tenantId, snapshot.Id, cancellationToken);

            var mappings = new List<DbTableBusinessMapping>();
            foreach (var inference in inferences)
            {
                mappings.Add(new DbTableBusinessMapping
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ConnectionProfileId = connectionId,
                    SnapshotId = snapshot.Id,
                    BusinessDiscoveryJobId = job.Id,
                    SchemaName = inference.SchemaName,
                    TableName = inference.TableName,
                    InferredEntityType = inference.EntityType,
                    ConfidencePercent = inference.ConfidencePercent,
                    ExplanationJson = BusinessDiscoveryMappingSerializer.SerializeReasons(inference.Reasons),
                    Status = DbBusinessMappingStatus.Inferred,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            _db.DbTableBusinessMappings.AddRange(mappings);

            job.Status = DbDiscoveryJobStatus.Completed;
            job.Stage = BusinessDiscoveryStages.Completed;
            job.ProgressPercent = 100;
            job.TablesAnalyzed = inferences.Count;
            job.EntitiesDetected = inferences.Count(i => i.EntityType != BusinessEntityType.Unknown);
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.BusinessDiscoveryCompleted, userId, connectionId,
                connection.EngineType, null, connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

            await _notifier.NotifyBusinessDiscoveryCompletedAsync(tenantId, job.Id, mappings.Count, cancellationToken);

            return ToResultDto(job, mappings);
        }
        catch (Exception ex)
        {
            job.Status = DbDiscoveryJobStatus.Failed;
            job.ErrorMessage = DbConnectionStringValidator.SanitizeErrorMessage(ex.Message);
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.BusinessDiscoveryFailed, userId, connectionId,
                null, null, null, false, ipAddress, userAgent, job.ErrorMessage), cancellationToken);

            await _notifier.NotifyBusinessDiscoveryFailedAsync(tenantId, job.Id, job.ErrorMessage, cancellationToken);
            throw;
        }
    }

    public async Task<BusinessDiscoveryResultDto?> GetLatestBusinessDiscoveryAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbBusinessDiscoveryJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.ConnectionProfileId == connectionId && j.Status == DbDiscoveryJobStatus.Completed)
            .OrderByDescending(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job == null)
        {
            job = await _db.DbBusinessDiscoveryJobs.AsNoTracking()
                .Where(j => j.TenantId == tenantId && j.ConnectionProfileId == connectionId)
                .OrderByDescending(j => j.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }
        if (job == null) return null;

        var mappings = await _db.DbTableBusinessMappings.AsNoTracking()
            .Where(m => m.BusinessDiscoveryJobId == job.Id)
            .OrderByDescending(m => m.ConfidencePercent)
            .ToListAsync(cancellationToken);

        return ToResultDto(job, mappings);
    }

    public async Task<IReadOnlyList<DbTableBusinessMappingDto>> ListMappingsAsync(
        Guid tenantId, Guid? connectionId = null, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var query = _db.DbTableBusinessMappings.AsNoTracking().Where(m => m.TenantId == tenantId);
        if (connectionId.HasValue)
            query = query.Where(m => m.ConnectionProfileId == connectionId.Value);

        var items = await query
            .OrderByDescending(m => m.UpdatedAtUtc)
            .ThenByDescending(m => m.ConfidencePercent)
            .ToListAsync(cancellationToken);

        return items.Select(ToMappingDto).ToList();
    }

    public async Task<DbTableBusinessMappingDto> ConfirmMappingAsync(
        Guid tenantId,
        Guid userId,
        ConfirmBusinessMappingRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var mapping = await _db.DbTableBusinessMappings
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == request.MappingId, cancellationToken)
            ?? throw new KeyNotFoundException("Mapping not found.");

        var action = request.Action.Trim();
        mapping.UpdatedAtUtc = DateTime.UtcNow;
        mapping.ConfirmedByUserId = userId;
        mapping.ConfirmedAtUtc = DateTime.UtcNow;

        switch (action.ToLowerInvariant())
        {
            case "confirm":
                mapping.Status = DbBusinessMappingStatus.Confirmed;
                mapping.ConfirmedEntityType = mapping.InferredEntityType;
                break;
            case "correct":
                if (request.CorrectedEntityType == null)
                    throw new DbIntelligenceValidationException("Corrected entity type is required.");
                mapping.Status = DbBusinessMappingStatus.Corrected;
                mapping.ConfirmedEntityType = request.CorrectedEntityType;
                break;
            case "ignore":
                mapping.Status = DbBusinessMappingStatus.Ignored;
                mapping.ConfirmedEntityType = BusinessEntityType.Unknown;
                break;
            default:
                throw new DbIntelligenceValidationException("Action must be Confirm, Correct, or Ignore.");
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (mapping.Status is DbBusinessMappingStatus.Confirmed or DbBusinessMappingStatus.Corrected)
            await PersistBusinessMemoryAsync(tenantId, userId, mapping, cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.BusinessMappingConfirmed, userId, mapping.ConnectionProfileId,
            null, null,
            $"{mapping.SchemaName}.{mapping.TableName}", true, ipAddress, userAgent), cancellationToken);

        return ToMappingDto(mapping);
    }

    private async Task<BusinessDiscoveryCatalogInput> BuildCatalogInputAsync(
        Guid tenantId,
        Guid connectionId,
        Guid snapshotId,
        DbConnectionProfile connection,
        CancellationToken cancellationToken)
    {
        var tables = await _schemaDiscovery.ListCatalogTablesAsync(tenantId, connectionId, cancellationToken);
        var columns = await _schemaDiscovery.ListCatalogColumnsAsync(tenantId, connectionId, cancellationToken);
        var relationships = await _schemaDiscovery.ListCatalogRelationshipsAsync(tenantId, connectionId, cancellationToken);

        var input = new BusinessDiscoveryCatalogInput
        {
            Tables = tables
                .Where(t => t.ObjectType == DbCatalogObjectTypes.Table)
                .Select(t => new BusinessDiscoveryTableInput
                {
                    SchemaName = t.SchemaName,
                    TableName = t.ObjectName,
                    ObjectType = t.ObjectType,
                    EstimatedRowCount = t.EstimatedRowCount
                }).ToList(),
            Columns = columns.Select(c => new BusinessDiscoveryColumnInput
            {
                SchemaName = c.SchemaName,
                TableName = c.ObjectName,
                ColumnName = c.ColumnName,
                DataType = c.DataType,
                IsPrimaryKey = c.IsPrimaryKey,
                IsForeignKey = c.IsForeignKey,
                IsIndexed = c.IsIndexed
            }).ToList(),
            Relationships = relationships.Select(r => new BusinessDiscoveryRelationshipInput
            {
                FromSchema = r.FromSchema,
                FromTable = r.FromTable,
                FromColumn = r.FromColumn,
                ToSchema = r.ToSchema,
                ToTable = r.ToTable,
                ToColumn = r.ToColumn
            }).ToList()
        };

        if (connection.IsReadOnly)
        {
            try
            {
                var secrets = _vault.Decrypt(connection.EncryptedConnectionBlob);
                foreach (var table in input.Tables.Take(40))
                {
                    var tableColumns = input.Columns
                        .Where(c => c.SchemaName == table.SchemaName && c.TableName == table.TableName)
                        .Select(c => c.ColumnName)
                        .ToList();

                    try
                    {
                        var rows = await _sampleReader.ReadTopNAsync(
                            connection, secrets, table.SchemaName, table.TableName,
                            tableColumns, SampleRowLimit, 15, cancellationToken);
                        input.SampleRowsByTableKey[BusinessDiscoveryCatalogInput.TableKey(table.SchemaName, table.TableName)] = rows;
                    }
                    catch
                    {
                        // Sample optional per table.
                    }
                }
            }
            catch
            {
                // Invalid vault blob or unreachable DB — catalog-only inference still applies.
            }
        }

        return input;
    }

    private async Task RemoveExistingMappingsAsync(Guid tenantId, Guid snapshotId, CancellationToken cancellationToken)
    {
        var existing = await _db.DbTableBusinessMappings
            .Where(m => m.TenantId == tenantId && m.SnapshotId == snapshotId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.DbTableBusinessMappings.RemoveRange(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task PersistBusinessMemoryAsync(
        Guid tenantId, Guid userId, DbTableBusinessMapping mapping, CancellationToken cancellationToken)
    {
        var entityType = mapping.ConfirmedEntityType ?? mapping.InferredEntityType;
        var reasons = BusinessDiscoveryMappingSerializer.DeserializeReasons(mapping.ExplanationJson);
        var episodeKey = $"dbi-{mapping.ConnectionProfileId:N}-{mapping.SchemaName}.{mapping.TableName}".ToLowerInvariant();

        var existing = await _memoryRepository.GetByEpisodeKeyAsync(tenantId, episodeKey, cancellationToken);
        if (existing != null) return;

        var episode = BusinessMemoryRoot.CreateEpisode(
            tenantId,
            "DatabaseConnection",
            mapping.ConnectionProfileId,
            episodeKey,
            $"{mapping.SchemaName}.{mapping.TableName} → {BusinessEntitySignals.DisplayName(entityType)}",
            BusinessEntitySignals.BuildExplanation(entityType, mapping.ConfidencePercent, reasons.ToList()),
            importance: 7,
            sourceChannel: "database_intelligence",
            tags: ["business_discovery", entityType.ToString(), mapping.SchemaName, mapping.TableName]);

        await _memoryRepository.AddMemoryAsync(episode, cancellationToken);
        await _memoryRepository.AddFactAsync(
            BusinessMemoryFact.Create(
                episode.Id,
                tenantId,
                "TableBusinessEntity",
                $"{mapping.SchemaName}.{mapping.TableName}={entityType}",
                mapping.ConfidencePercent / 100.0),
            cancellationToken);

        await _graphRepository.AddEdgeAsync(
            BusinessKnowledgeGraphEdge.Link(
                tenantId,
                "DatabaseTable",
                mapping.Id,
                "BusinessEntity",
                mapping.Id,
                "MapsTo",
                mapping.ConfidencePercent / 100m,
                new Dictionary<string, object>
                {
                    ["entityType"] = entityType.ToString(),
                    ["schema"] = mapping.SchemaName,
                    ["table"] = mapping.TableName,
                    ["confirmedBy"] = userId.ToString()
                }),
            cancellationToken);
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    private static BusinessDiscoveryResultDto ToResultDto(DbBusinessDiscoveryJob job, IReadOnlyList<DbTableBusinessMapping> mappings) =>
        new(
            job.Id,
            job.TenantId,
            job.ConnectionProfileId,
            job.SnapshotId,
            job.Status,
            job.ProgressPercent,
            job.TablesAnalyzed,
            job.EntitiesDetected,
            mappings.Select(ToMappingDto).ToList(),
            job.CreatedAtUtc,
            job.CompletedAtUtc);

    private static DbTableBusinessMappingDto ToMappingDto(DbTableBusinessMapping mapping)
    {
        var effective = mapping.ConfirmedEntityType ?? mapping.InferredEntityType;
        return new DbTableBusinessMappingDto(
            mapping.Id,
            mapping.TenantId,
            mapping.ConnectionProfileId,
            mapping.SnapshotId,
            mapping.SchemaName,
            mapping.TableName,
            mapping.InferredEntityType,
            effective,
            mapping.ConfidencePercent,
            BusinessDiscoveryMappingSerializer.DeserializeReasons(mapping.ExplanationJson),
            mapping.Status,
            mapping.ConfirmedByUserId,
            mapping.ConfirmedAtUtc,
            mapping.CreatedAtUtc,
            mapping.UpdatedAtUtc);
    }
}
