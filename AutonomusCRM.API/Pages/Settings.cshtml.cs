using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Tenants;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages;

public class SettingsModel : PageModel
{
    public Tenant? CurrentTenant { get; set; }
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(IServiceProvider serviceProvider, ILogger<SettingsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            if (TenantId != Guid.Empty)
            {
                var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
                CurrentTenant = await tenantRepository.GetByIdAsync(TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
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

