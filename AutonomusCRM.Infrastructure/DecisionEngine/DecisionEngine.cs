using AutonomusCRM.Application.DecisionEngine;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DecisionEngine;

/// <summary>
/// Implementación del Autonomous Decision Engine
/// </summary>
public class DecisionEngine : IDecisionEngine
{
    private readonly ILogger<DecisionEngine> _logger;

    public DecisionEngine(ILogger<DecisionEngine> logger)
    {
        _logger = logger;
    }

    public async Task<Decision> MakeDecisionAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DecisionEngine making decision for event {EventType} (Id: {EventId})",
            domainEvent.EventType,
            domainEvent.Id);

        // Analizar contexto
        var context = AnalyzeContext(domainEvent);
        
        // Aplicar reglas de negocio
        var action = ApplyBusinessRules(domainEvent, context);
        
        // Calcular impacto y prioridad
        var impact = CalculateImpact(domainEvent, action);
        var priority = CalculatePriority(impact, context);
        
        // Generar razón
        var reason = GenerateReason(domainEvent, action, context);

        var decision = new Decision(
            Id: Guid.NewGuid(),
            Type: domainEvent.EventType,
            Action: action,
            Priority: priority,
            Impact: impact,
            Reason: reason,
            Context: context,
            CreatedAt: DateTime.UtcNow
        );

        _logger.LogInformation(
            "DecisionEngine made decision: {Action} (Priority: {Priority}, Impact: {Impact})",
            action,
            priority,
            impact);

        return decision;
    }

    public async Task<List<Decision>> PrioritizeDecisionsAsync(List<Decision> decisions, CancellationToken cancellationToken = default)
    {
        return decisions
            .OrderByDescending(d => d.Priority)
            .ThenByDescending(d => d.Impact)
            .ThenBy(d => d.CreatedAt)
            .ToList();
    }

    public async Task<string> ExplainDecisionAsync(Decision decision, CancellationToken cancellationToken = default)
    {
        var explanation = $@"
DECISIÓN: {decision.Action}
TIPO: {decision.Type}
PRIORIDAD: {decision.Priority}/100
IMPACTO: {decision.Impact:F2}
RAZÓN: {decision.Reason}
CONTEXTO: {string.Join(", ", decision.Context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}
FECHA: {decision.CreatedAt:yyyy-MM-dd HH:mm:ss}
";

        return explanation.Trim();
    }

    private Dictionary<string, object> AnalyzeContext(IDomainEvent domainEvent)
    {
        var context = new Dictionary<string, object>
        {
            ["EventType"] = domainEvent.EventType,
            ["EventId"] = domainEvent.Id,
            ["TenantId"] = domainEvent.TenantId?.ToString() ?? "Unknown",
            ["CorrelationId"] = domainEvent.CorrelationId?.ToString() ?? "Unknown",
            ["OccurredOn"] = domainEvent.OccurredOn
        };

        // TODO: Agregar más contexto según el tipo de evento
        return context;
    }

    private string ApplyBusinessRules(IDomainEvent domainEvent, Dictionary<string, object> context)
    {
        // Reglas de negocio básicas
        // TODO: Implementar motor de reglas más sofisticado

        if (domainEvent.EventType.Contains("Customer") && domainEvent.EventType.Contains("Created"))
        {
            return "SendWelcomeEmail";
        }

        if (domainEvent.EventType.Contains("Lead") && domainEvent.EventType.Contains("Qualified"))
        {
            return "AssignToSalesRep";
        }

        if (domainEvent.EventType.Contains("Deal") && domainEvent.EventType.Contains("StageChanged"))
        {
            return "UpdateProbability";
        }

        return "NoAction";
    }

    private decimal CalculateImpact(IDomainEvent domainEvent, string action)
    {
        // Cálculo básico de impacto
        // TODO: Implementar cálculo más sofisticado basado en datos históricos

        var baseImpact = 1.0m;

        if (action == "SendWelcomeEmail")
            return baseImpact * 0.5m; // Bajo impacto

        if (action == "AssignToSalesRep")
            return baseImpact * 2.0m; // Alto impacto

        if (action == "UpdateProbability")
            return baseImpact * 1.5m; // Medio impacto

        return baseImpact;
    }

    private int CalculatePriority(decimal impact, Dictionary<string, object> context)
    {
        // Prioridad basada en impacto
        // TODO: Implementar cálculo más sofisticado

        if (impact >= 2.0m)
            return 90; // Alta prioridad
        if (impact >= 1.5m)
            return 70; // Media-alta prioridad
        if (impact >= 1.0m)
            return 50; // Media prioridad

        return 30; // Baja prioridad
    }

    private string GenerateReason(IDomainEvent domainEvent, string action, Dictionary<string, object> context)
    {
        return $"Evento {domainEvent.EventType} requiere acción {action} basado en reglas de negocio y contexto actual.";
    }
}

