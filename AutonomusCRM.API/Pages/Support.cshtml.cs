using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AutonomusCRM.API.Pages;

public class SupportModel : PageModel
{
    public string DatabaseStatus { get; set; } = "Unknown";
    public string EventBusStatus { get; set; } = "Unknown";
    public string CacheStatus { get; set; } = "Unknown";
    public Guid TenantId { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SupportModel> _logger;

    public SupportModel(IServiceProvider serviceProvider, ILogger<SupportModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();

            // Verificar estado de health checks
            var healthCheckService = _serviceProvider.GetService<HealthCheckService>();
            if (healthCheckService != null)
            {
                var health = await healthCheckService.CheckHealthAsync();
                
                foreach (var entry in health.Entries)
                {
                    var status = entry.Value.Status.ToString();
                    switch (entry.Key.ToLower())
                    {
                        case "database":
                            DatabaseStatus = status;
                            break;
                        case "eventbus":
                            EventBusStatus = status;
                            break;
                        case "cache":
                            CacheStatus = status;
                            break;
                    }
                }
            }
            else
            {
                // Si no hay health check service, asumir que está operativo
                DatabaseStatus = "Healthy";
                EventBusStatus = "Healthy";
                CacheStatus = "Healthy";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading support page");
            DatabaseStatus = "Unhealthy";
            EventBusStatus = "Unhealthy";
            CacheStatus = "Unhealthy";
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default tenant");
            return Guid.Empty;
        }
    }
}

