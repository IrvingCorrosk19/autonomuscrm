using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class AutonomousOrchestrationEngine : IAutonomousOrchestrationEngine
{
    private readonly IAutonomousRevenueDecisionEngine _decisions;
    private readonly IRevenueAutonomousAgent _revenueAgent;
    private readonly IRenewalAutonomousAgent _renewalAgent;
    private readonly IChurnAutonomousAgent _churnAgent;
    private readonly IExpansionAutonomousAgent _expansionAgent;
    private readonly ICustomerAutonomousAgent _customerAgent;
    private readonly IOperationsAutonomousAgent _operationsAgent;
    private readonly IMlFoundationService _mlFoundation;
    private readonly IIntelligenceAutomationEngine _intelligence;
    private readonly IEnterpriseAiCycleService _enterpriseAi;
    private readonly IAutonomousPlatformGate _gate;
    private readonly ILogger<AutonomousOrchestrationEngine> _logger;

    public AutonomousOrchestrationEngine(
        IAutonomousRevenueDecisionEngine decisions,
        IRevenueAutonomousAgent revenueAgent,
        IRenewalAutonomousAgent renewalAgent,
        IChurnAutonomousAgent churnAgent,
        IExpansionAutonomousAgent expansionAgent,
        ICustomerAutonomousAgent customerAgent,
        IOperationsAutonomousAgent operationsAgent,
        IMlFoundationService mlFoundation,
        IIntelligenceAutomationEngine intelligence,
        IEnterpriseAiCycleService enterpriseAi,
        IAutonomousPlatformGate gate,
        ILogger<AutonomousOrchestrationEngine> logger)
    {
        _decisions = decisions;
        _revenueAgent = revenueAgent;
        _renewalAgent = renewalAgent;
        _churnAgent = churnAgent;
        _expansionAgent = expansionAgent;
        _customerAgent = customerAgent;
        _operationsAgent = operationsAgent;
        _mlFoundation = mlFoundation;
        _intelligence = intelligence;
        _enterpriseAi = enterpriseAi;
        _gate = gate;
        _logger = logger;
    }

    public async Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId == null) return;
        if (!await _gate.IsAutonomousExecutionAllowedAsync(domainEvent.TenantId.Value, cancellationToken))
            return;
        var decision = await _decisions.DecideFromEventAsync(domainEvent, cancellationToken);
        if (decision.DecisionType != AutonomousConstants.DecisionNoAction)
            await _decisions.ExecuteDecisionAsync(domainEvent.TenantId.Value, decision, cancellationToken);
    }

    public async Task RunAutonomousCycleAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!await _gate.IsAutonomousExecutionAllowedAsync(tenantId, cancellationToken))
        {
            _logger.LogInformation("Autonomous cycle skipped for tenant {TenantId} (disabled or kill-switch)", tenantId);
            return;
        }

        await _intelligence.RunPeriodicIntelligenceScanAsync(tenantId, cancellationToken);
        await _mlFoundation.CaptureTrainingSamplesAsync(tenantId, cancellationToken);
        await _enterpriseAi.RunEnterpriseAiCycleAsync(tenantId, cancellationToken);

        var results = new[]
        {
            await _revenueAgent.RunAsync(tenantId, cancellationToken),
            await _renewalAgent.RunAsync(tenantId, cancellationToken),
            await _churnAgent.RunAsync(tenantId, cancellationToken),
            await _expansionAgent.RunAsync(tenantId, cancellationToken),
            await _customerAgent.RunAsync(tenantId, cancellationToken),
            await _operationsAgent.RunAsync(tenantId, cancellationToken)
        };

        _logger.LogInformation(
            "Autonomous cycle tenant {TenantId}: {Actions} total actions",
            tenantId, results.Sum(r => r.ActionsExecuted));
    }
}
