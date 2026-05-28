using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Application.Tenants.Commands;
using AutonomusCRM.Domain.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages.Customers;

public class DetailsModel : PageModel
{
    public CustomerDto? Customer { get; set; }
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IServiceProvider serviceProvider, ILogger<DetailsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(tenantId);
            
            var customer = customers.FirstOrDefault(c => c.Id == id);
            
            if (customer == null)
            {
                return NotFound();
            }
            
            Customer = new CustomerDto(
                customer.Id, customer.TenantId, customer.Name, customer.Email, customer.Phone, 
                customer.Company, customer.Status, customer.LifetimeValue, customer.RiskScore, customer.CreatedAt
            );
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer details");
            return RedirectToPage("/Customers");
        }
    }

    public async Task<IActionResult> OnPostCreateDealAsync(Guid id, string title, decimal amount, string? description)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var createDealCommand = new AutonomusCRM.Application.Deals.Commands.CreateDealCommand(
                tenantId, id, title, amount, description);
            var createDealHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Deals.Commands.CreateDealCommand, Guid>>();
            var dealId = await createDealHandler.HandleAsync(createDealCommand, CancellationToken.None);
            
            return RedirectToPage("/Deals/Details", new { id = dealId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal from customer");
            return RedirectToPage("/Customers/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostRecordContactAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customer = await customerRepository.GetByIdAsync(id, CancellationToken.None);
            
            if (customer != null && customer.TenantId == tenantId)
            {
                customer.RecordContact(DateTime.UtcNow);
                await customerRepository.UpdateAsync(customer, CancellationToken.None);
                var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
            
            return RedirectToPage("/Customers/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording contact");
            return RedirectToPage("/Customers/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AutonomusCRM.Application.Customers.Commands.DeleteCustomerCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Customers.Commands.DeleteCustomerCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Customers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer");
            return RedirectToPage("/Customers/Details", new { id });
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

