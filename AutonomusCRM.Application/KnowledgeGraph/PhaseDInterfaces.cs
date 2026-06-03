namespace AutonomusCRM.Application.KnowledgeGraph;

public interface IOperationalGraphFeed
{
    Task RecordTrustApprovalQueuedAsync(Guid tenantId, Guid approvalId, Guid auditId, string decisionType, CancellationToken cancellationToken = default);
    Task RecordTrustApprovedAsync(Guid tenantId, Guid approvalId, Guid auditId, CancellationToken cancellationToken = default);
    Task RecordTrustRejectedAsync(Guid tenantId, Guid approvalId, Guid auditId, CancellationToken cancellationToken = default);
    Task RecordTrustRollbackAsync(Guid tenantId, Guid approvalId, Guid auditId, CancellationToken cancellationToken = default);
    Task RecordCommunicationAsync(Guid tenantId, Guid communicationLogId, string channel, Guid? customerId, string? agentName, CancellationToken cancellationToken = default);
    Task RecordVoiceCallAsync(Guid tenantId, Guid callId, Guid? customerId, string outcome, string? summary, CancellationToken cancellationToken = default);
}

public record GraphReasoningResultDto(
    string Summary,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> CausalChain,
    double Confidence,
    string ProviderBadge);

public record DecisionIntelligenceResultDto(
    string WhatHappened,
    string WhyItHappened,
    IReadOnlyList<string> Evidence,
    string RecommendedAction,
    string RiskAssessment,
    string EconomicImpactEstimate,
    bool RequiresHumanApproval,
    string ReasoningSummary,
    IReadOnlyList<string> SimilarMemories);

public record SimulationScenarioResultDto(
    string ScenarioKey,
    string Title,
    string Narrative,
    IReadOnlyList<string> ProjectedEffects,
    decimal EstimatedRevenueImpact,
    bool BasedOnHistoricalData);

public interface IGraphReasoningEngine
{
    Task<GraphReasoningResultDto> ExplainCustomerRiskAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<GraphReasoningResultDto> ExplainCustomerRenewalAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<GraphReasoningResultDto> ExplainRevenueOutcomeAsync(Guid tenantId, Guid? customerId, CancellationToken cancellationToken = default);
    Task<GraphReasoningResultDto> RecommendNextActionAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<GraphReasoningResultDto> ExplainDecisionAsync(Guid tenantId, Guid auditId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> FindCausalChainAsync(Guid tenantId, Guid fromNodeId, string fromNodeType, CancellationToken cancellationToken = default);
    Task<GraphReasoningResultDto> DetectRevenueLeakAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<GraphReasoningResultDto> DetectExpansionPathAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}

public interface IDecisionIntelligenceEngine
{
    Task<DecisionIntelligenceResultDto> AnalyzeCustomerDecisionAsync(
        Guid tenantId, Guid customerId, string? agentContext, CancellationToken cancellationToken = default);
    Task<DecisionIntelligenceResultDto> AnalyzeAuditDecisionAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken = default);
    Task<DecisionIntelligenceResultDto> ExplainTrustApprovalAsync(
        Guid tenantId, Guid approvalId, CancellationToken cancellationToken = default);
}

public interface IBusinessSimulationEngine
{
    Task<SimulationScenarioResultDto> RunScenarioAsync(
        Guid tenantId, string scenarioKey, Guid? customerId = null, CancellationToken cancellationToken = default);
    IReadOnlyList<string> GetAvailableScenarios();
}
