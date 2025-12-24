using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Domain.Deals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages;

public class DealsModel : PageModel
{
    public List<DealDto> Deals { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DealsModel> _logger;

    public DealsModel(IServiceProvider serviceProvider, ILogger<DealsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var dealsHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
            var dealsQuery = new GetDealsByTenantQuery(TenantId);
            var deals = await dealsHandler.HandleAsync(dealsQuery);
            Deals = deals.ToList();

            // Para el formulario, necesitamos todos los customers
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deals");
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(Guid customerId, string title, decimal amount, string? description)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
            var command = new CreateDealCommand(TenantId, customerId, title, amount, description);
            var dealId = await handler.HandleAsync(command);
            
            return RedirectToPage("/Deals");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal");
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

