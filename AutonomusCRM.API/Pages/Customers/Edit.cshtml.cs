using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Domain.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages.Customers;

public class EditModel : PageModel
{
    public CustomerDto? Customer { get; set; }
    public List<SelectListItem> Statuses { get; set; } = new();
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EditModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EditModel(IServiceProvider serviceProvider, ILogger<EditModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
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

            InitializeSelectLists();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer for edit");
            return RedirectToPage("/Customers");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string name, string? email, string? phone, string? company, CustomerStatus? status)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new UpdateCustomerCommand(id, tenantId, name, email, phone, company, status);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateCustomerCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Customers/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer");
            ModelState.AddModelError("", _localizer["Flash_CustomerUpdateError"].Value);
            
            // Recargar datos
            var tenantId = await GetDefaultTenantIdAsync();
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(tenantId);
            var customer = customers.FirstOrDefault(c => c.Id == id);
            if (customer != null)
            {
                Customer = new CustomerDto(
                    customer.Id, customer.TenantId, customer.Name, customer.Email, customer.Phone,
                    customer.Company, customer.Status, customer.LifetimeValue, customer.RiskScore, customer.CreatedAt
                );
            }
            InitializeSelectLists();
            
            return Page();
        }
    }

    private void InitializeSelectLists()
    {
        Statuses = Enum.GetValues<CustomerStatus>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = s.ToString(),
                Selected = Customer?.Status == s
            }).ToList();
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

