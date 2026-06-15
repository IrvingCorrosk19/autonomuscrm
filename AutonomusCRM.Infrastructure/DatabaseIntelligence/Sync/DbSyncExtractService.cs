using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncExtractService : IDbSyncExtractService
{
    private const int SampleRowLimit = 500;

    private readonly ApplicationDbContext _db;
    private readonly IDbConnectionVault _vault;
    private readonly DbBusinessSampleReader _sampleReader;

    public DbSyncExtractService(
        ApplicationDbContext db,
        IDbConnectionVault vault,
        DbBusinessSampleReader sampleReader)
    {
        _db = db;
        _vault = vault;
        _sampleReader = sampleReader;
    }

    public async Task<IReadOnlyList<DbSyncExtractedRow>> ExtractAsync(
        Guid tenantId, Guid connectionId, IReadOnlyList<DbSyncMappingContext> mappings,
        string syncMode, DateTime? watermarkUtc, CancellationToken cancellationToken = default)
    {
        var connection = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId && c.IsActive, cancellationToken);
        if (connection == null)
            return Array.Empty<DbSyncExtractedRow>();

        var columns = await _db.DbCatalogColumns.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.ConnectionProfileId == connectionId)
            .ToListAsync(cancellationToken);

        DbConnectionSecrets secrets;
        try
        {
            secrets = _vault.Decrypt(connection.EncryptedConnectionBlob);
        }
        catch
        {
            return Array.Empty<DbSyncExtractedRow>();
        }

        var rows = new List<DbSyncExtractedRow>();
        var rowNumber = 1;

        foreach (var mapping in mappings.Where(m => m.Status != DbBusinessMappingStatus.Ignored))
        {
            var tableColumns = columns
                .Where(c => c.SchemaName == mapping.SchemaName && c.ObjectName == mapping.TableName)
                .Select(c => c.ColumnName)
                .ToList();

            var sample = await _sampleReader.ReadTopNAsync(
                connection, secrets, mapping.SchemaName, mapping.TableName,
                tableColumns, SampleRowLimit, 30, cancellationToken);

            foreach (var sampleRow in sample)
            {
                var modified = ParseTimestamp(sampleRow);
                if (syncMode == DbSyncMode.Delta && watermarkUtc.HasValue)
                {
                    if (!modified.HasValue || modified.Value <= watermarkUtc.Value)
                        continue;
                }

                rows.Add(new DbSyncExtractedRow(
                    mapping.EntityType,
                    mapping.SchemaName,
                    mapping.TableName,
                    rowNumber++,
                    sampleRow.ToDictionary(kv => kv.Key, kv => kv.Value),
                    modified));
            }
        }

        return rows;
    }

    private static DateTime? ParseTimestamp(IReadOnlyDictionary<string, string?> row)
    {
        foreach (var key in new[] { "updated_at", "modified_at", "last_modified", "fecha_modificacion" })
        {
            foreach (var kv in row)
            {
                if (!kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (DateTime.TryParse(kv.Value, out var dt))
                    return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }
        return null;
    }
}
