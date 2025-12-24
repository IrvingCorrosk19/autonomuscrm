using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

/// <summary>
/// Agente autónomo que analiza leads y actualiza su score automáticamente
/// </summary>
public class LeadIntelligenceAgent
{
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<LeadIntelligenceAgent> _logger;

    public LeadIntelligenceAgent(
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<LeadIntelligenceAgent> logger)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessLeadCreatedEvent(LeadCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "LeadIntelligenceAgent processing LeadCreatedEvent for Lead {LeadId}",
            domainEvent.LeadId);

        if (domainEvent.TenantId == null)
            return;

        var lead = await _leadRepository.GetByIdAsync(domainEvent.LeadId, cancellationToken);
        if (lead == null || lead.TenantId != domainEvent.TenantId)
            return;

        // Lógica de scoring automático
        var score = CalculateLeadScore(lead);
        lead.UpdateScore(score);

        await _leadRepository.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventBus.PublishAsync(lead.DomainEvents.First(), cancellationToken);
        lead.ClearDomainEvents();

        _logger.LogInformation(
            "LeadIntelligenceAgent updated score for Lead {LeadId} to {Score}",
            lead.Id,
            score);
    }

    private int CalculateLeadScore(Lead lead)
    {
        var score = 0;

        // Scoring basado en fuente
        score += lead.Source switch
        {
            LeadSource.Referral => 30,
            LeadSource.Website => 20,
            LeadSource.SocialMedia => 15,
            LeadSource.EmailCampaign => 10,
            _ => 5
        };

        // Scoring basado en información disponible
        if (!string.IsNullOrWhiteSpace(lead.Email))
            score += 15;
        if (!string.IsNullOrWhiteSpace(lead.Phone))
            score += 10;
        if (!string.IsNullOrWhiteSpace(lead.Company))
            score += 20;

        return Math.Min(score, 100);
    }
}

