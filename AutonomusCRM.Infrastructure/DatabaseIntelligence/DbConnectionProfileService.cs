using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence;

public sealed class DbConnectionProfileService : IDbConnectionProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly IDbConnectionVault _vault;
    private readonly IDbConnectorFactory _connectorFactory;
    private readonly IDbIntelligenceAuditService _audit;
    private readonly DbIntelligenceSecurityOptions _options;

    public DbConnectionProfileService(
        ApplicationDbContext db,
        IDbConnectionVault vault,
        IDbConnectorFactory connectorFactory,
        IDbIntelligenceAuditService audit,
        IOptions<DbIntelligenceSecurityOptions> options)
    {
        _db = db;
        _vault = vault;
        _connectorFactory = connectorFactory;
        _audit = audit;
        _options = options.Value;
    }

    public async Task<DbConnectionProfileDto> CreateAsync(
        Guid tenantId,
        Guid userId,
        CreateDbConnectionProfileRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        DbConnectionStringValidator.ValidateCreateRequest(request);

        var count = await _db.DbConnectionProfiles.CountAsync(p => p.TenantId == tenantId && p.IsActive, cancellationToken);
        if (count >= _options.MaxConnectionsPerTenant)
            throw new DbIntelligenceQuotaException($"Maximum active connections ({_options.MaxConnectionsPerTenant}) reached for this organization.");

        var test = await TestAsync(new TestDbConnectionRequest(
            request.EngineType, request.Host, request.Port, request.DatabaseName,
            request.Username, request.Password, request.IsReadOnly), cancellationToken);
        if (!test.Success)
            throw new DbIntelligenceValidationException(test.Message);

        var now = DateTime.UtcNow;
        var entity = new DbConnectionProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            EngineType = request.EngineType,
            Host = request.Host.Trim(),
            Port = request.Port,
            DatabaseName = request.DatabaseName.Trim(),
            Username = request.Username.Trim(),
            UsernameMasked = DbIntelligenceMasking.MaskUsername(request.Username),
            EncryptedConnectionBlob = _vault.Encrypt(new DbConnectionSecrets(request.Password)),
            IsReadOnly = request.IsReadOnly,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            LastTestedAtUtc = now,
            LastTestSucceeded = true,
            LastErrorMessage = null
        };

        _db.DbConnectionProfiles.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId,
            DbIntelligenceForensicActions.ConnectionCreated,
            userId,
            entity.Id,
            entity.EngineType,
            DbIntelligenceMasking.MaskHost(entity.Host),
            entity.DatabaseName,
            true,
            ipAddress,
            userAgent), cancellationToken);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<DbConnectionProfileDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var items = await _db.DbConnectionProfiles
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<DbConnectionProfileDto?> GetAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DbConnectionProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == connectionId && p.IsActive, cancellationToken);
        return entity == null ? null : ToDto(entity);
    }

    public async Task DeleteAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == connectionId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Connection not found.");

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId,
            DbIntelligenceForensicActions.ConnectionDeleted,
            userId,
            entity.Id,
            entity.EngineType,
            DbIntelligenceMasking.MaskHost(entity.Host),
            entity.DatabaseName,
            true,
            ipAddress,
            userAgent), cancellationToken);
    }

    public async Task<DbConnectionTestResultDto> TestExistingAsync(
        Guid tenantId,
        Guid userId,
        Guid connectionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.DbConnectionProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == connectionId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Connection not found.");

        var secrets = _vault.Decrypt(entity.EncryptedConnectionBlob);
        var endpoint = new DbConnectionEndpoint(entity.Host, entity.Port, entity.DatabaseName, entity.Username);
        var connector = _connectorFactory.Create(entity.EngineType);
        var result = await connector.TestConnectionAsync(
            endpoint, secrets, entity.IsReadOnly, _options.ConnectionTimeoutSeconds, cancellationToken);

        entity.LastTestedAtUtc = DateTime.UtcNow;
        entity.LastTestSucceeded = result.Success;
        entity.LastErrorMessage = result.Success ? null : result.Message;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(new DbIntelligenceAuditEntry(
            tenantId,
            result.Success ? DbIntelligenceForensicActions.ConnectionTested : DbIntelligenceForensicActions.ConnectionTestFailed,
            userId,
            entity.Id,
            entity.EngineType,
            DbIntelligenceMasking.MaskHost(entity.Host),
            entity.DatabaseName,
            result.Success,
            ipAddress,
            userAgent,
            result.Success ? null : result.Message), cancellationToken);

        return result;
    }

    public Task<DbConnectionTestResultDto> TestAsync(
        TestDbConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        DbConnectionStringValidator.ValidateTestRequest(request);
        var endpoint = new DbConnectionEndpoint(
            request.Host.Trim(),
            request.Port,
            request.DatabaseName.Trim(),
            request.Username.Trim());
        var connector = _connectorFactory.Create(request.EngineType);
        return connector.TestConnectionAsync(
            endpoint,
            new DbConnectionSecrets(request.Password),
            request.IsReadOnly,
            _options.ConnectionTimeoutSeconds,
            cancellationToken);
    }

    private static DbConnectionProfileDto ToDto(DbConnectionProfile entity) => new(
        entity.Id,
        entity.TenantId,
        entity.Name,
        entity.EngineType,
        entity.Host,
        entity.Port,
        entity.DatabaseName,
        entity.UsernameMasked,
        entity.IsReadOnly,
        entity.IsActive,
        entity.CreatedAtUtc,
        entity.UpdatedAtUtc,
        entity.LastTestedAtUtc,
        entity.LastTestSucceeded,
        entity.LastErrorMessage);
}
