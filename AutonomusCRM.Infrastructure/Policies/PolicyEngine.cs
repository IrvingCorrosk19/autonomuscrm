using AutonomusCRM.Application.Policies;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Policies;

/// <summary>
/// Implementación básica del Policy Engine
/// </summary>
public class PolicyEngine : IPolicyEngine
{
    private readonly IPolicyRepository _policyRepository;
    private readonly ILogger<PolicyEngine> _logger;

    public PolicyEngine(
        IPolicyRepository policyRepository,
        ILogger<PolicyEngine> logger)
    {
        _policyRepository = policyRepository;
        _logger = logger;
    }

    public async Task<PolicyEvaluationResult> EvaluatePolicyAsync(
        string policyName,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId == null)
            return new PolicyEvaluationResult(false, "TenantId is required", new List<string> { "Missing TenantId" });

        var policies = await _policyRepository.GetActiveByTenantAndNameAsync(
            domainEvent.TenantId.Value,
            policyName,
            cancellationToken);

        var violations = new List<string>();

        foreach (var policy in policies)
        {
            var result = EvaluatePolicyExpression(policy, domainEvent);
            if (!result.IsAllowed)
            {
                violations.Add($"{policy.Name}: {result.Reason}");
            }
        }

        var isAllowed = violations.Count == 0;
        var reason = isAllowed ? "All policies passed" : string.Join("; ", violations);

        return new PolicyEvaluationResult(isAllowed, reason, violations);
    }

    public async Task<bool> IsActionAllowedAsync(
        string action,
        Guid tenantId,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        var policies = await _policyRepository.GetActiveByTenantAsync(tenantId, cancellationToken);
        
        // Filtrar políticas relevantes para la acción
        var relevantPolicies = policies.Where(p => p.Expression.Contains(action));

        foreach (var policy in relevantPolicies)
        {
            // TODO: Evaluar expresión de política contra contexto
            // Por ahora, todas las políticas se consideran cumplidas
        }

        return true;
    }

    public async Task<List<Policy>> GetPoliciesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return (await _policyRepository.GetActiveByTenantAsync(tenantId, cancellationToken)).ToList();
    }

    private PolicyEvaluationResult EvaluatePolicyExpression(Policy policy, IDomainEvent domainEvent)
    {
        // TODO: Implementar evaluación de expresiones de política
        // Por ahora, todas las políticas se consideran cumplidas
        return new PolicyEvaluationResult(true, "Policy passed", new List<string>());
    }
}

