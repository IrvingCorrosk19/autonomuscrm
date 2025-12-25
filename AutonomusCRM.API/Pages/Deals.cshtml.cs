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
    public List<DealDto> FilteredDeals { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public AutonomusCRM.Domain.Deals.DealStatus? FilterStatus { get; set; }
    public AutonomusCRM.Domain.Deals.DealStage? FilterStage { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DealsModel> _logger;

    public DealsModel(IServiceProvider serviceProvider, ILogger<DealsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool? Created { get; set; }

    public async Task OnGetAsync(bool? created = null, string? search = null, AutonomusCRM.Domain.Deals.DealStatus? status = null, AutonomusCRM.Domain.Deals.DealStage? stage = null, int? bulkUpdated = null, int? imported = null)
    {
        try
        {
            Created = created;
            SearchTerm = search;
            FilterStatus = status;
            FilterStage = stage;
            TenantId = await GetDefaultTenantIdAsync();
            
            var dealsHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
            var dealsQuery = new GetDealsByTenantQuery(TenantId, status, stage);
            var deals = await dealsHandler.HandleAsync(dealsQuery);
            Deals = deals.ToList();

            // Para el formulario, necesitamos todos los customers
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
            
            // Aplicar búsqueda adicional
            FilteredDeals = Deals.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                FilteredDeals = FilteredDeals.Where(d => 
                    (d.Title?.ToLower().Contains(searchLower) ?? false)
                );
            }
            
            FilteredDeals = FilteredDeals.ToList();
            
            if (bulkUpdated.HasValue && bulkUpdated.Value > 0)
            {
                TempData["Message"] = $"Se actualizaron {bulkUpdated.Value} deals correctamente.";
            }
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = $"Se importaron {imported.Value} deals correctamente.";
            }
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

