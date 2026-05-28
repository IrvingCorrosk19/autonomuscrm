using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages;

public class WorkflowsModel : PageModel
{
    public List<Workflow> Workflows { get; set; } = new();
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowsModel> _logger;

    public WorkflowsModel(IServiceProvider serviceProvider, ILogger<WorkflowsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
                TempData["Message"] = $"Se importaron {imported.Value} workflows correctamente.";
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

