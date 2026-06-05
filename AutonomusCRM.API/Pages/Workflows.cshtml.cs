using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages;

public class WorkflowsModel : PageModel
{
    public List<Workflow> Workflows { get; set; } = new();
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowsModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WorkflowsModel(IServiceProvider serviceProvider, ILogger<WorkflowsModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task OnGetAsync(int? imported = null)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var workflowRepository = _serviceProvider.GetRequiredService<IWorkflowRepository>();
            var workflows = await workflowRepository.GetActiveByTenantAsync(TenantId);
            Workflows = workflows.ToList();
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = _localizer["Flash_WorkflowsImported", imported.Value].Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflows");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

