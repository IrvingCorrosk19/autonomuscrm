using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.Policies;

/// <summary>
/// Motor de políticas (Policy Engine)
/// </summary>
public interface IPolicyEngine
{
    Task<PolicyEvaluationResult> EvaluatePolicyAsync(string policyName, IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task<bool> IsActionAllowedAsync(string action, Guid tenantId, Dictionary<string, object> context, CancellationToken cancellationToken = default);
    Task<List<Policy>> GetPoliciesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record Policy(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    string Expression,
    bool IsActive,
    DateTime CreatedAt
);

public record PolicyEvaluationResult(
    bool IsAllowed,
    string? Reason,
    List<string> Violations
);

