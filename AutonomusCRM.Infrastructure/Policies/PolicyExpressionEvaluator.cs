using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Infrastructure.Policies;

/// <summary>
/// Evaluates policy expressions (semicolon/newline separated rules).
/// Supported: allow, deny, requireApproval, tenantKillSwitch, humanInTheLoopRequired,
/// maxRiskScore:N, minConfidence:N, action:Name
/// </summary>
public static class PolicyExpressionEvaluator
{
    public sealed record EvaluationOutcome(
        bool IsAllowed,
        bool RequiresApproval,
        string Reason,
        IReadOnlyList<string> Violations);

    public static EvaluationOutcome EvaluateExpression(string expression, IReadOnlyDictionary<string, object> context)
    {
        var rules = ParseRules(expression);
        var violations = new List<string>();
        var requiresApproval = false;

        foreach (var rule in rules)
        {
            switch (rule.Key)
            {
                case "deny":
                    return new EvaluationOutcome(false, false, "Policy denied", new[] { "deny rule" });
                case "allow":
                    continue;
                case "requireapproval":
                    requiresApproval = true;
                    continue;
                case "humaninthelooprequired":
                    if (!GetBool(context, "humanInTheLoop") && !GetBool(context, "humanInTheLoopRequired"))
                        violations.Add("humanInTheLoopRequired");
                    continue;
                case "tenantkillswitch":
                    if (GetBool(context, "tenantKillSwitch"))
                        return new EvaluationOutcome(false, false, "Tenant kill switch active", new[] { "tenantKillSwitch" });
                    continue;
                case "maxriskscore":
                    if (TryGetDouble(context, "riskScore", out var risk) && TryParseDouble(rule.Value, out var maxRisk) && risk > maxRisk)
                        violations.Add($"riskScore {risk} > maxRiskScore {maxRisk}");
                    continue;
                case "minconfidence":
                    if (TryGetDouble(context, "confidence", out var conf) && TryParseDouble(rule.Value, out var minConf) && conf < minConf)
                        violations.Add($"confidence {conf} < minConfidence {minConf}");
                    continue;
                case "action":
                    if (!string.IsNullOrWhiteSpace(rule.Value) &&
                        context.TryGetValue("action", out var act) &&
                        !string.Equals(act?.ToString(), rule.Value, StringComparison.OrdinalIgnoreCase))
                        violations.Add($"action mismatch: expected {rule.Value}, got {act}");
                    continue;
            }
        }

        if (violations.Count > 0)
            return new EvaluationOutcome(false, requiresApproval, string.Join("; ", violations), violations);

        if (requiresApproval)
            return new EvaluationOutcome(false, true, "Requires human approval", Array.Empty<string>());

        return new EvaluationOutcome(true, false, "Policy passed", Array.Empty<string>());
    }

    public static EvaluationOutcome EvaluateForDomainEvent(string expression, IDomainEvent domainEvent)
    {
        var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["eventType"] = domainEvent.EventType,
            ["tenantId"] = domainEvent.TenantId?.ToString() ?? ""
        };

        foreach (var prop in domainEvent.GetType().GetProperties())
        {
            if (!prop.CanRead || prop.Name is "Id" or "OccurredOn" or "EventType" or "TenantId" or "CorrelationId")
                continue;
            var value = prop.GetValue(domainEvent);
            if (value != null)
                context[prop.Name] = value;
        }

        return EvaluateExpression(expression, context);
    }

    private static List<(string Key, string? Value)> ParseRules(string expression)
    {
        var rules = new List<(string, string?)>();
        foreach (var raw in expression.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var part = raw.Trim();
            if (string.IsNullOrWhiteSpace(part)) continue;
            var colon = part.IndexOf(':');
            if (colon > 0)
            {
                rules.Add((part[..colon].Trim().ToLowerInvariant(), part[(colon + 1)..].Trim()));
            }
            else
            {
                rules.Add((part.ToLowerInvariant(), null));
            }
        }
        return rules;
    }

    private static bool GetBool(IReadOnlyDictionary<string, object> ctx, string key) =>
        ctx.TryGetValue(key, out var v) && v switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var parsed) && parsed,
            _ => false
        };

    private static bool TryGetDouble(IReadOnlyDictionary<string, object> ctx, string key, out double value)
    {
        value = 0;
        if (!ctx.TryGetValue(key, out var v) || v == null) return false;
        return double.TryParse(v.ToString(), out value);
    }

    private static bool TryParseDouble(string? s, out double value) =>
        double.TryParse(s, out value);
}
