using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AutonomusCRM.Workers.Agents;

public class LeadIntelligenceAgent
{
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<LeadIntelligenceAgent> _logger;

    public LeadIntelligenceAgent(
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IAgentConfigurationService agentConfig,
        ILogger<LeadIntelligenceAgent> logger)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task ProcessLeadCreatedEvent(LeadCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.TenantId == null)
            return;

        var config = await _agentConfig.GetConfigAsync(domainEvent.TenantId.Value, "LeadIntelligenceAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var lead = await _leadRepository.GetByIdAsync(domainEvent.LeadId, cancellationToken);
        if (lead == null || lead.TenantId != domainEvent.TenantId)
            return;

        var score = CalculateLeadScore(lead, config);
        lead.UpdateScore(score);

        await _leadRepository.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var evt in lead.DomainEvents)
            await _eventBus.PublishAsync(evt, cancellationToken);
        lead.ClearDomainEvents();

        _logger.LogInformation("LeadIntelligenceAgent updated score for Lead {LeadId} to {Score}", lead.Id, score);
    }

    private int CalculateLeadScore(Lead lead, Dictionary<string, object> config)
    {
        var score = 0;
        var sourceWeights = GetWeightMap(config, "SourceWeights");
        var contactWeights = GetWeightMap(config, "ContactWeights");

        score += lead.Source switch
        {
            LeadSource.Referral => sourceWeights.GetValueOrDefault("Referral", 30),
            LeadSource.Website => sourceWeights.GetValueOrDefault("Website", 20),
            LeadSource.SocialMedia => sourceWeights.GetValueOrDefault("SocialMedia", 15),
            LeadSource.EmailCampaign => sourceWeights.GetValueOrDefault("EmailCampaign", 10),
            _ => sourceWeights.GetValueOrDefault("Other", 5)
        };

        if (!string.IsNullOrWhiteSpace(lead.Email))
            score += contactWeights.GetValueOrDefault("Email", 15);
        if (!string.IsNullOrWhiteSpace(lead.Phone))
            score += contactWeights.GetValueOrDefault("Phone", 10);
        if (!string.IsNullOrWhiteSpace(lead.Company))
            score += contactWeights.GetValueOrDefault("Company", 20);

        return Math.Min(100, score);
    }

    private static Dictionary<string, int> GetWeightMap(Dictionary<string, object> config, string key)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!config.TryGetValue(key, out var raw) || raw is null)
            return result;

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in je.EnumerateObject())
            {
                if (prop.Value.TryGetInt32(out var v))
                    result[prop.Name] = v;
            }
        }

        return result;
    }
}
