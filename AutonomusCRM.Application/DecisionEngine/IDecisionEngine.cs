using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.DecisionEngine;

/// <summary>
/// Motor de decisiones autónomas (Autonomous Decision Engine)
/// </summary>
public interface IDecisionEngine
{
    Task<Decision> MakeDecisionAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task<List<Decision>> PrioritizeDecisionsAsync(List<Decision> decisions, CancellationToken cancellationToken = default);
    Task<string> ExplainDecisionAsync(Decision decision, CancellationToken cancellationToken = default);
}

public record Decision(
    Guid Id,
    string Type,
    string Action,
    int Priority,
    decimal Impact,
    string Reason,
    Dictionary<string, object> Context,
    DateTime CreatedAt
);

