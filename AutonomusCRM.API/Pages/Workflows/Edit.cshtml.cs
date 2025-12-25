using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Automation.Workflows.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Workflows;

public class EditModel : PageModel
{
    public Workflow? Workflow { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IServiceProvider serviceProvider, ILogger<EditModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var workflowRepository = _serviceProvider.GetRequiredService<IWorkflowRepository>();
            var workflow = await workflowRepository.GetByIdAsync(id, CancellationToken.None);
            
            if (workflow == null || workflow.TenantId != tenantId)
            {
                return NotFound();
            }

            Workflow = workflow;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflow for edit");
            return RedirectToPage("/Workflows");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string name, string? description, bool? isActive)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new UpdateWorkflowCommand(id, tenantId, name, description, isActive);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateWorkflowCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Workflows");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow");
            ModelState.AddModelError("", "Error al actualizar el workflow: " + ex.Message);
            
            // Recargar datos
            var tenantId = await GetDefaultTenantIdAsync();
            var workflowRepository = _serviceProvider.GetRequiredService<IWorkflowRepository>();
            var workflow = await workflowRepository.GetByIdAsync(id, CancellationToken.None);
            if (workflow != null && workflow.TenantId == tenantId)
            {
                Workflow = workflow;
            }
            
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDuplicateAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new DuplicateWorkflowCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<DuplicateWorkflowCommand, Guid>>();
            
            var newWorkflowId = await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Workflows/Edit", new { id = newWorkflowId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating workflow");
            return RedirectToPage("/Workflows/Edit", new { id });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new DeleteWorkflowCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<DeleteWorkflowCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Workflows");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow");
            return RedirectToPage("/Workflows/Edit", new { id });
        }
    }

    public async Task<IActionResult> OnPostAddTriggerAsync(Guid id, string type, string eventType)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AddWorkflowTriggerCommand(id, tenantId, type, eventType);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AddWorkflowTriggerCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Workflows/Edit", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding trigger");
            return RedirectToPage("/Workflows/Edit", new { id });
        }
    }

    public async Task<IActionResult> OnPostAddConditionAsync(Guid id, string type, string expression)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AddWorkflowConditionCommand(id, tenantId, type, expression);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AddWorkflowConditionCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Workflows/Edit", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding condition");
            return RedirectToPage("/Workflows/Edit", new { id });
        }
    }

    public async Task<IActionResult> OnPostAddActionAsync(Guid id, string type, string target)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AddWorkflowActionCommand(id, tenantId, type, target);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AddWorkflowActionCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Workflows/Edit", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding action");
            return RedirectToPage("/Workflows/Edit", new { id });
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

