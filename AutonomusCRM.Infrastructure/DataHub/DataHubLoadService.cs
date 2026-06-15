using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Application.Users.Commands;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubLoadService : IDataHubLoadService
{
    private readonly ApplicationDbContext _db;
    private readonly IRequestHandler<CreateCustomerCommand, Guid> _createCustomer;
    private readonly IRequestHandler<CreateLeadCommand, Guid> _createLead;
    private readonly IRequestHandler<CreateDealCommand, Guid> _createDeal;
    private readonly IRequestHandler<CreateUserCommand, Guid> _createUser;
    private readonly ICustomerRepository _customers;
    private readonly IDataHubRollbackService _rollback;

    public DataHubLoadService(
        ApplicationDbContext db,
        IRequestHandler<CreateCustomerCommand, Guid> createCustomer,
        IRequestHandler<CreateLeadCommand, Guid> createLead,
        IRequestHandler<CreateDealCommand, Guid> createDeal,
        IRequestHandler<CreateUserCommand, Guid> createUser,
        ICustomerRepository customers,
        IDataHubRollbackService rollback)
    {
        _db = db;
        _createCustomer = createCustomer;
        _createLead = createLead;
        _createDeal = createDeal;
        _createUser = createUser;
        _customers = customers;
        _rollback = rollback;
    }

    public async Task<DataHubLoadRowResult> LoadRowAsync(
        Guid tenantId, string targetEntity, string loadMode, Dictionary<string, string?> data,
        bool dryRun, int rowNumber = 0, int? batchNumber = null,
        CancellationToken cancellationToken = default)
    {
        if (dryRun || loadMode == DataHubLoadMode.DryRun.ToString())
            return new DataHubLoadRowResult(0, 0, 1, null, null, null);

        try
        {
            return targetEntity switch
            {
                nameof(DataHubTargetEntity.Customer) or "Customer" => await LoadCustomerAsync(tenantId, loadMode, data, rowNumber, batchNumber, cancellationToken),
                nameof(DataHubTargetEntity.Lead) or "Lead" => await LoadLeadAsync(tenantId, loadMode, data, rowNumber, batchNumber, cancellationToken),
                nameof(DataHubTargetEntity.Deal) or "Deal" => await LoadDealAsync(tenantId, loadMode, data, rowNumber, batchNumber, cancellationToken),
                nameof(DataHubTargetEntity.User) or "User" => await LoadUserAsync(tenantId, loadMode, data, rowNumber, batchNumber, cancellationToken),
                _ => new DataHubLoadRowResult(0, 0, 0, null, $"Unsupported entity: {targetEntity}", null)
            };
        }
        catch (Exception ex)
        {
            return new DataHubLoadRowResult(0, 0, 0, null, ex.Message, null);
        }
    }

    private async Task<DataHubLoadRowResult> LoadCustomerAsync(
        Guid tenantId, string loadMode, Dictionary<string, string?> data, int rowNumber, int? batchNumber, CancellationToken ct)
    {
        var name = data.GetValueOrDefault("Name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new DataHubLoadRowResult(0, 0, 0, null, "Name required", null);

        var email = data.GetValueOrDefault("Email");
        var phone = data.GetValueOrDefault("Phone");
        var company = data.GetValueOrDefault("Company");
        var jobId = ParseGuid(data.GetValueOrDefault("_jobId"));

        if (!string.IsNullOrWhiteSpace(email) &&
            (loadMode is nameof(DataHubLoadMode.Upsert) or nameof(DataHubLoadMode.UpdateExisting)
             or nameof(DataHubLoadMode.SkipDuplicates) or nameof(DataHubLoadMode.MergeDuplicates)))
        {
            var existing = await _db.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == email.ToLower(), ct);
            if (existing != null)
            {
                if (loadMode == DataHubLoadMode.SkipDuplicates.ToString())
                    return new DataHubLoadRowResult(0, 0, 1, existing.Id, null, null);

                var prev = DataHubRollbackService.CaptureCustomerState(existing);
                existing.UpdateContactInfo(email, phone, company);
                await _db.SaveChangesAsync(ct);
                var snap = jobId.HasValue
                    ? _rollback.CreateSnapshot(jobId.Value, tenantId, rowNumber, batchNumber, "Customer", existing.Id, "Updated", prev)
                    : null;
                return new DataHubLoadRowResult(0, 1, 0, existing.Id, null, snap);
            }
        }

        var id = await _createCustomer.HandleAsync(new CreateCustomerCommand(tenantId, name, email, phone, company), ct);
        var createdSnap = jobId.HasValue
            ? _rollback.CreateSnapshot(jobId.Value, tenantId, rowNumber, batchNumber, "Customer", id, "Created")
            : null;
        return new DataHubLoadRowResult(1, 0, 0, id, null, createdSnap);
    }

    private async Task<DataHubLoadRowResult> LoadLeadAsync(
        Guid tenantId, string loadMode, Dictionary<string, string?> data, int rowNumber, int? batchNumber, CancellationToken ct)
    {
        var name = data.GetValueOrDefault("Name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new DataHubLoadRowResult(0, 0, 0, null, "Name required", null);

        var sourceStr = data.GetValueOrDefault("Source") ?? "Other";
        var source = Enum.TryParse<LeadSource>(sourceStr, true, out var parsed) ? parsed : LeadSource.Other;
        var email = data.GetValueOrDefault("Email");
        var phone = data.GetValueOrDefault("Phone");
        var company = data.GetValueOrDefault("Company");
        var jobId = ParseGuid(data.GetValueOrDefault("_jobId"));

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existing = await _db.Leads.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Email != null && l.Email.ToLower() == email.ToLower(), ct);
            if (existing != null)
            {
                if (loadMode == DataHubLoadMode.SkipDuplicates.ToString())
                    return new DataHubLoadRowResult(0, 0, 1, existing.Id, null, null);

                if (loadMode is nameof(DataHubLoadMode.Upsert) or nameof(DataHubLoadMode.UpdateExisting) or nameof(DataHubLoadMode.MergeDuplicates))
                {
                    var prev = DataHubRollbackService.CaptureLeadState(existing);
                    existing.UpdateInfo(name, email, phone, company, source);
                    await _db.SaveChangesAsync(ct);
                    var snap = jobId.HasValue
                        ? _rollback.CreateSnapshot(jobId.Value, tenantId, rowNumber, batchNumber, "Lead", existing.Id, "Updated", prev)
                        : null;
                    return new DataHubLoadRowResult(0, 1, 0, existing.Id, null, snap);
                }
            }
        }

        var id = await _createLead.HandleAsync(new CreateLeadCommand(tenantId, name, source, email, phone, company), ct);
        var createdSnap = jobId.HasValue
            ? _rollback.CreateSnapshot(jobId.Value, tenantId, rowNumber, batchNumber, "Lead", id, "Created")
            : null;
        return new DataHubLoadRowResult(1, 0, 0, id, null, createdSnap);
    }

    private async Task<DataHubLoadRowResult> LoadDealAsync(
        Guid tenantId, string loadMode, Dictionary<string, string?> data, int rowNumber, int? batchNumber, CancellationToken ct)
    {
        var title = data.GetValueOrDefault("Title")?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return new DataHubLoadRowResult(0, 0, 0, null, "Title required", null);
        if (!decimal.TryParse(data.GetValueOrDefault("Amount"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return new DataHubLoadRowResult(0, 0, 0, null, "Invalid amount", null);

        var customerEmail = data.GetValueOrDefault("CustomerEmail");
        Guid customerId;
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var match = await _db.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == customerEmail.ToLower(), ct);
            if (match == null) return new DataHubLoadRowResult(0, 0, 0, null, "Customer not found", null);
            customerId = match.Id;
        }
        else
        {
            var all = (await _customers.FindAsync(c => c.TenantId == tenantId, ct)).ToList();
            if (all.Count != 1) return new DataHubLoadRowResult(0, 0, 0, null, "CustomerEmail required", null);
            customerId = all[0].Id;
        }

        var jobId = ParseGuid(data.GetValueOrDefault("_jobId"));
        var id = await _createDeal.HandleAsync(new CreateDealCommand(tenantId, customerId, title, amount), ct);
        var snap = jobId.HasValue
            ? _rollback.CreateSnapshot(jobId.Value, tenantId, rowNumber, batchNumber, "Deal", id, "Created")
            : null;
        return new DataHubLoadRowResult(1, 0, 0, id, null, snap);
    }

    private async Task<DataHubLoadRowResult> LoadUserAsync(
        Guid tenantId, string loadMode, Dictionary<string, string?> data, int rowNumber, int? batchNumber, CancellationToken ct)
    {
        var email = data.GetValueOrDefault("Email")?.Trim();
        var password = data.GetValueOrDefault("Password");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return new DataHubLoadRowResult(0, 0, 0, null, "Email and Password required", null);

        if (loadMode == DataHubLoadMode.SkipDuplicates.ToString())
        {
            var exists = await _db.Users.AnyAsync(u => u.TenantId == tenantId && u.Email.ToLower() == email.ToLower(), ct);
            if (exists) return new DataHubLoadRowResult(0, 0, 1, null, null, null);
        }

        var id = await _createUser.HandleAsync(new CreateUserCommand(
            tenantId, email, password,
            data.GetValueOrDefault("FirstName"), data.GetValueOrDefault("LastName"),
            "Sales"), ct);
        var jobId = ParseGuid(data.GetValueOrDefault("_jobId"));
        var snap = jobId.HasValue
            ? _rollback.CreateSnapshot(jobId.Value, tenantId, rowNumber, batchNumber, "User", id, "Created")
            : null;
        return new DataHubLoadRowResult(1, 0, 0, id, null, snap);
    }

    private static Guid? ParseGuid(string? v) => Guid.TryParse(v, out var g) ? g : null;
}
