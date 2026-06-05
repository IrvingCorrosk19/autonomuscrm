using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

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
            _logger.LogError(ex, "Error loading create lead page");
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

            if (string.IsNullOrWhiteSpace(Source))
            {
                ErrorMessage = _localizer["Flash_SourceRequired"].Value;
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

            return RedirectToPage("/Leads", new { created = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lead");
            
            if (ex.Message.Contains("None of the specified endpoints were reachable") || 
                ex.Message.Contains("No connection could be made") ||
                ex.InnerException?.Message?.Contains("No connection could be made") == true)
            {
                ErrorMessage = _localizer["Flash_DbConnectionError"].Value;
            }
            else if (ex.Message.Contains("timeout") || ex.Message.Contains("Timeout"))
            {
                ErrorMessage = _localizer["Flash_DbTimeout"].Value;
            }
            else if (ex.Message.Contains("authentication") || ex.Message.Contains("password"))
            {
                ErrorMessage = _localizer["Flash_DbAuthError"].Value;
            }
            else
            {
                ErrorMessage = _localizer["Flash_LeadCreateError", ex.Message].Value;
            }
            
            return Page();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}
