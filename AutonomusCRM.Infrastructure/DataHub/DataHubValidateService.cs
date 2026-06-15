using System.Globalization;
using System.Text.RegularExpressions;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubValidateService : IDataHubValidateService
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"^\+?[\d\s\-().]{7,20}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _db;
    private readonly ICustomerRepository _customers;
    private readonly ILeadRepository _leads;
    private readonly IUserRepository _users;
    private readonly IDataHubFieldCatalog _fieldCatalog;

    public DataHubValidateService(ApplicationDbContext db, ICustomerRepository customers, ILeadRepository leads, IUserRepository users, IDataHubFieldCatalog fieldCatalog)
    {
        _db = db;
        _customers = customers;
        _leads = leads;
        _users = users;
        _fieldCatalog = fieldCatalog;
    }

    public async Task<IReadOnlyList<DataHubImportError>> ValidateRowAsync(
        Guid tenantId, string targetEntity, int rowNumber, Dictionary<string, string?> data,
        IReadOnlyList<DataHubValidationRule> rules, CancellationToken cancellationToken = default)
    {
        var errors = new List<DataHubImportError>();
        var fields = _fieldCatalog.GetFields(targetEntity);

        foreach (var field in fields.Where(f => f.IsRequired))
        {
            if (!data.TryGetValue(field.Name, out var val) || string.IsNullOrWhiteSpace(val))
            {
                errors.Add(new DataHubImportError
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    RowNumber = rowNumber,
                    ErrorCode = "Required",
                    FieldName = field.Name,
                    Message = $"{field.Label} is required"
                });
            }
        }

        if (data.TryGetValue("Email", out var email) && !string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
        {
            errors.Add(new DataHubImportError
            {
                Id = Guid.NewGuid(), TenantId = tenantId, RowNumber = rowNumber,
                ErrorCode = "InvalidEmail", FieldName = "Email", RawValue = email, Message = "Invalid email format"
            });
        }

        if (data.TryGetValue("Phone", out var phone) && !string.IsNullOrWhiteSpace(phone) && !PhoneRegex.IsMatch(phone))
        {
            errors.Add(new DataHubImportError
            {
                Id = Guid.NewGuid(), TenantId = tenantId, RowNumber = rowNumber,
                ErrorCode = "InvalidPhone", FieldName = "Phone", RawValue = phone, Message = "Invalid phone format"
            });
        }

        if (targetEntity.Equals(nameof(DataHubTargetEntity.Deal), StringComparison.OrdinalIgnoreCase)
            && data.TryGetValue("Amount", out var amountStr) && !string.IsNullOrWhiteSpace(amountStr))
        {
            if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt) || amt <= 0)
            {
                errors.Add(new DataHubImportError
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, RowNumber = rowNumber,
                    ErrorCode = "InvalidAmount", FieldName = "Amount", RawValue = amountStr, Message = "Amount must be greater than zero"
                });
            }
        }

        foreach (var rule in rules)
        {
            if (!data.TryGetValue(rule.TargetField, out var val)) continue;
            if (!Enum.TryParse<DataHubValidationType>(rule.ValidationType, true, out var vtype)) continue;

            switch (vtype)
            {
                case DataHubValidationType.MaxLength when rule.Parameters.TryGetValue("max", out var maxStr)
                    && int.TryParse(maxStr, out var max) && (val?.Length ?? 0) > max:
                    errors.Add(new DataHubImportError
                    {
                        Id = Guid.NewGuid(), TenantId = tenantId, RowNumber = rowNumber,
                        ErrorCode = "MaxLength", FieldName = rule.TargetField, Message = $"{rule.TargetField} exceeds max length {max}"
                    });
                    break;
            }
        }

        if (string.Equals(targetEntity, nameof(DataHubTargetEntity.Deal), StringComparison.OrdinalIgnoreCase)
            && data.TryGetValue("CustomerEmail", out var custEmail) && !string.IsNullOrWhiteSpace(custEmail))
        {
            var exists = await _db.Customers.AnyAsync(c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == custEmail.ToLower(), cancellationToken);
            if (!exists)
            {
                errors.Add(new DataHubImportError
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, RowNumber = rowNumber,
                    ErrorCode = "ForeignKey", FieldName = "CustomerEmail", RawValue = custEmail,
                    Message = "Customer email not found in tenant"
                });
            }
        }

        return errors;
    }

    public async Task<IReadOnlyList<DataHubQualityIssueDto>> ScanQualityAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var issues = new List<DataHubQualityIssueDto>();

        var customersNoEmail = await _db.Customers
            .Where(c => c.TenantId == tenantId && (c.Email == null || c.Email == ""))
            .Select(c => new { c.Id, c.Name })
            .Take(200)
            .ToListAsync(cancellationToken);
        issues.AddRange(customersNoEmail.Select(c => new DataHubQualityIssueDto(
            "Customer", c.Id, "MissingEmail", $"Customer '{c.Name}' has no email", "Warning",
            new Dictionary<string, string> { ["action"] = "MarkReview", ["actionLabel"] = "Mark for review" })));

        var duplicateEmails = await _db.Customers
            .Where(c => c.TenantId == tenantId && c.Email != null && c.Email != "")
            .GroupBy(c => c.Email!.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => new { Email = g.Key, Count = g.Count(), Ids = g.Select(x => x.Id).ToList() })
            .Take(50)
            .ToListAsync(cancellationToken);
        foreach (var dup in duplicateEmails)
        {
            issues.Add(new DataHubQualityIssueDto(
                "Customer", dup.Ids.First(), "DuplicateEmail",
                $"Email '{dup.Email}' appears {dup.Count} times", "Critical",
                new Dictionary<string, string>
                {
                    ["action"] = "Merge",
                    ["actionLabel"] = "Merge duplicates",
                    ["keepId"] = dup.Ids.First().ToString(),
                    ["duplicates"] = string.Join(",", dup.Ids.Skip(1))
                }));
        }

        var leadsNoOwner = await _db.Leads
            .Where(l => l.TenantId == tenantId && l.AssignedToUserId == null)
            .Select(l => new { l.Id, l.Name })
            .Take(200)
            .ToListAsync(cancellationToken);
        issues.AddRange(leadsNoOwner.Select(l => new DataHubQualityIssueDto(
            "Lead", l.Id, "MissingOwner", $"Lead '{l.Name}' has no owner", "Info",
            new Dictionary<string, string> { ["action"] = "Assign", ["actionLabel"] = "Assign owner" })));

        var dealsNoCustomer = await _db.Deals
            .Where(d => d.TenantId == tenantId)
            .Join(_db.Customers, d => d.CustomerId, c => c.Id, (d, c) => new { d.Id, c.Name })
            .Take(0)
            .ToListAsync(cancellationToken);
        _ = dealsNoCustomer;

        var orphanDeals = await _db.Deals
            .Where(d => d.TenantId == tenantId)
            .Select(d => d.CustomerId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var validCustomers = await _db.Customers.Where(c => c.TenantId == tenantId).Select(c => c.Id).ToListAsync(cancellationToken);
        var invalidDealCustomers = orphanDeals.Except(validCustomers).ToList();
        foreach (var cid in invalidDealCustomers.Take(50))
        {
            issues.Add(new DataHubQualityIssueDto(
                "Deal", cid, "InvalidCustomer", "Deal references missing customer", "Critical",
                new Dictionary<string, string> { ["action"] = "Fix manually" }));
        }

        return issues;
    }
}
