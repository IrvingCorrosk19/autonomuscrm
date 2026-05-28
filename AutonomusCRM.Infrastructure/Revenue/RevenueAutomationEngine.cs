using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Leads.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Revenue;

public class RevenueAutomationEngine : IRevenueAutomationEngine
{
    private const int HighScoreThreshold = 70;
    private const int StagnantDays = 14;
    private const int InactivityHours = 48;

    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IOperationalTaskService _taskService;
    private readonly ICommercialSlaEngine _slaEngine;
    private readonly ISmartAssignmentEngine _assignment;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<RevenueAutomationEngine> _logger;

    public RevenueAutomationEngine(
        ILeadRepository leadRepository,
        IDealRepository dealRepository,
        IOperationalTaskService taskService,
        ICommercialSlaEngine slaEngine,
        ISmartAssignmentEngine assignment,
        IAgentConfigurationService agentConfig,
        ILogger<RevenueAutomationEngine> logger)
    {
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
        _taskService = taskService;
        _slaEngine = slaEngine;
        _assignment = assignment;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId == null)
            return;

        var tenantId = domainEvent.TenantId.Value;

        switch (domainEvent)
        {
            case LeadCreatedEvent lce:
                await _slaEngine.EnforceLeadCreatedSlaAsync(tenantId, lce.LeadId, cancellationToken);
                break;
            case LeadScoreUpdatedEvent lse:
                await OnLeadScoredAsync(tenantId, lse.LeadId, cancellationToken);
                break;
            case LeadQualifiedEvent lqe:
                await OnLeadQualifiedSlaAsync(tenantId, lqe.LeadId, cancellationToken);
                break;
        }
    }

    public async Task RunPeriodicRevenueScanAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await ScanStagnantDealsAsync(tenantId, cancellationToken);
        await ScanInactiveLeadsAsync(tenantId, cancellationToken);
    }

    private async Task OnLeadScoredAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken)
    {
        var config = await _agentConfig.GetConfigAsync(tenantId, "LeadIntelligenceAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead == null || !lead.Score.HasValue || lead.Score < HighScoreThreshold)
            return;

        await _assignment.AssignLeadToBestRepAsync(tenantId, leadId, cancellationToken);
        _logger.LogInformation("RevenueAutomation: high-score lead {LeadId} assigned", leadId);
    }

    private async Task OnLeadQualifiedSlaAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken)
    {
        if (await _taskService.ExistsOpenTaskAsync(tenantId, "Lead", leadId, "SLA_QualifiedFollowUp", cancellationToken))
            return;

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead == null)
            return;

        await _taskService.CreateTaskAsync(
            tenantId,
            $"SLA: Seguimiento post-calificación — {lead.Name}",
            "Avanzar oportunidad en 48h.",
            "Lead",
            leadId,
            lead.AssignedToUserId,
            DateTime.UtcNow.AddHours(48),
            "High",
            "SLA_QualifiedFollowUp",
            cancellationToken);
    }

    private async Task ScanStagnantDealsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var deals = await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var deal in deals.Where(d => d.Status == DealStatus.Open))
        {
            var days = (DateTime.UtcNow - (deal.UpdatedAt ?? deal.CreatedAt)).TotalDays;
            if (days < StagnantDays)
                continue;

            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Deal", deal.Id, "StagnantEscalation", cancellationToken))
                continue;

            await _taskService.CreateTaskAsync(
                tenantId,
                $"Escalar: deal estancado {deal.Title}",
                $"Sin movimiento {days:F0} días — revisión gerente.",
                "Deal",
                deal.Id,
                deal.AssignedToUserId,
                DateTime.UtcNow.AddDays(2),
                "Urgent",
                "StagnantEscalation",
                cancellationToken);
        }
    }

    private async Task ScanInactiveLeadsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var leads = await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var lead in leads.Where(l => l.Status is LeadStatus.New or LeadStatus.Contacted))
        {
            if ((DateTime.UtcNow - (lead.UpdatedAt ?? lead.CreatedAt)).TotalHours < InactivityHours)
                continue;

            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Lead", lead.Id, "Inactivity48h", cancellationToken))
                continue;

            await _taskService.CreateTaskAsync(
                tenantId,
                $"48h sin actividad — {lead.Name}",
                "Reactivar lead con contacto o descalificar.",
                "Lead",
                lead.Id,
                lead.AssignedToUserId,
                DateTime.UtcNow.AddDays(1),
                "High",
                "Inactivity48h",
                cancellationToken);
        }
    }
}
