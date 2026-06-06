using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Automation.Workflows.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace AutonomusCRM.API.Pages.Workflows;

public class ImportModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ImportModel(IServiceProvider serviceProvider, ILogger<ImportModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", _localizer["Import_Error_SelectFile"].Value);
                return RedirectToPage("/Workflows");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            
            var workflows = JsonSerializer.Deserialize<List<WorkflowImportDto>>(json);
            
            if (workflows == null || !workflows.Any())
            {
                ModelState.AddModelError("", _localizer["Import_Error_NoValidWorkflows"].Value);
                return RedirectToPage("/Workflows");
            }

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<CreateWorkflowCommand, Guid>>();
            var createdCount = 0;

            foreach (var workflowDto in workflows)
            {
                try
                {
                    var command = new CreateWorkflowCommand(tenantId, workflowDto.Name, workflowDto.Description);
                    await createHandler.HandleAsync(command, CancellationToken.None);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error importing workflow {Name}", workflowDto.Name);
                }
            }

            return RedirectToPage("/Workflows", new { imported = createdCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing workflows");
            ModelState.AddModelError("", _localizer["Import_Error_ImportWorkflowsFailed", ex.Message].Value);
            return RedirectToPage("/Workflows");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);

    private class WorkflowImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
