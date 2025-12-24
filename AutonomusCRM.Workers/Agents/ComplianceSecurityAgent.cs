using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

/// <summary>
/// Agente autónomo que monitorea cumplimiento y seguridad
/// </summary>
public class ComplianceSecurityAgent
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ComplianceSecurityAgent> _logger;

    public ComplianceSecurityAgent(
        ITenantRepository tenantRepository,
        IEventBus eventBus,
        ILogger<ComplianceSecurityAgent> logger)
    {
        _tenantRepository = tenantRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessDomainEvent(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ComplianceSecurityAgent processing event {EventType} (Id: {EventId})",
            domainEvent.EventType,
            domainEvent.Id);

        if (domainEvent.TenantId == null)
            return;

        // Verificar kill-switch
        var isKillSwitchEnabled = await _tenantRepository.IsKillSwitchEnabledAsync(domainEvent.TenantId.Value, cancellationToken);
        if (isKillSwitchEnabled)
        {
            _logger.LogWarning(
                "ComplianceSecurityAgent: Kill-switch enabled for Tenant {TenantId}. Blocking event {EventType}",
                domainEvent.TenantId,
                domainEvent.EventType);
            
            // TODO: Bloquear procesamiento del evento
            return;
        }

        // Verificar políticas de compliance
        var complianceCheck = await CheckCompliance(domainEvent, cancellationToken);
        if (!complianceCheck.IsCompliant)
        {
            _logger.LogWarning(
                "ComplianceSecurityAgent: Compliance violation detected for event {EventType}. Reason: {Reason}",
                domainEvent.EventType,
                complianceCheck.Reason);

            // TODO: Bloquear o alertar según política
        }
    }

    private async Task<ComplianceCheckResult> CheckCompliance(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Verificaciones básicas de compliance
        // TODO: Implementar motor de políticas completo

        // Verificar que el evento tenga CorrelationId
        if (domainEvent.CorrelationId == null)
        {
            return new ComplianceCheckResult(false, "Missing CorrelationId");
        }

        // Verificar que el evento tenga TenantId
        if (domainEvent.TenantId == null)
        {
            return new ComplianceCheckResult(false, "Missing TenantId");
        }

        // Verificar tipos de eventos sensibles
        var sensitiveEventTypes = new[] { "User.", "Tenant.KillSwitch", "Customer.RiskScore" };
        if (sensitiveEventTypes.Any(t => domainEvent.EventType.Contains(t)))
        {
            // Eventos sensibles requieren validación adicional
            // TODO: Implementar validaciones específicas
        }

        return new ComplianceCheckResult(true, null);
    }
}

public record ComplianceCheckResult(
    bool IsCompliant,
    string? Reason
);

