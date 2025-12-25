using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Domain.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages;

public class CustomersModel : PageModel
{
    public List<CustomerDto> Customers { get; set; } = new();
    public List<CustomerDto> FilteredCustomers { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public AutonomusCRM.Domain.Customers.CustomerStatus? FilterStatus { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomersModel> _logger;

    public CustomersModel(IServiceProvider serviceProvider, ILogger<CustomersModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool? Created { get; set; }

    public async Task OnGetAsync(bool? created = null, string? search = null, AutonomusCRM.Domain.Customers.CustomerStatus? status = null, int? bulkUpdated = null, int? imported = null)
    {
        try
        {
            Created = created;
            SearchTerm = search;
            FilterStatus = status;
            TenantId = await GetDefaultTenantIdAsync();
            
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
            
            // Aplicar filtros
            var filtered = Customers.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filtered = filtered.Where(c => 
                    (c.Name?.ToLower().Contains(searchLower) ?? false) ||
                    (c.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (c.Company?.ToLower().Contains(searchLower) ?? false) ||
                    (c.Phone?.Contains(SearchTerm) ?? false)
                );
            }
            
            if (FilterStatus.HasValue)
            {
                filtered = filtered.Where(c => c.Status == FilterStatus.Value);
            }
            
            FilteredCustomers = filtered.ToList();
            
            if (bulkUpdated.HasValue && bulkUpdated.Value > 0)
            {
                TempData["Message"] = $"Se actualizaron {bulkUpdated.Value} clientes correctamente.";
            }
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = $"Se importaron {imported.Value} clientes correctamente.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string? email, string? phone, string? company)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateCustomerCommand, Guid>>();
            var command = new CreateCustomerCommand(TenantId, name, email, phone, company);
            var customerId = await handler.HandleAsync(command);
            
            return RedirectToPage("/Customers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return Page();
        }
    }

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        try
        {
            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync();
            var firstTenant = tenants.FirstOrDefault();
            
            if (firstTenant != null)
                return firstTenant.Id;

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand, Guid>>();
            var createCommand = new AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand("Default Tenant", "Tenant por defecto");
            return await createHandler.HandleAsync(createCommand);
        }
        catch
        {
            return Guid.Empty;
        }
    }
}

