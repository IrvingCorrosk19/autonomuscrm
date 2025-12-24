using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

public class AgentsModel : PageModel
{
    public List<AgentInfo> Agents { get; set; } = new();

    public void OnGet()
    {
        Agents = new List<AgentInfo>
        {
            new AgentInfo { Name = "Lead Intelligence Agent", Status = "Active", Description = "Analiza y califica leads automáticamente", LastRun = DateTime.UtcNow.AddMinutes(-5) },
            new AgentInfo { Name = "Customer Risk Agent", Status = "Active", Description = "Evalúa riesgo de clientes y detecta churn", LastRun = DateTime.UtcNow.AddMinutes(-8) },
            new AgentInfo { Name = "Deal Strategy Agent", Status = "Active", Description = "Optimiza estrategias de cierre de deals", LastRun = DateTime.UtcNow.AddMinutes(-3) },
            new AgentInfo { Name = "Communication Agent", Status = "Active", Description = "Gestiona comunicaciones multicanal", LastRun = DateTime.UtcNow.AddMinutes(-12) },
            new AgentInfo { Name = "Data Quality Guardian", Status = "Active", Description = "Detecta y corrige problemas de calidad de datos", LastRun = DateTime.UtcNow.AddMinutes(-15) },
            new AgentInfo { Name = "Compliance & Security Agent", Status = "Active", Description = "Monitorea compliance y seguridad", LastRun = DateTime.UtcNow.AddMinutes(-20) },
            new AgentInfo { Name = "Automation Optimizer Agent", Status = "Active", Description = "Optimiza workflows y automatizaciones", LastRun = DateTime.UtcNow.AddMinutes(-10) }
        };
    }
}

public class AgentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastRun { get; set; }
}

