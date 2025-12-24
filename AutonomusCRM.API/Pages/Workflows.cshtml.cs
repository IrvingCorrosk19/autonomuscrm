using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

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

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var workflowRepository = _serviceProvider.GetRequiredService<IWorkflowRepository>();
            var workflows = await workflowRepository.GetActiveByTenantAsync(TenantId);
            Workflows = workflows.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflows");
        }
    }

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        try
        {
            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync();
            var firstTenant = tenants.FirstOrDefault();
            
            if (firstTenant != null)
                return firstTenant.Id;

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand, Guid>>();
            var createCommand = new AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand("Default Tenant", "Tenant por defecto");
            return await createHandler.HandleAsync(createCommand);
        }
        catch
        {
            return Guid.Empty;
        }
    }
}

