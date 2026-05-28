using AutonomusCRM.Application.DecisionEngine;
using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.Autonomous;

public interface IAiDecisionAuditRepository : Common.Interfaces.IRepository<AiDecisionAudit>
{
    Task<IEnumerable<AiDecisionAudit>> GetByTenantAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default);
}

public interface IAutonomousPlaybookStateRepository : Common.Interfaces.IRepository<AutonomousPlaybookState>
{
    Task<AutonomousPlaybookState?> GetActiveAsync(Guid tenantId, Guid customerId, string playbookType, CancellationToken cancellationToken = default);
}

public interface IBusinessKnowledgeRepository : Common.Interfaces.IRepository<BusinessKnowledgeRecord>
{
    Task<BusinessKnowledgeRecord?> GetByPatternAsync(Guid tenantId, string patternKey, CancellationToken cancellationToken = default);
}

public interface IMlFeatureSnapshotRepository : Common.Interfaces.IRepository<MlFeatureSnapshot>
{
    Task<int> CountByDatasetAsync(Guid tenantId, string datasetType, CancellationToken cancellationToken = default);
    Task<IEnumerable<MlFeatureSnapshot>> GetByDatasetAsync(Guid tenantId, string datasetType, int take = 500, CancellationToken cancellationToken = default);
}

public interface IAutonomousRevenueDecisionEngine
{
    Task<AutonomousDecisionDto> DecideForCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<AutonomousDecisionDto> DecideFromEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task ExecuteDecisionAsync(Guid tenantId, AutonomousDecisionDto decision, CancellationToken cancellationToken = default);
}

public interface INextBestActionEngine
{
    Task<IReadOnlyList<NextBestActionDto>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<NextBestActionDto?> GetForCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<NextBestActionDto?> GetForDealAsync(Guid tenantId, Guid dealId, CancellationToken cancellationToken = default);
}

public interface IAutonomousPlaybookEngine
{
    Task<PlaybookProgressDto> StartOrAdvanceAsync(Guid tenantId, Guid customerId, string playbookType, CancellationToken cancellationToken = default);
    Task<int> ProcessDuePlaybooksAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IPredictiveRevenueEngine
{
    Task<PredictiveRevenueForecastDto> ForecastAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IMlFoundationService
{
    Task<int> CaptureTrainingSamplesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MlDatasetSummaryDto>> GetDatasetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IAutonomousCustomerSuccessEngine
{
    Task<int> RunAutonomousCycleAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IAutonomousCommunicationsEngine
{
    Task<int> ExecuteForDecisionAsync(Guid tenantId, AutonomousDecisionDto decision, CancellationToken cancellationToken = default);
}

public interface IAiDecisionAuditService
{
    Task<Guid> RecordAsync(AutonomousDecisionDto decision, Guid tenantId, string? agentName = null, CancellationToken cancellationToken = default);
    Task MarkOutcomeAsync(Guid auditId, string outcome, bool success, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutonomousDecisionDto>> GetRecentAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
}

public interface IBusinessKnowledgeEngine
{
    Task RecordPatternOutcomeAsync(Guid tenantId, string patternKey, bool success, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeInsightDto>> GetInsightsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    string ResolvePreferredAction(string decisionType, Guid tenantId);
}

public interface IAutonomousOrchestrationEngine
{
    Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task RunAutonomousCycleAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IRevenueAutonomousAgent { Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default); }
public interface IRenewalAutonomousAgent { Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default); }
public interface IChurnAutonomousAgent { Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default); }
public interface IExpansionAutonomousAgent { Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default); }
public interface ICustomerAutonomousAgent { Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default); }
public interface IOperationsAutonomousAgent { Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default); }

public interface IExecutiveAiDashboardService
{
    Task<ExecutiveAiDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
