using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Policies;

public class PolicyEngine : IPolicyEngine
{
    private readonly IPolicyRepository _policyRepository;
    private readonly ILogger<PolicyEngine> _logger;

    public PolicyEngine(IPolicyRepository policyRepository, ILogger<PolicyEngine> logger)
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
            domainEvent.TenantId.Value, policyName, cancellationToken);

        var violations = new List<string>();
        foreach (var policy in policies)
        {
            var outcome = PolicyExpressionEvaluator.EvaluateForDomainEvent(policy.Expression, domainEvent);
            if (!outcome.IsAllowed)
                violations.Add($"{policy.Name}: {outcome.Reason}");
            if (outcome.RequiresApproval)
                violations.Add($"{policy.Name}: requires approval");
        }

        var isAllowed = violations.Count == 0;
        return new PolicyEvaluationResult(isAllowed, isAllowed ? "All policies passed" : string.Join("; ", violations), violations);
    }

    public async Task<bool> IsActionAllowedAsync(
        string action,
        Guid tenantId,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        context["action"] = action;
        var policies = await _policyRepository.GetActiveByTenantAsync(tenantId, cancellationToken);

        foreach (var policy in policies)
        {
            var outcome = PolicyExpressionEvaluator.EvaluateExpression(policy.Expression, context);
            if (!outcome.IsAllowed)
            {
                _logger.LogWarning("Policy {Policy} blocked action {Action}: {Reason}", policy.Name, action, outcome.Reason);
                return false;
            }
        }

        return true;
    }

    public async Task<List<Policy>> GetPoliciesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        (await _policyRepository.GetActiveByTenantAsync(tenantId, cancellationToken)).ToList();
}
