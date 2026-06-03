using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Infrastructure.Imports;

public sealed class CrmImportService : ICrmImportService
{
    private readonly IRequestHandler<CreateCustomerCommand, Guid> _createCustomer;
    private readonly IRequestHandler<CreateLeadCommand, Guid> _createLead;
    private readonly IRequestHandler<CreateDealCommand, Guid> _createDeal;
    private readonly ICustomerRepository _customers;

    public CrmImportService(
        IRequestHandler<CreateCustomerCommand, Guid> createCustomer,
        IRequestHandler<CreateLeadCommand, Guid> createLead,
        IRequestHandler<CreateDealCommand, Guid> createDeal,
        ICustomerRepository customers)
    {
        _createCustomer = createCustomer;
        _createLead = createLead;
        _createDeal = createDeal;
        _customers = customers;
    }

    public async Task<ImportResultDto> ImportCustomersAsync(
        Guid tenantId, IReadOnlyList<CustomerImportRow> rows, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var created = 0;
        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    errors.Add("Customer row missing Name");
                    continue;
                }
                await _createCustomer.HandleAsync(
                    new CreateCustomerCommand(tenantId, row.Name.Trim(), row.Email, row.Phone, row.Company),
                    cancellationToken);
                created++;
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Name}: {ex.Message}");
            }
        }
        return new ImportResultDto(rows.Count, created, rows.Count - created, errors);
    }

    public async Task<ImportResultDto> ImportLeadsAsync(
        Guid tenantId, IReadOnlyList<LeadImportRow> rows, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var created = 0;
        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    errors.Add("Lead row missing Name");
                    continue;
                }
                var source = Enum.TryParse<LeadSource>(row.Source, true, out var parsed)
                    ? parsed
                    : LeadSource.Other;
                await _createLead.HandleAsync(
                    new CreateLeadCommand(tenantId, row.Name.Trim(), source, row.Email, row.Phone, row.Company),
                    cancellationToken);
                created++;
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Name}: {ex.Message}");
            }
        }
        return new ImportResultDto(rows.Count, created, rows.Count - created, errors);
    }

    public async Task<ImportResultDto> ImportDealsAsync(
        Guid tenantId, IReadOnlyList<DealImportRow> rows, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var created = 0;
        var allCustomers = (await _customers.FindAsync(c => c.TenantId == tenantId, cancellationToken)).ToList();

        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.Title))
                {
                    errors.Add("Deal row missing Title");
                    continue;
                }

                Guid customerId;
                if (!string.IsNullOrWhiteSpace(row.CustomerEmail))
                {
                    var match = allCustomers.FirstOrDefault(c =>
                        string.Equals(c.Email, row.CustomerEmail, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        errors.Add($"Deal {row.Title}: customer email not found");
                        continue;
                    }
                    customerId = match.Id;
                }
                else if (allCustomers.Count == 1)
                {
                    customerId = allCustomers[0].Id;
                }
                else
                {
                    errors.Add($"Deal {row.Title}: CustomerEmail required when multiple customers exist");
                    continue;
                }

                await _createDeal.HandleAsync(
                    new CreateDealCommand(tenantId, customerId, row.Title.Trim(), row.Amount),
                    cancellationToken);
                created++;
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Title}: {ex.Message}");
            }
        }
        return new ImportResultDto(rows.Count, created, rows.Count - created, errors);
    }
}
