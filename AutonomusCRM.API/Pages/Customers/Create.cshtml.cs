using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateModel(IServiceProvider serviceProvider, ILogger<CreateModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
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
            ErrorMessage = _localizer["Flash_PageLoadError"].Value;
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = _localizer["Flash_NameRequired"].Value;
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
            ErrorMessage = _localizer["Flash_CustomerCreateError", ex.Message].Value;
            return Page();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}
