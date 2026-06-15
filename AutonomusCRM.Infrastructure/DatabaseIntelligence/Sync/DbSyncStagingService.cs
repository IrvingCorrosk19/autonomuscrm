using System.Text.Json;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncStagingService : IDbSyncStagingService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ApplicationDbContext _db;

    public DbSyncStagingService(ApplicationDbContext db) => _db = db;

    public async Task StageRowsAsync(
        Guid tenantId, Guid jobId, IReadOnlyList<DbSyncExtractedRow> rows, CancellationToken cancellationToken = default)
    {
        var entities = rows.Select((r, i) => new DbSyncStagingRow
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            JobId = jobId,
            RowNumber = r.RowNumber > 0 ? r.RowNumber : i + 1,
            EntityType = r.EntityType,
            SchemaName = r.SchemaName,
            TableName = r.TableName,
            PayloadJson = JsonSerializer.Serialize(r.Data, JsonOptions),
            Status = DbSyncStagingStatus.Pending,
            SourceModifiedAtUtc = r.ModifiedAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        _db.DbSyncStagingRows.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DbSyncStagingRow>> GetPendingRowsAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default) =>
        await _db.DbSyncStagingRows
            .Where(r => r.TenantId == tenantId && r.JobId == jobId &&
                        (r.Status == DbSyncStagingStatus.Pending || r.Status == DbSyncStagingStatus.Valid))
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);
}
