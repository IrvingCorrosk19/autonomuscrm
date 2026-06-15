using System.Text.Json;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

public sealed class DbSchemaDiscoveryService : IDbSchemaDiscoveryService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IDbConnectionVault _vault;
    private readonly DbSchemaIntrospectorRegistry _introspectors;
    private readonly IDbIntelligenceAuditService _audit;
    private readonly IDbIntelligenceProgressNotifier _notifier;
    private readonly DbIntelligenceSecurityOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public DbSchemaDiscoveryService(
        ApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IDbConnectionVault vault,
        DbSchemaIntrospectorRegistry introspectors,
        IDbIntelligenceAuditService audit,
        IDbIntelligenceProgressNotifier notifier,
        IOptions<DbIntelligenceSecurityOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _vault = vault;
        _introspectors = introspectors;
        _audit = audit;
        _notifier = notifier;
        _options = options.Value;
        _scopeFactory = scopeFactory;
    }

    private void ScopeToTenant(Guid tenantId) => _tenantAccessor.TenantId = tenantId;

    public async Task<DbDiscoveryJobDto> StartDiscoveryAsync(
        Guid tenantId, Guid userId, Guid connectionId,
        string? ipAddress, string? userAgent,         CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var connection = await GetActiveConnectionAsync(tenantId, connectionId, cancellationToken);
        var job = new DbDiscoveryJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            CreatedByUserId = userId,
            Status = DbDiscoveryJobStatus.Pending,
            ProgressPercent = 0,
            CreatedAtUtc = DateTime.UtcNow,
            LogsJson = JsonSerializer.Serialize(new[] { "Discovery queued." })
        };
        _db.DbDiscoveryJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.DiscoveryStarted, userId, connectionId,
            connection.EngineType, DbIntelligenceMasking.MaskHost(connection.Host),
            connection.DatabaseName, true, ipAddress, userAgent), cancellationToken);

        await _notifier.NotifyDiscoveryStartedAsync(tenantId, job.Id, connectionId, cancellationToken);
        return ToJobDto(job, ["Discovery queued."]);
    }

    public async Task<DbDiscoveryJobDto?> GetDiscoveryJobAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = await _db.DbDiscoveryJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken);
        return job == null ? null : ToJobDto(job, ParseLogs(job.LogsJson));
    }

    public async Task<DbCatalogSnapshotDto?> GetCatalogSnapshotAsync(
        Guid tenantId, Guid snapshotId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var snap = await _db.DbCatalogSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == snapshotId, cancellationToken);
        return snap == null ? null : ToSnapshotDto(snap);
    }

    public async Task<DbCatalogSnapshotDto?> GetLatestCatalogForConnectionAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var snap = await _db.DbCatalogSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return snap == null ? null : ToSnapshotDto(snap);
    }

    public async Task<(DbDiscoveryJobDto Job, DbCatalogSnapshotDto Snapshot)> DiscoverNowAsync(
        Guid tenantId, Guid userId, Guid connectionId,
        string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var job = new DbDiscoveryJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionProfileId = connectionId,
            CreatedByUserId = userId,
            Status = DbDiscoveryJobStatus.Running,
            ProgressPercent = 0,
            CreatedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow,
            LogsJson = JsonSerializer.Serialize(new[] { "Discovery started." })
        };
        _db.DbDiscoveryJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId, DbIntelligenceForensicActions.DiscoveryStarted, userId, connectionId,
            null, null, null, true, ipAddress, userAgent), cancellationToken);

        await _notifier.NotifyDiscoveryStartedAsync(tenantId, job.Id, connectionId, cancellationToken);

        try
        {
            var snapshot = await ExecuteDiscoveryAsync(job, cancellationToken);
            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.DiscoveryCompleted, userId, connectionId,
                null, null, null, true, ipAddress, userAgent), cancellationToken);
            await _notifier.NotifyDiscoveryCompletedAsync(tenantId, job.Id, snapshot.Id, cancellationToken);
            return (ToJobDto(job, ParseLogs(job.LogsJson)), ToSnapshotDto(snapshot));
        }
        catch (Exception ex)
        {
            job.Status = DbDiscoveryJobStatus.Failed;
            job.ErrorMessage = DbConnectionStringValidator.SanitizeErrorMessage(ex.Message);
            job.CompletedAtUtc = DateTime.UtcNow;
            AppendLog(job, job.ErrorMessage);
            await _db.SaveChangesAsync(cancellationToken);
            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                tenantId, DbIntelligenceForensicActions.DiscoveryFailed, userId, connectionId,
                null, null, null, false, ipAddress, userAgent, job.ErrorMessage), cancellationToken);
            await _notifier.NotifyDiscoveryFailedAsync(tenantId, job.Id, job.ErrorMessage, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<DbCatalogTableDto>> ListCatalogTablesAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var snap = await GetLatestSnapshotEntityAsync(tenantId, connectionId, cancellationToken);
        if (snap == null) return Array.Empty<DbCatalogTableDto>();

        var tables = await _db.DbCatalogTables.AsNoTracking()
            .Where(t => t.SnapshotId == snap.Id)
            .OrderBy(t => t.SchemaName).ThenBy(t => t.ObjectName)
            .ToListAsync(cancellationToken);
        var views = await _db.DbCatalogViews.AsNoTracking()
            .Where(v => v.SnapshotId == snap.Id)
            .OrderBy(v => v.SchemaName).ThenBy(v => v.ObjectName)
            .ToListAsync(cancellationToken);
        var columnCounts = await _db.DbCatalogColumns.AsNoTracking()
            .Where(c => c.SnapshotId == snap.Id)
            .GroupBy(c => new { c.SchemaName, c.ObjectName })
            .Select(g => new { g.Key.SchemaName, g.Key.ObjectName, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var pkSet = await _db.DbCatalogColumns.AsNoTracking()
            .Where(c => c.SnapshotId == snap.Id && c.IsPrimaryKey)
            .Select(c => new { c.SchemaName, c.ObjectName })
            .ToListAsync(cancellationToken);
        var pkLookup = pkSet.Select(p => $"{p.SchemaName}.{p.ObjectName}").ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = tables.Select(t =>
        {
            var key = $"{t.SchemaName}.{t.ObjectName}";
            var count = columnCounts.FirstOrDefault(c => c.SchemaName == t.SchemaName && c.ObjectName == t.ObjectName)?.Count ?? 0;
            return new DbCatalogTableDto(t.Id, t.SchemaName, t.ObjectName, t.ObjectType, t.EstimatedRowCount, count, pkLookup.Contains(key));
        }).ToList();

        result.AddRange(views.Select(v =>
        {
            var count = columnCounts.FirstOrDefault(c => c.SchemaName == v.SchemaName && c.ObjectName == v.ObjectName)?.Count ?? 0;
            return new DbCatalogTableDto(v.Id, v.SchemaName, v.ObjectName, DbCatalogObjectTypes.View, null, count, false);
        }));
        return result;
    }

    public async Task<IReadOnlyList<DbCatalogRelationshipDto>> ListCatalogRelationshipsAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var snap = await GetLatestSnapshotEntityAsync(tenantId, connectionId, cancellationToken);
        if (snap == null) return Array.Empty<DbCatalogRelationshipDto>();

        return await _db.DbCatalogRelationships.AsNoTracking()
            .Where(r => r.SnapshotId == snap.Id)
            .OrderByDescending(r => r.ConfidencePercent)
            .Select(r => new DbCatalogRelationshipDto(
                r.Id, r.FromSchema, r.FromTable, r.FromColumn,
                r.ToSchema, r.ToTable, r.ToColumn, r.Source, r.ConfidencePercent))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DbCatalogColumnDto>> ListCatalogColumnsAsync(
        Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        ScopeToTenant(tenantId);
        var snap = await GetLatestSnapshotEntityAsync(tenantId, connectionId, cancellationToken);
        if (snap == null) return Array.Empty<DbCatalogColumnDto>();

        return await _db.DbCatalogColumns.AsNoTracking()
            .Where(c => c.SnapshotId == snap.Id)
            .OrderBy(c => c.SchemaName).ThenBy(c => c.ObjectName).ThenBy(c => c.Ordinal)
            .Select(c => new DbCatalogColumnDto(
                c.Id, c.SchemaName, c.ObjectName, c.ColumnName, c.DataType,
                c.IsNullable, c.DefaultValue, c.IsPrimaryKey, c.IsForeignKey, c.IsIndexed, c.Ordinal))
            .ToListAsync(cancellationToken);
    }

    internal async Task ProcessPendingJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _db.DbDiscoveryJobs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null || job.Status != DbDiscoveryJobStatus.Pending) return;

        ScopeToTenant(job.TenantId);
        job.Status = DbDiscoveryJobStatus.Running;
        job.StartedAtUtc = DateTime.UtcNow;
        AppendLog(job, "Discovery worker started.");
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var snapshot = await ExecuteDiscoveryAsync(job, cancellationToken);
            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                job.TenantId, DbIntelligenceForensicActions.DiscoveryCompleted, job.CreatedByUserId,
                job.ConnectionProfileId, null, null, null, true), cancellationToken);
            await _notifier.NotifyDiscoveryCompletedAsync(job.TenantId, job.Id, snapshot.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            job.Status = DbDiscoveryJobStatus.Failed;
            job.ErrorMessage = DbConnectionStringValidator.SanitizeErrorMessage(ex.Message);
            job.CompletedAtUtc = DateTime.UtcNow;
            AppendLog(job, job.ErrorMessage);
            await _db.SaveChangesAsync(cancellationToken);
            await _audit.RecordAsync(new DbIntelligenceAuditEntry(
                job.TenantId, DbIntelligenceForensicActions.DiscoveryFailed, job.CreatedByUserId,
                job.ConnectionProfileId, null, null, null, false, ErrorMessage: job.ErrorMessage), cancellationToken);
            await _notifier.NotifyDiscoveryFailedAsync(job.TenantId, job.Id, job.ErrorMessage, cancellationToken);
        }
    }

    private async Task<DbCatalogSnapshot> ExecuteDiscoveryAsync(DbDiscoveryJob job, CancellationToken cancellationToken)
    {
        var connection = await GetActiveConnectionAsync(job.TenantId, job.ConnectionProfileId, cancellationToken);
        var secrets = _vault.Decrypt(connection.EncryptedConnectionBlob);
        var endpoint = new DbConnectionEndpoint(connection.Host, connection.Port, connection.DatabaseName, connection.Username);
        var introspector = _introspectors.Resolve(connection.EngineType);

        var progress = new Progress<DbDiscoveryProgress>(p =>
        {
            _ = ReportProgressAsync(job, p, cancellationToken);
        });

        var physical = await introspector.DiscoverAsync(
            endpoint, secrets, connection.IsReadOnly, _options.ConnectionTimeoutSeconds, progress, cancellationToken);

        var snapshot = await PersistCatalogAsync(job, connection, physical, cancellationToken);

        job.Status = physical.Warnings.Count > 0 ? DbDiscoveryJobStatus.CompletedWithWarnings : DbDiscoveryJobStatus.Completed;
        job.ProgressPercent = 100;
        job.CatalogSnapshotId = snapshot.Id;
        job.TablesDiscovered = physical.Tables.Count(t => t.ObjectType == DbCatalogObjectTypes.Table);
        job.ViewsDiscovered = physical.Tables.Count(t => t.ObjectType == DbCatalogObjectTypes.View);
        job.ColumnsDiscovered = physical.Columns.Count;
        job.RelationshipsDiscovered = physical.Relationships.Count;
        job.CompletedAtUtc = DateTime.UtcNow;
        AppendLog(job, $"Discovery completed: {job.TablesDiscovered} tables, {job.ViewsDiscovered} views.");
        await _db.SaveChangesAsync(cancellationToken);
        return snapshot;
    }

    private async Task ReportProgressAsync(DbDiscoveryJob job, DbDiscoveryProgress progress, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.TenantId = job.TenantId;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<IDbIntelligenceProgressNotifier>();
        var tracked = await db.DbDiscoveryJobs.IgnoreQueryFilters().FirstAsync(j => j.Id == job.Id, ct);
        tracked.ProgressPercent = progress.ProgressPercent;
        if (progress.Stage == "TableDiscovered")
        {
            tracked.TablesDiscovered++;
            if (progress.ObjectType == DbCatalogObjectTypes.View) tracked.ViewsDiscovered++;
        }
        AppendLog(tracked, progress.Message ?? progress.Stage);
        await db.SaveChangesAsync(ct);

        if (progress.Stage == "SchemaDiscovered" && progress.SchemaName != null)
            await notifier.NotifySchemaDiscoveredAsync(tracked.TenantId, tracked.Id, progress.SchemaName, ct);
        else if (progress.Stage == "TableDiscovered" && progress.SchemaName != null && progress.TableName != null)
            await notifier.NotifyTableDiscoveredAsync(tracked.TenantId, tracked.Id, progress.SchemaName, progress.TableName, progress.ObjectType ?? DbCatalogObjectTypes.Table, ct);
        else
            await notifier.NotifyDiscoveryProgressAsync(tracked.TenantId, tracked.Id, progress, ct);
    }

    private async Task<DbCatalogSnapshot> PersistCatalogAsync(
        DbDiscoveryJob job, DbConnectionProfile connection, PhysicalSchemaDiscoveryResult physical, CancellationToken ct)
    {
        var snapshot = new DbCatalogSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = job.TenantId,
            ConnectionProfileId = connection.Id,
            DiscoveryJobId = job.Id,
            SchemaVersion = 1,
            SchemaCount = physical.Schemas.DistinctBy(s => s.SchemaName).Count(),
            TableCount = physical.Tables.Count(t => t.ObjectType == DbCatalogObjectTypes.Table),
            ViewCount = physical.Tables.Count(t => t.ObjectType == DbCatalogObjectTypes.View),
            ColumnCount = physical.Columns.Count,
            IndexCount = physical.Indexes.Count,
            RelationshipCount = physical.Relationships.Count,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.DbCatalogSnapshots.Add(snapshot);

        foreach (var schema in physical.Schemas.DistinctBy(s => s.SchemaName))
        {
            _db.DbCatalogSchemas.Add(new DbCatalogSchema
            {
                Id = Guid.NewGuid(),
                TenantId = job.TenantId,
                ConnectionProfileId = connection.Id,
                SnapshotId = snapshot.Id,
                SchemaName = schema.SchemaName
            });
        }

        foreach (var table in physical.Tables)
        {
            if (table.ObjectType == DbCatalogObjectTypes.View)
            {
                _db.DbCatalogViews.Add(new DbCatalogView
                {
                    Id = Guid.NewGuid(),
                    TenantId = job.TenantId,
                    ConnectionProfileId = connection.Id,
                    SnapshotId = snapshot.Id,
                    SchemaName = table.SchemaName,
                    ObjectName = table.ObjectName
                });
            }
            else
            {
                _db.DbCatalogTables.Add(new DbCatalogTable
                {
                    Id = Guid.NewGuid(),
                    TenantId = job.TenantId,
                    ConnectionProfileId = connection.Id,
                    SnapshotId = snapshot.Id,
                    SchemaName = table.SchemaName,
                    ObjectName = table.ObjectName,
                    ObjectType = table.ObjectType,
                    EstimatedRowCount = table.EstimatedRowCount
                });
            }
        }

        foreach (var col in physical.Columns)
        {
            _db.DbCatalogColumns.Add(new DbCatalogColumn
            {
                Id = Guid.NewGuid(),
                TenantId = job.TenantId,
                ConnectionProfileId = connection.Id,
                SnapshotId = snapshot.Id,
                SchemaName = col.SchemaName,
                ObjectName = col.ObjectName,
                ColumnName = col.ColumnName,
                DataType = col.DataType,
                IsNullable = col.IsNullable,
                DefaultValue = col.DefaultValue,
                IsPrimaryKey = col.IsPrimaryKey,
                IsForeignKey = col.IsForeignKey,
                IsIndexed = col.IsIndexed,
                Ordinal = col.Ordinal
            });
        }

        foreach (var idx in physical.Indexes)
        {
            _db.DbCatalogIndexes.Add(new DbCatalogIndex
            {
                Id = Guid.NewGuid(),
                TenantId = job.TenantId,
                ConnectionProfileId = connection.Id,
                SnapshotId = snapshot.Id,
                SchemaName = idx.SchemaName,
                ObjectName = idx.ObjectName,
                IndexName = idx.IndexName,
                IsUnique = idx.IsUnique,
                ColumnNames = string.Join(",", idx.ColumnNames)
            });
        }

        foreach (var rel in physical.Relationships)
        {
            _db.DbCatalogRelationships.Add(new DbCatalogRelationship
            {
                Id = Guid.NewGuid(),
                TenantId = job.TenantId,
                ConnectionProfileId = connection.Id,
                SnapshotId = snapshot.Id,
                FromSchema = rel.FromSchema,
                FromTable = rel.FromTable,
                FromColumn = rel.FromColumn,
                ToSchema = rel.ToSchema,
                ToTable = rel.ToTable,
                ToColumn = rel.ToColumn,
                Source = rel.Source,
                ConfidencePercent = rel.ConfidencePercent
            });
        }

        foreach (var c in physical.Constraints)
        {
            _db.DbCatalogConstraints.Add(new DbCatalogConstraint
            {
                Id = Guid.NewGuid(),
                TenantId = job.TenantId,
                ConnectionProfileId = connection.Id,
                SnapshotId = snapshot.Id,
                SchemaName = c.SchemaName,
                ObjectName = c.ObjectName,
                ConstraintName = c.ConstraintName,
                ConstraintType = c.ConstraintType,
                ColumnNames = string.Join(",", c.ColumnNames)
            });
        }

        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    private async Task<DbConnectionProfile> GetActiveConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken ct)
    {
        var connection = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId && c.IsActive, ct);
        return connection ?? throw new KeyNotFoundException("Connection not found.");
    }

    private async Task<DbCatalogSnapshot?> GetLatestSnapshotEntityAsync(Guid tenantId, Guid connectionId, CancellationToken ct)
        => await _db.DbCatalogSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ConnectionProfileId == connectionId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    private static void AppendLog(DbDiscoveryJob job, string message)
    {
        var logs = ParseLogs(job.LogsJson).ToList();
        logs.Add($"{DateTime.UtcNow:O} {message}");
        job.LogsJson = JsonSerializer.Serialize(logs);
    }

    private static IReadOnlyList<string> ParseLogs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return Array.Empty<string>(); }
    }

    private static DbDiscoveryJobDto ToJobDto(DbDiscoveryJob job, IReadOnlyList<string> logs) => new(
        job.Id, job.TenantId, job.ConnectionProfileId, job.Status, job.ProgressPercent,
        job.TablesDiscovered, job.ViewsDiscovered, job.ColumnsDiscovered, job.RelationshipsDiscovered,
        job.CatalogSnapshotId, job.ErrorMessage, logs,
        job.CreatedAtUtc, job.StartedAtUtc, job.CompletedAtUtc,
        job.StartedAtUtc.HasValue && job.CompletedAtUtc.HasValue
            ? (long?)(job.CompletedAtUtc.Value - job.StartedAtUtc.Value).TotalMilliseconds
            : null);

    private static DbCatalogSnapshotDto ToSnapshotDto(DbCatalogSnapshot s) => new(
        s.Id, s.TenantId, s.ConnectionProfileId, s.DiscoveryJobId, s.SchemaVersion,
        s.SchemaCount, s.TableCount, s.ViewCount, s.ColumnCount, s.IndexCount, s.RelationshipCount, s.CreatedAtUtc);
}

public sealed class DbDiscoveryBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DbDiscoveryBackgroundWorker> _logger;

    public DbDiscoveryBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<DbDiscoveryBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var discovery = scope.ServiceProvider.GetRequiredService<DbSchemaDiscoveryService>();
                var pending = await db.DbDiscoveryJobs
                    .IgnoreQueryFilters()
                    .Where(j => j.Status == DbDiscoveryJobStatus.Pending)
                    .OrderBy(j => j.CreatedAtUtc)
                    .Select(j => j.Id)
                    .Take(5)
                    .ToListAsync(stoppingToken);

                foreach (var jobId in pending)
                    await discovery.ProcessPendingJobAsync(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DbDiscoveryBackgroundWorker cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
