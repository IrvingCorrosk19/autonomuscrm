using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Automation.Workflows.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

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

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        try
        {
            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync(CancellationToken.None);
            var tenant = tenants.FirstOrDefault();
            
            if (tenant == null)
            {
                var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand, Guid>>();
                var tenantId = await createHandler.HandleAsync(
                    new AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand("Default Tenant", "default@autonomuscrm.com"),
                    CancellationToken.None);
                return tenantId;
            }
            
            return tenant.Id;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}

