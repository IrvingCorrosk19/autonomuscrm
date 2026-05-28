using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.DecisionEngine;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DecisionEngine;

/// <summary>
/// Autonomous Decision Engine — delega a motor de revenue Fase 15.
/// </summary>
public class DecisionEngine : IDecisionEngine
{
    private readonly IAutonomousRevenueDecisionEngine _autonomous;
    private readonly ILogger<DecisionEngine> _logger;

    public DecisionEngine(IAutonomousRevenueDecisionEngine autonomous, ILogger<DecisionEngine> logger)
    {
        _autonomous = autonomous;
        _logger = logger;
    }

    public async Task<Decision> MakeDecisionAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var auto = await _autonomous.DecideFromEventAsync(domainEvent, cancellationToken);
        _logger.LogInformation("DecisionEngine: {Type} -> {Action} (score {Score})", auto.DecisionType, auto.Action, auto.Score);

        return new Decision(
            auto.DecisionId,
            domainEvent.EventType,
            auto.Action,
            auto.Score,
            auto.Score / 100m,
            auto.Reason,
            auto.Evidence,
            DateTime.UtcNow);
    }

    public Task<List<Decision>> PrioritizeDecisionsAsync(List<Decision> decisions, CancellationToken cancellationToken = default)
        => Task.FromResult(decisions.OrderByDescending(d => d.Priority).ThenByDescending(d => d.Impact).ToList());

    public Task<string> ExplainDecisionAsync(Decision decision, CancellationToken cancellationToken = default)
        => Task.FromResult($"DECISIÓN: {decision.Action}\nPRIORIDAD: {decision.Priority}\nRAZÓN: {decision.Reason}");
}
