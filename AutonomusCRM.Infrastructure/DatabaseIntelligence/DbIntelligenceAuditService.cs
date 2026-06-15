using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence;

public sealed class DbIntelligenceAuditService : IDbIntelligenceAuditService
{
    private readonly ApplicationDbContext _db;

    public DbIntelligenceAuditService(ApplicationDbContext db) => _db = db;

    public async Task RecordAsync(DbIntelligenceAuditEntry entry, CancellationToken cancellationToken = default)
    {
        _db.DbIntelligenceForensicAudits.Add(new DbIntelligenceForensicAudit
        {
            Id = Guid.NewGuid(),
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            ConnectionProfileId = entry.ConnectionProfileId,
            Action = entry.Action,
            EngineType = entry.EngineType?.ToString(),
            HostMasked = entry.HostMasked,
            DatabaseName = entry.DatabaseName,
            Success = entry.Success,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            ErrorMessage = entry.ErrorMessage,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
