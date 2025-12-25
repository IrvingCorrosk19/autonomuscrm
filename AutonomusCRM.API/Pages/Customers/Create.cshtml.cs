using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Customers;

public class CreateModel : PageModel
{
    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Phone { get; set; }

    [BindProperty]
    public string? Company { get; set; }

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create customer page");
            ErrorMessage = "Error al cargar la página. Por favor, intenta nuevamente.";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "El nombre es requerido.";
                return Page();
            }

            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateCustomerCommand, Guid>>();
            var command = new CreateCustomerCommand(TenantId, Name, Email, Phone, Company);
            var customerId = await handler.HandleAsync(command);

            _logger.LogInformation("Customer creado exitosamente: {CustomerId}", customerId);

            return RedirectToPage("/Customers", new { created = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            ErrorMessage = $"Error al crear el cliente: {ex.Message}";
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default tenant");
            return Guid.Empty;
        }
    }
}

