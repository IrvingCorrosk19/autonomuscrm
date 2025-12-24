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
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomersModel> _logger;

    public CustomersModel(IServiceProvider serviceProvider, ILogger<CustomersModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
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

