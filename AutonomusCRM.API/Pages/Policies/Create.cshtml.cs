using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Application.Policies.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages.Policies;

public class CreateModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IServiceProvider serviceProvider, ILogger<CreateModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string name, string expression, string? description)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new CreatePolicyCommand(tenantId, name, expression, description);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreatePolicyCommand, Guid>>();
            
            var policyId = await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Policies");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating policy");
            ModelState.AddModelError("", "Error al crear la política: " + ex.Message);
            return Page();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

