using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Infrastructure.Persistence.EventStore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages;

public class AuditModel : PageModel
{
    public List<AuditEntry> AuditEntries { get; set; } = new();
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditModel> _logger;

    public AuditModel(IServiceProvider serviceProvider, ILogger<AuditModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            // Obtener eventos recientes del Event Store
            var eventStore = _serviceProvider.GetRequiredService<Application.Events.EventSourcing.IEventStore>();
            // Por ahora, mostramos eventos de ejemplo ya que necesitaríamos un método para obtener eventos por tenant
            AuditEntries = new List<AuditEntry>
            {
                new AuditEntry { Action = "Lead Created", User = "System", Timestamp = DateTime.UtcNow.AddMinutes(-10), Details = "New lead created via API" },
                new AuditEntry { Action = "Deal Updated", User = "System", Timestamp = DateTime.UtcNow.AddMinutes(-25), Details = "Deal stage changed to Proposal" },
                new AuditEntry { Action = "Customer Risk Assessed", User = "CustomerRiskAgent", Timestamp = DateTime.UtcNow.AddMinutes(-30), Details = "Risk score calculated: 45" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit log");
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

public class AuditEntry
{
    public string Action { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;
}

