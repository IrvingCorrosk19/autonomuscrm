namespace AutonomusCRM.Application.DataPlatform;

public record CustomerTimelineEventDto(
    DateTime OccurredAt,
    string Category,
    string Title,
    string Detail,
    string Severity);

public record CustomerHealthCenterDto(
    int HealthScore,
    double? ChurnRisk,
    int ExpansionReadiness,
    int UsageEvents30d,
    string RiskLevel);

public record CustomerJourneyStageDto(string Stage, string Status, DateTime? OccurredAt);

public record RelationshipNodeDto(string Id, string Label, string Type, string? Meta);

public record RelationshipEdgeDto(string FromId, string ToId, string Label);

public record Customer360EnterpriseDto(
    Customer360Dto Profile,
    IReadOnlyList<CustomerTimelineEventDto> Timeline,
    CustomerHealthCenterDto Health,
    IReadOnlyList<CustomerJourneyStageDto> Journey,
    IReadOnlyList<string> ExecutiveSummaryBullets,
    IReadOnlyList<CustomerTimelineEventDto> Communications,
    IReadOnlyList<RelationshipNodeDto> RelationshipNodes,
    IReadOnlyList<RelationshipEdgeDto> RelationshipEdges);

public interface ICustomer360EnterpriseService
{
    Task<Customer360EnterpriseDto?> GetEnterpriseViewAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}
