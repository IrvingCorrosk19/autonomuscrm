using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Leads;

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

    [BindProperty]
    public string? Source { get; set; }

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
            _logger.LogError(ex, "Error loading create lead page");
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

            if (string.IsNullOrWhiteSpace(Source))
            {
                ErrorMessage = "La fuente es requerida.";
                return Page();
            }

            if (!Enum.TryParse<LeadSource>(Source, out var leadSource))
            {
                leadSource = LeadSource.Other;
            }

            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateLeadCommand, Guid>>();
            var command = new CreateLeadCommand(TenantId, Name, leadSource, Email, Phone, Company);
            var leadId = await handler.HandleAsync(command);

            _logger.LogInformation("Lead creado exitosamente: {LeadId}", leadId);

            // Redirigir a la página de leads con mensaje de éxito
            return RedirectToPage("/Leads", new { created = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lead");
            ErrorMessage = $"Error al crear el lead: {ex.Message}";
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

