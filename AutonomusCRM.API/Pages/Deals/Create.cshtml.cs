using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Customers.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Deals;

public class CreateModel : PageModel
{
    [BindProperty]
    public Guid? CustomerId { get; set; }

    [BindProperty]
    public string? Title { get; set; }

    [BindProperty]
    public decimal? Amount { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    public List<CustomerDto> Customers { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid TenantId { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IServiceProvider serviceProvider, ILogger<CreateModel> logger)
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
            _logger.LogError(ex, "Error loading create deal page");
            ErrorMessage = "Error al cargar la página. Por favor, intenta nuevamente.";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();

            if (!CustomerId.HasValue)
            {
                ErrorMessage = "Debes seleccionar un cliente.";
                await LoadCustomersAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "El título es requerido.";
                await LoadCustomersAsync();
                return Page();
            }

            if (!Amount.HasValue || Amount.Value <= 0)
            {
                ErrorMessage = "El monto debe ser mayor a cero.";
                await LoadCustomersAsync();
                return Page();
            }

            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
            var command = new CreateDealCommand(TenantId, CustomerId.Value, Title, Amount.Value, Description);
            var dealId = await handler.HandleAsync(command);

            _logger.LogInformation("Deal creado exitosamente: {DealId}", dealId);

            return RedirectToPage("/Deals", new { created = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal");
            ErrorMessage = $"Error al crear el deal: {ex.Message}";
            await LoadCustomersAsync();
            return Page();
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
        }
        catch
        {
            Customers = new List<CustomerDto>();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default tenant");
            return Guid.Empty;
        }
    }
}

