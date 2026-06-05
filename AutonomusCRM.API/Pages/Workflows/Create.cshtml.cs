using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Automation.Workflows.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages.Workflows;

public class CreateModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreateModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateModel(IServiceProvider serviceProvider, ILogger<CreateModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
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
            ModelState.AddModelError("", _localizer["Flash_WorkflowCreateError"].Value);
            return Page();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

