using System.Text.Json;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncLoadService : IDbSyncLoadService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ApplicationDbContext _db;
    private readonly IRequestHandler<CreateCustomerCommand, Guid> _createCustomer;
    private readonly IRequestHandler<CreateLeadCommand, Guid> _createLead;
    private readonly IRequestHandler<CreateDealCommand, Guid> _createDeal;
    private readonly IDbSyncConflictResolver _conflicts;
    private readonly IDbSyncRollbackService _rollback;

    public DbSyncLoadService(
        ApplicationDbContext db,
        IRequestHandler<CreateCustomerCommand, Guid> createCustomer,
        IRequestHandler<CreateLeadCommand, Guid> createLead,
        IRequestHandler<CreateDealCommand, Guid> createDeal,
        IDbSyncConflictResolver conflicts,
        IDbSyncRollbackService rollback)
    {
        _db = db;
        _createCustomer = createCustomer;
        _createLead = createLead;
        _createDeal = createDeal;
        _conflicts = conflicts;
        _rollback = rollback;
    }

    public async Task<DbSyncLoadResult> LoadRowAsync(
        Guid tenantId, Guid jobId, DbSyncStagingRow row, string conflictPolicy,
        CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string?>>(row.PayloadJson, JsonOptions)
                   ?? new Dictionary<string, string?>();

        return row.EntityType switch
        {
            BusinessEntityType.Customer or BusinessEntityType.Company =>
                await LoadCustomerAsync(tenantId, jobId, row, data, conflictPolicy, cancellationToken),
            BusinessEntityType.Contact or BusinessEntityType.Activity =>
                await LoadLeadAsync(tenantId, jobId, row, data, conflictPolicy, cancellationToken),
            BusinessEntityType.Sale =>
                await LoadDealAsync(tenantId, jobId, row, data, conflictPolicy, cancellationToken),
            _ => new DbSyncLoadResult(0, 0, 1, 0, null, $"Unsupported entity type: {row.EntityType}", null)
        };
    }

    private async Task<DbSyncLoadResult> LoadCustomerAsync(
        Guid tenantId, Guid jobId, DbSyncStagingRow row, Dictionary<string, string?> data,
        string conflictPolicy, CancellationToken ct)
    {
        var name = FirstNonEmpty(data, "name", "cli_name", "customer_name", "razon_social", "company");
        if (string.IsNullOrWhiteSpace(name))
            return new DbSyncLoadResult(0, 0, 0, 1, null, "Name required", null);

        var email = FirstNonEmpty(data, "email", "cli_email", "customer_email", "correo");
        var phone = FirstNonEmpty(data, "phone", "telefono");
        var company = row.EntityType == BusinessEntityType.Company
            ? name
            : FirstNonEmpty(data, "company", "razon_social");

        var existing = !string.IsNullOrWhiteSpace(email)
            ? await _db.Customers.FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == email.ToLower(), ct)
            : null;

        var decision = _conflicts.Resolve(conflictPolicy, existing != null, true, existing != null);
        if (decision.Action == "Skip")
            return new DbSyncLoadResult(0, 0, 1, 0, existing?.Id, null, null);
        if (decision.Action == "Conflict")
            return new DbSyncLoadResult(0, 0, 0, 0, null, decision.Reason, null);

        if (existing != null && decision.Action == "Update")
        {
            var prev = DbSyncRollbackService.CaptureCustomerState(existing);
            existing.UpdateContactInfo(email, phone, company);
            await _db.SaveChangesAsync(ct);
            var snap = _rollback.CreateSnapshot(jobId, tenantId, row.RowNumber, "Customer", existing.Id, "Updated", prev);
            return new DbSyncLoadResult(0, 1, 0, 0, existing.Id, null, snap);
        }

        try
        {
            var id = await _createCustomer.HandleAsync(new CreateCustomerCommand(tenantId, name, email, phone, company), ct);
            var created = _rollback.CreateSnapshot(jobId, tenantId, row.RowNumber, "Customer", id, "Created");
            return new DbSyncLoadResult(1, 0, 0, 0, id, null, created);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var created = await _db.Customers.FirstOrDefaultAsync(
                    c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == email.ToLower(), ct);
                if (created != null)
                {
                    var snap = _rollback.CreateSnapshot(jobId, tenantId, row.RowNumber, "Customer", created.Id, "Created");
                    return new DbSyncLoadResult(1, 0, 0, 0, created.Id, null, snap);
                }
            }
            return new DbSyncLoadResult(0, 0, 0, 1, null, ex.Message, null);
        }
    }

    private async Task<DbSyncLoadResult> LoadLeadAsync(
        Guid tenantId, Guid jobId, DbSyncStagingRow row, Dictionary<string, string?> data,
        string conflictPolicy, CancellationToken ct)
    {
        var name = FirstNonEmpty(data, "name", "first_name", "subject", "contact_name");
        if (string.IsNullOrWhiteSpace(name))
            return new DbSyncLoadResult(0, 0, 0, 1, null, "Name required", null);

        var email = FirstNonEmpty(data, "email", "correo");
        var phone = FirstNonEmpty(data, "phone", "telefono");
        var company = FirstNonEmpty(data, "company", "razon_social");

        var existing = !string.IsNullOrWhiteSpace(email)
            ? await _db.Leads.FirstOrDefaultAsync(
                l => l.TenantId == tenantId && l.Email != null && l.Email.ToLower() == email.ToLower(), ct)
            : null;

        var decision = _conflicts.Resolve(conflictPolicy, existing != null, true, existing != null);
        if (decision.Action == "Skip")
            return new DbSyncLoadResult(0, 0, 1, 0, existing?.Id, null, null);
        if (decision.Action == "Conflict")
            return new DbSyncLoadResult(0, 0, 0, 0, null, decision.Reason, null);

        if (existing != null && decision.Action == "Update")
        {
            var prev = DbSyncRollbackService.CaptureLeadState(existing);
            existing.UpdateInfo(name, email, phone, company, LeadSource.Other);
            await _db.SaveChangesAsync(ct);
            var snap = _rollback.CreateSnapshot(jobId, tenantId, row.RowNumber, "Lead", existing.Id, "Updated", prev);
            return new DbSyncLoadResult(0, 1, 0, 0, existing.Id, null, snap);
        }

        try
        {
            var id = await _createLead.HandleAsync(
                new CreateLeadCommand(tenantId, name, LeadSource.Other, email, phone, company), ct);
            var created = _rollback.CreateSnapshot(jobId, tenantId, row.RowNumber, "Lead", id, "Created");
            return new DbSyncLoadResult(1, 0, 0, 0, id, null, created);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var created = await _db.Leads.FirstOrDefaultAsync(
                    l => l.TenantId == tenantId && l.Email != null && l.Email.ToLower() == email.ToLower(), ct);
                if (created != null)
                {
                    var snap = _rollback.CreateSnapshot(jobId, tenantId, row.RowNumber, "Lead", created.Id, "Created");
                    return new DbSyncLoadResult(1, 0, 0, 0, created.Id, null, snap);
                }
            }
            return new DbSyncLoadResult(0, 0, 0, 1, null, ex.Message, null);
        }
    }

    private async Task<DbSyncLoadResult> LoadDealAsync(
        Guid tenantId, Guid jobId, DbSyncStagingRow row, Dictionary<string, string?> data,
        string conflictPolicy, CancellationToken ct)
    {
        var title = FirstNonEmpty(data, "title", "order_id", "order_total", "venta_id");
        if (string.IsNullOrWhiteSpace(title))
            title = $"Sale {row.RowNumber}";

        var amountStr = FirstNonEmpty(data, "amount", "order_total", "total_amount");
        _ = decimal.TryParse(amountStr, out var amount);

        var customerEmail = FirstNonEmpty(data, "customer_email", "email");
        Guid customerId;
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == customerEmail.ToLower(), ct);
            if (customer == null)
                return new DbSyncLoadResult(0, 0, 0, 1, null, "Customer not found for deal", null);
            customerId = customer.Id;
        }
        else
        {
            var any = await _db.Customers.Where(c => c.TenantId == tenantId).Select(c => c.Id).FirstOrDefaultAsync(ct);
            if (any == Guid.Empty)
                return new DbSyncLoadResult(0, 0, 0, 1, null, "No customer available for deal", null);
            customerId = any;
        }

        var id = await _createDeal.HandleAsync(
            new CreateDealCommand(tenantId, customerId, title, amount, "Database Intelligence sync"), ct);
        var created = _rollback.CreateSnapshot(jobId, tenantId, row.RowNumber, "Deal", id, "Created");
        return new DbSyncLoadResult(1, 0, 0, 0, id, null, created);
    }

    private static string? FirstNonEmpty(Dictionary<string, string?> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var kv in data)
            {
                if (kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
                    return kv.Value!.Trim();
            }
        }
        return null;
    }
}
