namespace AutonomusCRM.Application.Autonomous;

public record AiCommandCenterDto(
    int PendingApprovals,
    int DecisionsLast24h,
    int BusinessOutcomesLast7d,
    int ApprovalThreshold,
    decimal RevenueGeneratedByAi7d,
    int IncompleteOutcomeFabric,
    IReadOnlyList<CommandCenterCustomerRow> AtRiskCustomers,
    IReadOnlyList<CommandCenterCustomerRow> ExpansionTargets,
    IReadOnlyList<CommandCenterCustomerRow> RenewalTargets,
    IReadOnlyList<CommandCenterAgentRow> ActiveAgents,
    IReadOnlyList<CommandCenterDecisionRow> RecentDecisions,
    IReadOnlyList<CommandCenterNbaRow> TopActions);

public record CommandCenterCustomerRow(Guid CustomerId, string Name, string Reason, int PriorityScore);

public record CommandCenterAgentRow(string AgentName, int Actions24h, int Outcomes7d, decimal RevenueImpact7d);

public record CommandCenterDecisionRow(
    Guid AuditId,
    string DecisionType,
    string Action,
    int Score,
    string Status,
    DateTime CreatedAt,
    bool? BusinessSucceeded);

public record CommandCenterNbaRow(string EntityName, string Action, int Priority, string Rationale);

public interface IAiCommandCenterService
{
    Task<AiCommandCenterDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
