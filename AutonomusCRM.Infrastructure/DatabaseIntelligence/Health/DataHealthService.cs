using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Health;

public sealed class DataHealthService : IDataHealthService
{
    private const int SampleRowLimit = 500;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IBusinessDiscoveryService _businessDiscovery;
    private readonly IDbSchemaDiscoveryService _schemaDiscovery;
    private readonly IDbConnectionVault _vault;
    private readonly DbBusinessSampleReader _sampleReader;
    private readonly IDataHealthEngine _engine;
    private readonly IDbIntelligenceAuditService _audit;
    private readonly IDbIntelligenceHealthProgressNotifier _notifier;

    public DataHealthService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IBusinessDiscoveryService businessDiscovery,
        IDbSchemaDiscoveryService schemaDiscovery,
        IDbConnectionVault vault,
        DbBusinessSampleReader sampleReader,
        IDataHealthEngine engine,
        IDbIntelligenceAuditService audit,
        IDbIntelligenceHealthProgressNotifier notifier)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _businessDiscovery = businessDiscovery;
        _schemaDiscovery = schemaDiscovery;
        _vault = vault;
        _sampleReader = sampleReader;
        _engine = engine;
        _audit = audit;
        _notifier = notifier;
    }

    public async Task<DataHealthResultDto> RunHealthScanAsync(
        Guid tenantId, Guid userId, Guid connectionId, string scanMode,
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
            ?? throw new InvalidOperationException("Run physical discovery before health scan.");

        var mappings = await _businessDiscovery.ListMappingsAsync(tenantId, connectionId, cancellationToken);
        if (mappings.Count == 0)
            throw new InvalidOperationException("Run business discovery before health scan.");

        var job = new DataHealthJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshot.Id,
            CreatedByUserId = userId,
            Status = DataHealthJobStatus.Running,
            ScanMode = string.IsNullOrWhiteSpace(scanMode) ? DataHealthScanMode.Full : scanMode,
            Stage = DataHealthStages.ScanningCustomers,
            ProgressPercent = 5,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.DataHealthJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.HealthScanStarted, userId, connectionId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        await _notifier.NotifyHealthScanStartedAsync(tenantId, job.Id, connectionId, cancellationToken);

        try
        {
            var input = await BuildScanInputAsync(
                tenantId, connectionId, snapshot.Id, connection, mappings, job.ScanMode, cancellationToken);

            var progress = new Progress<DataHealthProgress>(p =>
            {
                job.Stage = p.Stage;
                job.ProgressPercent = p.ProgressPercent;
                _ = _notifier.NotifyHealthScanProgressAsync(tenantId, job.Id, p, cancellationToken);
            });

            var result = _engine.Scan(input, progress);
            await RemoveExistingHealthDataAsync(tenantId, snapshot.Id, cancellationToken);
            await PersistResultsAsync(job, result, cancellationToken);

            job.Status = result.Findings.Any(f => f.Severity == DataHealthFindingSeverity.Critical)
                ? DataHealthJobStatus.CompletedWithWarnings
                : DataHealthJobStatus.Completed;
            job.Stage = DataHealthStages.Completed;
            job.ProgressPercent = 100;
            job.GlobalScore = result.GlobalScore;
            job.FindingsCount = result.Findings.Count;
            job.CriticalFindings = result.Findings.Count(f => f.Severity == DataHealthFindingSeverity.Critical);
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.HealthScanCompleted, userId, connectionId,
                connection.EngineType, null, connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

            await _notifier.NotifyHealthScanCompletedAsync(tenantId, job.Id, result.GlobalScore, result.Findings.Count, cancellationToken);

            return ToResultDto(job, result.Scores, result.Findings);
        }
        catch (Exception ex)
        {
            job.Status = DataHealthJobStatus.Failed;
            job.ErrorMessage = DbConnectionStringValidator.SanitizeErrorMessage(ex.Message);
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.HealthScanFailed, userId, connectionId,
                null, null, null, false, ipAddress, userAgent, job.ErrorMessage), cancellationToken);

            await _notifier.NotifyHealthScanFailedAsync(tenantId, job.Id, job.ErrorMessage, cancellationToken);
            throw;
        }
    }

    public async Task<DataHealthJobDto?> GetHealthJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DataHealthJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);
        return job == null ? null : ToJobDto(job);
    }

    public async Task<DataHealthResultDto?> GetLatestHealthResultAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DataHealthJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.ConnectionProfileId == connectionId &&
                        (j.Status == DataHealthJobStatus.Completed || j.Status == DataHealthJobStatus.CompletedWithWarnings))
            .OrderByDescending(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job == null)
        {
            job = await _db.DataHealthJobs.AsNoTracking()
                .Where(j => j.TenantId == tenantId && j.ConnectionProfileId == connectionId)
                .OrderByDescending(j => j.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }
        if (job == null) return null;

        var scores = await _db.DataHealthScores.AsNoTracking()
            .Where(s => s.HealthJobId == job.Id).ToListAsync(cancellationToken);
        var findings = await _db.DataHealthFindings.AsNoTracking()
            .Where(f => f.HealthJobId == job.Id)
            .OrderByDescending(f => f.Severity == DataHealthFindingSeverity.Critical)
            .ThenByDescending(f => f.AffectedCount)
            .ToListAsync(cancellationToken);

        return ToResultDto(job, scores.Select(ToScoreDto).ToList(), findings.Select(ToFindingDto).ToList());
    }

    public async Task<IReadOnlyList<DataHealthFindingDto>> ListFindingsAsync(
        Guid tenantId, Guid? connectionId = null, string? severity = null,
        CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var query = _db.DataHealthFindings.AsNoTracking().Where(f => f.TenantId == tenantId);
        if (connectionId.HasValue)
            query = query.Where(f => f.ConnectionProfileId == connectionId.Value);
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(f => f.Severity == severity);

        var items = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
        return items.Select(ToFindingDto).ToList();
    }

    internal async Task<DataHealthScanInput> BuildScanInputAsync(
        Guid tenantId,
        Guid connectionId,
        Guid snapshotId,
        DbConnectionProfile connection,
        IReadOnlyList<DbTableBusinessMappingDto> mappings,
        string scanMode,
        CancellationToken cancellationToken)
    {
        var columns = await _schemaDiscovery.ListCatalogColumnsAsync(tenantId, connectionId, cancellationToken);
        var relationships = await _schemaDiscovery.ListCatalogRelationshipsAsync(tenantId, connectionId, cancellationToken);

        var mappingLookup = mappings
            .Where(m => m.Status != DbBusinessMappingStatus.Ignored)
            .ToDictionary(m => $"{m.SchemaName}.{m.TableName}", m => m.EffectiveEntityType, StringComparer.OrdinalIgnoreCase);

        var input = new DataHealthScanInput
        {
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            SnapshotId = snapshotId,
            ScanMode = scanMode,
            Relationships = relationships.Select(r =>
            {
                var fromKey = $"{r.FromSchema}.{r.FromTable}";
                var toKey = $"{r.ToSchema}.{r.ToTable}";
                mappingLookup.TryGetValue(fromKey, out var fromEt);
                mappingLookup.TryGetValue(toKey, out var toEt);
                return new DataHealthRelationshipContext
                {
                    FromSchema = r.FromSchema,
                    FromTable = r.FromTable,
                    FromColumn = r.FromColumn,
                    ToSchema = r.ToSchema,
                    ToTable = r.ToTable,
                    ToColumn = r.ToColumn,
                    FromEntity = fromEt,
                    ToEntity = toEt
                };
            }).ToList()
        };

        var tablesToScan = scanMode == DataHealthScanMode.Incremental
            ? mappings.Where(m => m.Status == DbBusinessMappingStatus.Inferred).Take(10).ToList()
            : mappings.Where(m => m.Status != DbBusinessMappingStatus.Ignored).ToList();

        try
        {
            var secrets = _vault.Decrypt(connection.EncryptedConnectionBlob);
            foreach (var mapping in tablesToScan)
                await AddTableContextAsync(input, columns, connection, secrets, mapping, cancellationToken);
        }
        catch
        {
            foreach (var mapping in tablesToScan)
                AddTableContextFromCatalog(input, columns, mapping, Array.Empty<IReadOnlyDictionary<string, string?>>());
        }

        return input;
    }

    private async Task AddTableContextAsync(
        DataHealthScanInput input,
        IReadOnlyList<DbCatalogColumnDto> columns,
        DbConnectionProfile connection,
        DbConnectionSecrets secrets,
        DbTableBusinessMappingDto mapping,
        CancellationToken cancellationToken)
    {
        var tableCols = columns
            .Where(c => c.SchemaName == mapping.SchemaName && c.ObjectName == mapping.TableName)
            .Select(c => c.ColumnName).ToList();

        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows;
        try
        {
            rows = await _sampleReader.ReadTopNAsync(
                connection, secrets, mapping.SchemaName, mapping.TableName,
                tableCols, SampleRowLimit, 15, cancellationToken);
        }
        catch
        {
            rows = Array.Empty<IReadOnlyDictionary<string, string?>>();
        }

        AddTableContextFromCatalog(input, columns, mapping, rows);
    }

    private static void AddTableContextFromCatalog(
        DataHealthScanInput input,
        IReadOnlyList<DbCatalogColumnDto> columns,
        DbTableBusinessMappingDto mapping,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows)
    {
        input.Tables.Add(new DataHealthTableContext
        {
            SchemaName = mapping.SchemaName,
            TableName = mapping.TableName,
            EntityType = mapping.EffectiveEntityType,
            Columns = columns
                .Where(c => c.SchemaName == mapping.SchemaName && c.ObjectName == mapping.TableName)
                .Select(c => new DataHealthColumnContext
                {
                    ColumnName = c.ColumnName,
                    DataType = c.DataType,
                    IsPrimaryKey = c.IsPrimaryKey,
                    IsForeignKey = c.IsForeignKey
                }).ToList(),
            Rows = rows
        });
    }

    private async Task RemoveExistingHealthDataAsync(Guid tenantId, Guid snapshotId, CancellationToken ct)
    {
        var oldFindings = await _db.DataHealthFindings.Where(f => f.TenantId == tenantId && f.SnapshotId == snapshotId).ToListAsync(ct);
        var oldScores = await _db.DataHealthScores.Where(s => s.TenantId == tenantId && s.SnapshotId == snapshotId).ToListAsync(ct);
        if (oldFindings.Count > 0) _db.DataHealthFindings.RemoveRange(oldFindings);
        if (oldScores.Count > 0) _db.DataHealthScores.RemoveRange(oldScores);
        if (oldFindings.Count > 0 || oldScores.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private async Task PersistResultsAsync(DataHealthJob job, DataHealthScanResult result, CancellationToken ct)
    {
        foreach (var f in result.Findings)
        {
            _db.DataHealthFindings.Add(new DataHealthFinding
            {
                Id = f.Id,
                TenantId = job.TenantId,
                ConnectionProfileId = job.ConnectionProfileId,
                SnapshotId = job.SnapshotId,
                HealthJobId = job.Id,
                EntityType = f.EntityType,
                Severity = f.Severity,
                Category = f.Category,
                Title = f.Title,
                Explanation = f.Explanation,
                BusinessImpact = f.BusinessImpact,
                Evidence = f.Evidence,
                Recommendation = f.Recommendation,
                SchemaName = f.SchemaName,
                TableName = f.TableName,
                AffectedCount = f.AffectedCount
            });
        }

        foreach (var s in result.Scores)
        {
            _db.DataHealthScores.Add(new DataHealthScore
            {
                Id = Guid.NewGuid(),
                TenantId = job.TenantId,
                ConnectionProfileId = job.ConnectionProfileId,
                SnapshotId = job.SnapshotId,
                HealthJobId = job.Id,
                EntityType = s.EntityType,
                Score = s.Score,
                CompletenessScore = s.CompletenessScore,
                ValidityScore = s.ValidityScore,
                ConsistencyScore = s.ConsistencyScore,
                DuplicateScore = s.DuplicateScore
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    private static DataHealthResultDto ToResultDto(
        DataHealthJob job, IReadOnlyList<DataHealthScoreDto> scores, IReadOnlyList<DataHealthFindingDto> findings) =>
        new(ToJobDto(job), scores, findings);

    private static DataHealthJobDto ToJobDto(DataHealthJob job) => new(
        job.Id, job.TenantId, job.ConnectionProfileId, job.SnapshotId,
        job.Status, job.ScanMode, job.Stage, job.ProgressPercent,
        job.GlobalScore, DataHealthScoreBand.Label(job.GlobalScore),
        job.FindingsCount, job.CriticalFindings, job.ErrorMessage,
        job.CreatedAtUtc, job.StartedAtUtc, job.CompletedAtUtc);

    private static DataHealthScoreDto ToScoreDto(DataHealthScore s) => new(
        s.EntityType, s.Score, DataHealthScoreBand.Label(s.Score),
        s.CompletenessScore, s.ValidityScore, s.ConsistencyScore, s.DuplicateScore);

    private static DataHealthFindingDto ToFindingDto(DataHealthFinding f) => new(
        f.Id, f.EntityType, f.Severity, f.Category, f.Title, f.Explanation,
        f.BusinessImpact, f.Evidence, f.Recommendation, f.SchemaName, f.TableName, f.AffectedCount);
}
