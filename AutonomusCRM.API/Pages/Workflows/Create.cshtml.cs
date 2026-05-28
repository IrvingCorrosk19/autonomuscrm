using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Automation.Workflows.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages.Workflows;

public class CreateModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IServiceProvider serviceProvider, ILogger<CreateModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string name, string? description)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new CreateWorkflowCommand(tenantId, name, description);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateWorkflowCommand, Guid>>();
            
            var workflowId = await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Workflows");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            ModelState.AddModelError("", "Error al crear el workflow: " + ex.Message);
            return Page();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

