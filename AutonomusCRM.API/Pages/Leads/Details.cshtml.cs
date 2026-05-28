using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Queries;
using AutonomusCRM.Application.Tenants.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages.Leads;

public class DetailsModel : PageModel
{
    public LeadDto? Lead { get; set; }
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
            var query = new GetLeadsByTenantQuery(tenantId, null);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var leads = await handler.HandleAsync(query, CancellationToken.None);
            
            Lead = leads.FirstOrDefault(l => l.Id == id);
            
            if (Lead == null)
            {
                TempData["ErrorMessage"] = "Lead no encontrado.";
                return RedirectToPage("/Leads");
            }
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading lead details");
            return RedirectToPage("/Leads");
        }
    }

    [Authorize(Roles = "Admin,Manager,Sales")]
    public async Task<IActionResult> OnPostQualifyAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AutonomusCRM.Application.Leads.Commands.QualifyLeadCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Leads.Commands.QualifyLeadCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Leads/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error qualifying lead");
            return RedirectToPage("/Leads/Details", new { id });
        }
    }

    [Authorize(Roles = "Admin,Manager,Sales")]
    public async Task<IActionResult> OnPostConvertToCustomerAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var leadQuery = new GetLeadsByTenantQuery(tenantId, null);
            var leadHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var leads = await leadHandler.HandleAsync(leadQuery, CancellationToken.None);
            var lead = leads.FirstOrDefault(l => l.Id == id);
            
            if (lead == null)
            {
                return NotFound();
            }

            // Crear customer desde lead
            var createCustomerCommand = new AutonomusCRM.Application.Customers.Commands.CreateCustomerCommand(
                tenantId, lead.Name, lead.Email, lead.Phone, lead.Company);
            var createCustomerHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Customers.Commands.CreateCustomerCommand, Guid>>();
            var customerId = await createCustomerHandler.HandleAsync(createCustomerCommand, CancellationToken.None);
            
            // Convertir lead
            var leadRepository = _serviceProvider.GetRequiredService<ILeadRepository>();
            var leadEntity = await leadRepository.GetByIdAsync(id, CancellationToken.None);
            if (leadEntity != null)
            {
                leadEntity.ConvertToCustomer(customerId);
                await leadRepository.UpdateAsync(leadEntity, CancellationToken.None);
                var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
            
            return RedirectToPage("/Customers/Details", new { id = customerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting lead to customer");
            return RedirectToPage("/Leads/Details", new { id });
        }
    }

    [Authorize(Roles = "Admin,Manager,Sales")]
    public async Task<IActionResult> OnPostCreateDealAsync(Guid id, string title, decimal amount, string? description)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var leadQuery = new GetLeadsByTenantQuery(tenantId, null);
            var leadHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var leads = await leadHandler.HandleAsync(leadQuery, CancellationToken.None);
            var lead = leads.FirstOrDefault(l => l.Id == id);
            
            if (lead == null)
            {
                return NotFound();
            }

            // Buscar o crear customer
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(tenantId);
            var customer = customers.FirstOrDefault(c => c.Email == lead.Email);
            
            Guid customerId;
            if (customer == null)
            {
                var createCustomerCommand = new AutonomusCRM.Application.Customers.Commands.CreateCustomerCommand(
                    tenantId, lead.Name, lead.Email, lead.Phone, lead.Company);
                var createCustomerHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Customers.Commands.CreateCustomerCommand, Guid>>();
                customerId = await createCustomerHandler.HandleAsync(createCustomerCommand, CancellationToken.None);
            }
            else
            {
                customerId = customer.Id;
            }

            // Crear deal
            var createDealCommand = new AutonomusCRM.Application.Deals.Commands.CreateDealCommand(
                tenantId, customerId, title, amount, description);
            var createDealHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Deals.Commands.CreateDealCommand, Guid>>();
            var dealId = await createDealHandler.HandleAsync(createDealCommand, CancellationToken.None);
            
            return RedirectToPage("/Deals/Details", new { id = dealId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal from lead");
            return RedirectToPage("/Leads/Details", new { id });
        }
    }

    [Authorize(Roles = "Admin,Manager,Sales")]
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AutonomusCRM.Application.Leads.Commands.DeleteLeadCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Leads.Commands.DeleteLeadCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Leads");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lead");
            return RedirectToPage("/Leads/Details", new { id });
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

