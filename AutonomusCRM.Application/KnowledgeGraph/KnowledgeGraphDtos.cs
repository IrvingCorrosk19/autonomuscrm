namespace AutonomusCRM.Application.KnowledgeGraph;

public record GraphNodeDto(
    string NodeType,
    Guid NodeId,
    string Label,
    decimal Weight,
    IReadOnlyDictionary<string, string>? Metadata = null);

public record GraphEdgeDto(
    string SourceType,
    Guid SourceId,
    string TargetType,
    Guid TargetId,
    string Relation,
    decimal Weight);

public record BusinessKnowledgeGraphViewDto(
    Guid TenantId,
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    int EdgeCount,
    GraphExplorationDto Exploration);

public record CustomerKnowledgeGraphDto(
    Guid CustomerId,
    string CustomerName,
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    IReadOnlyList<string> Influences,
    IReadOnlyList<string> Outcomes,
    IReadOnlyList<string> RevenueLinks,
    IReadOnlyList<string> RiskSignals,
    IReadOnlyList<string> ExpansionSignals,
    GraphExplorationDto Exploration);

public record DecisionKnowledgeGraphDto(
    Guid DecisionId,
    string DecisionType,
    string Action,
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    IReadOnlyList<string> ContextChain,
    IReadOnlyList<string> MemoryLinks,
    IReadOnlyList<string> OutcomeLinks,
    IReadOnlyList<string> RevenueLinks,
    IReadOnlyList<string> LearningLinks);

public record OutcomeKnowledgeGraphDto(
    Guid OutcomeId,
    string Category,
    bool Succeeded,
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    IReadOnlyList<string> RevenueTrail);

public record RevenueKnowledgeGraphDto(
    Guid TenantId,
    decimal TotalRevenueSignal,
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    IReadOnlyList<string> AttributionChain);

public record GraphExplorationDto(
    IReadOnlyList<GraphExplorationAnswerDto> Answers);

public record GraphExplorationAnswerDto(string Question, string Answer, IReadOnlyList<string> Evidence);

public record GraphSearchResultDto(
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    string Query);

/// <summary>Base preparada para Decision Engine, Simulation y Workforce (Phase C — no ejecuta razonamiento aún).</summary>
public record GraphReasoningContextDto(
    Guid TenantId,
    string Scenario,
    bool GraphReady,
    int NodeCount,
    int EdgeCount,
    IReadOnlyList<string> PreparedCapabilities,
    string Notes);
