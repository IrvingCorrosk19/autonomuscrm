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

public class EditModel : PageModel
{
    public Workflow? Workflow { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EditModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EditModel(IServiceProvider serviceProvider, ILogger<EditModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
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
            ModelState.AddModelError("", _localizer["Flash_WorkflowUpdateError"].Value);
            
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

    public async Task<IActionResult> OnPostAddActionAsync(
        Guid id, string type, string target,
        string? param_userId, string? param_status, string? param_title,
        string? param_description, string? param_task_userId, string? param_dueDate, string? param_priority)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(param_userId)) parameters["userId"] = param_userId;
            if (!string.IsNullOrWhiteSpace(param_status)) parameters["status"] = param_status;
            if (!string.IsNullOrWhiteSpace(param_title)) parameters["title"] = param_title;
            if (!string.IsNullOrWhiteSpace(param_description)) parameters["description"] = param_description;
            if (!string.IsNullOrWhiteSpace(param_task_userId)) parameters["userId"] = param_task_userId;
            if (!string.IsNullOrWhiteSpace(param_dueDate) && DateTime.TryParse(param_dueDate, out var due))
                parameters["dueDate"] = due.ToUniversalTime().ToString("O");
            if (!string.IsNullOrWhiteSpace(param_priority)) parameters["priority"] = param_priority;

            var command = new AddWorkflowActionCommand(id, tenantId, type, target, parameters);
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
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

