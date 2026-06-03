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

public record FlowSparklinePoint(string DayLabel, decimal Value);

public record PipelineStageSnapshot(string StageName, int DealCount, decimal Amount);

public record WorkforceAgentDto(
    string Key,
    string DisplayName,
    int Actions24h,
    int Actions7d,
    int Outcomes7d,
    decimal RevenueImpact7d,
    string Status);

public record PlaybookStateRow(
    Guid Id,
    Guid CustomerId,
    string? CustomerName,
    string PlaybookType,
    string Status,
    int CurrentStep,
    int TotalSteps,
    DateTime? NextActionAt);

public record FlowCommandViewDto(
    AiCommandCenterDto Dashboard,
    decimal RevenueGeneratedPeriod,
    decimal RevenueProtectedPeriod,
    decimal TotalRevenueImpactPeriod,
    bool HasData,
    int PeriodDays,
    IReadOnlyList<FlowSparklinePoint> RevenueSparkline,
    IReadOnlyList<FlowSparklinePoint> RenewalSparkline,
    IReadOnlyList<FlowSparklinePoint> ExpansionSparkline,
    IReadOnlyList<FlowSparklinePoint> ChurnSparkline,
    IReadOnlyList<WorkforceAgentDto> Workforce,
    IReadOnlyList<PipelineStageSnapshot> PipelineSnapshot,
    IReadOnlyList<OutcomeFabricStatusDto> IncompleteOutcomes);

public record FlowOutcomesSummaryDto(
    decimal RevenueGenerated,
    decimal RevenueProtected,
    int CompleteChains,
    int IncompleteChains,
    IReadOnlyList<OutcomeFabricStatusDto> RecentChains);

public record DecisionHistoryRow(
    Guid AuditId,
    string? AgentName,
    string DecisionType,
    string Action,
    int Score,
    string Status,
    DateTime CreatedAt,
    bool? BusinessSucceeded,
    decimal? RevenueImpact,
    string? CustomerName);

public interface IAiCommandCenterService
{
    Task<AiCommandCenterDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<FlowCommandViewDto> GetFlowCommandAsync(Guid tenantId, int periodDays = 7, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DecisionHistoryRow>> GetDecisionHistoryAsync(
        Guid tenantId,
        string? status = null,
        string? agent = null,
        int? minScore = null,
        int take = 100,
        CancellationToken cancellationToken = default);
    Task<FlowOutcomesSummaryDto> GetOutcomesSummaryAsync(Guid tenantId, int periodDays = 30, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaybookStateRow>> GetPlaybooksAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
}
