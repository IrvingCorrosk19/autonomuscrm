using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

/// <summary>
/// Agente autónomo que analiza deals y sugiere estrategias de cierre
/// </summary>
public class DealStrategyAgent
{
    private readonly IDealRepository _dealRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DealStrategyAgent> _logger;

    public DealStrategyAgent(
        IDealRepository dealRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<DealStrategyAgent> logger)
    {
        _dealRepository = dealRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessDealCreatedEvent(DealCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DealStrategyAgent processing DealCreatedEvent for Deal {DealId}",
            domainEvent.DealId);

        if (domainEvent.TenantId == null)
            return;

        var deal = await _dealRepository.GetByIdAsync(domainEvent.DealId, cancellationToken);
        if (deal == null || deal.TenantId != domainEvent.TenantId)
            return;

        // Analizar y sugerir estrategia inicial
        await AnalyzeAndSuggestStrategy(deal, cancellationToken);
    }

    public async Task ProcessDealStageChangedEvent(DealStageChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DealStrategyAgent processing DealStageChangedEvent for Deal {DealId}",
            domainEvent.DealId);

        if (domainEvent.TenantId == null)
            return;

        var deal = await _dealRepository.GetByIdAsync(domainEvent.DealId, cancellationToken);
        if (deal == null || deal.TenantId != domainEvent.TenantId)
            return;

        // Re-analizar estrategia cuando cambia la etapa
        await AnalyzeAndSuggestStrategy(deal, cancellationToken);
    }

    private async Task AnalyzeAndSuggestStrategy(Deal deal, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(deal.CustomerId, cancellationToken);
        if (customer == null)
            return;

        // Calcular probabilidad mejorada basada en contexto
        var improvedProbability = CalculateImprovedProbability(deal, customer);
        
        // Detectar si está en riesgo
        var isAtRisk = IsDealAtRisk(deal, customer);

        // Sugerir acciones
        var suggestions = GenerateSuggestions(deal, customer, isAtRisk);

        _logger.LogInformation(
            "DealStrategyAgent analyzed Deal {DealId}: Probability={Probability}%, AtRisk={AtRisk}, Suggestions={SuggestionsCount}",
            deal.Id,
            improvedProbability,
            isAtRisk,
            suggestions.Count);

        // TODO: Guardar sugerencias en metadata del deal o crear tareas
        if (suggestions.Any())
        {
            foreach (var suggestion in suggestions)
            {
                deal.UpdateMetadata($"StrategySuggestion_{Guid.NewGuid()}", suggestion);
            }
            await _dealRepository.UpdateAsync(deal, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private int CalculateImprovedProbability(Deal deal, Domain.Customers.Customer customer)
    {
        var baseProbability = deal.Probability ?? 0;

        // Ajustar según lifetime value del cliente
        if (customer.LifetimeValue.HasValue && customer.LifetimeValue > 10000)
            baseProbability += 10;

        // Ajustar según riesgo del cliente
        if (customer.RiskScore.HasValue && customer.RiskScore < 30)
            baseProbability += 5;
        else if (customer.RiskScore.HasValue && customer.RiskScore > 70)
            baseProbability -= 10;

        // Ajustar según tiempo en etapa
        var daysInStage = (DateTime.UtcNow - deal.CreatedAt).TotalDays;
        if (daysInStage > 30)
            baseProbability -= 5; // Disminuye si está estancado

        return Math.Max(0, Math.Min(100, baseProbability));
    }

    private bool IsDealAtRisk(Deal deal, Domain.Customers.Customer customer)
    {
        // Deal está en riesgo si:
        // - Probabilidad baja y tiempo en etapa > 20 días
        // - Cliente con alto riesgo
        // - Fecha de cierre esperada pasada
        // - Sin actividad reciente

        var daysInStage = (DateTime.UtcNow - deal.CreatedAt).TotalDays;
        var lowProbability = (deal.Probability ?? 0) < 30;
        var highRiskCustomer = customer.RiskScore.HasValue && customer.RiskScore > 70;
        var pastDueDate = deal.ExpectedCloseDate.HasValue && deal.ExpectedCloseDate < DateTime.UtcNow;

        return (lowProbability && daysInStage > 20) || highRiskCustomer || pastDueDate;
    }

    private List<string> GenerateSuggestions(Deal deal, Domain.Customers.Customer customer, bool isAtRisk)
    {
        var suggestions = new List<string>();

        if (isAtRisk)
        {
            suggestions.Add("Deal en riesgo detectado. Revisar objeciones del cliente.");
            suggestions.Add("Considerar ajuste de precio o términos.");
            suggestions.Add("Agendar llamada de seguimiento urgente.");
        }

        if (deal.Stage == DealStage.Prospecting)
        {
            suggestions.Add("Calificar lead con preguntas clave de negocio.");
        }
        else if (deal.Stage == DealStage.Qualification)
        {
            suggestions.Add("Preparar propuesta personalizada basada en necesidades.");
        }
        else if (deal.Stage == DealStage.Proposal)
        {
            suggestions.Add("Seguimiento activo de propuesta enviada.");
        }
        else if (deal.Stage == DealStage.Negotiation)
        {
            suggestions.Add("Identificar puntos de negociación críticos.");
        }

        if (customer.LifetimeValue.HasValue && customer.LifetimeValue > 50000)
        {
            suggestions.Add("Cliente de alto valor. Priorizar atención personalizada.");
        }

        return suggestions;
    }
}

