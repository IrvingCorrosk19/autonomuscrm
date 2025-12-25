using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages;

public class AgentsModel : PageModel
{
    public List<AgentInfo> Agents { get; set; } = new();
    public Dictionary<string, Dictionary<string, object>> AgentConfigs { get; set; } = new();
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentsModel> _logger;

    public AgentsModel(IServiceProvider serviceProvider, ILogger<AgentsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Los agentes son servicios que se ejecutan en el Worker
            // Por ahora mostramos información estática sobre los agentes disponibles
            // En el futuro, esto podría conectarse a un servicio de estado del Worker
            
            TenantId = await GetDefaultTenantIdAsync();
            
            // Cargar configuraciones de agentes
            var agentNames = new[] { 
                "LeadIntelligenceAgent", "CustomerRiskAgent", "DealStrategyAgent", 
                "CommunicationAgent", "DataQualityGuardian", "ComplianceSecurityAgent", 
                "AutomationOptimizerAgent" 
            };
            
            foreach (var agentName in agentNames)
            {
                var configQuery = new AutonomusCRM.Application.Agents.Queries.GetAgentConfigQuery(TenantId, agentName);
                var configHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Agents.Queries.GetAgentConfigQuery, Dictionary<string, object>>>();
                AgentConfigs[agentName] = await configHandler.HandleAsync(configQuery, CancellationToken.None);
            }
            
            // Información sobre los agentes autónomos del sistema
            Agents = new List<AgentInfo>
            {
                new AgentInfo 
                { 
                    Name = "Lead Intelligence Agent", 
                    Status = "Active", 
                    Description = "Analiza y califica leads automáticamente basándose en fuente, información disponible y comportamiento", 
                    LastRun = DateTime.UtcNow.AddMinutes(-5),
                    EventsSubscribed = new[] { "LeadCreatedEvent" }
                },
                new AgentInfo 
                { 
                    Name = "Customer Risk Agent", 
                    Status = "Active", 
                    Description = "Evalúa riesgo de clientes, detecta churn potencial y calcula lifetime value", 
                    LastRun = DateTime.UtcNow.AddMinutes(-8),
                    EventsSubscribed = new[] { "CustomerCreatedEvent" }
                },
                new AgentInfo 
                { 
                    Name = "Deal Strategy Agent", 
                    Status = "Active", 
                    Description = "Optimiza estrategias de cierre de deals, calcula probabilidades y sugiere acciones", 
                    LastRun = DateTime.UtcNow.AddMinutes(-3),
                    EventsSubscribed = new[] { "DealCreatedEvent", "DealStageChangedEvent" }
                },
                new AgentInfo 
                { 
                    Name = "Communication Agent", 
                    Status = "Active", 
                    Description = "Gestiona comunicaciones multicanal automáticas basadas en eventos del sistema", 
                    LastRun = DateTime.UtcNow.AddMinutes(-12),
                    EventsSubscribed = new[] { "CustomerCreatedEvent", "LeadCreatedEvent" }
                },
                new AgentInfo 
                { 
                    Name = "Data Quality Guardian", 
                    Status = "Active", 
                    Description = "Detecta y corrige problemas de calidad de datos, valida integridad y consistencia", 
                    LastRun = DateTime.UtcNow.AddMinutes(-15),
                    EventsSubscribed = new[] { "Todos los eventos" }
                },
                new AgentInfo 
                { 
                    Name = "Compliance & Security Agent", 
                    Status = "Active", 
                    Description = "Monitorea compliance, seguridad y auditoría de todos los eventos del sistema", 
                    LastRun = DateTime.UtcNow.AddMinutes(-20),
                    EventsSubscribed = new[] { "IDomainEvent (todos)" }
                },
                new AgentInfo 
                { 
                    Name = "Automation Optimizer Agent", 
                    Status = "Active", 
                    Description = "Optimiza workflows y automatizaciones basándose en métricas de rendimiento", 
                    LastRun = DateTime.UtcNow.AddMinutes(-10),
                    EventsSubscribed = new[] { "Análisis periódico" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading agents information");
            Agents = new List<AgentInfo>();
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

    public async Task<IActionResult> OnPostUpdateAgentConfigAsync(string agentName, string configJson)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new();
            
            var command = new AutonomusCRM.Application.Agents.Commands.UpdateAgentConfigCommand(tenantId, agentName, config);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Agents.Commands.UpdateAgentConfigCommand, bool>>();
            
            var result = await handler.HandleAsync(command, CancellationToken.None);
            
            if (result)
            {
                TempData["SuccessMessage"] = $"Configuración de {agentName} actualizada exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Error al actualizar la configuración de {agentName}.";
            }
            
            return RedirectToPage("/Agents");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent config");
            TempData["ErrorMessage"] = "Error al actualizar la configuración: " + ex.Message;
            return RedirectToPage("/Agents");
        }
    }
}

public class AgentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastRun { get; set; }
    public string[] EventsSubscribed { get; set; } = Array.Empty<string>();
}

