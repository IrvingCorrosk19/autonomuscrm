namespace AutonomusCRM.Application.CustomerSuccess;

public record CsTicketDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string Subject,
    string? Description,
    string Status,
    string Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    bool IsOverdue);

public record CsCaseDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CaseType,
    string CaseTypeLabel,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTime CreatedAt);

public record CustomerSuccessHomeDto(
    CustomerKpiSnapshotDto Kpis,
    IReadOnlyList<ChurnRiskSignalDto> AtRiskCustomers,
    IReadOnlyList<RenewalAlertDto> Renewals,
    IReadOnlyList<CsTicketDto> OpenTickets,
    IReadOnlyList<CsTicketDto> ClosedTickets,
    IReadOnlyList<ExpansionOpportunityDto> Expansions,
    IReadOnlyList<CsCaseDto> OpenCases,
    IReadOnlyList<CustomerHealthDto> HealthSummary,
    int OpenTicketCount,
    int OpenCaseCount);

public record Customer360CsPanelDto(
    IReadOnlyList<CsTicketDto> OpenTickets,
    IReadOnlyList<CsTicketDto> ClosedTickets,
    IReadOnlyList<CsCaseDto> RecentCases);

public interface ICustomerSuccessOsService
{
    Task<CustomerSuccessHomeDto> GetHomeAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Customer360CsPanelDto> GetCustomerPanelAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<CsTicketDto> CreateTicketAsync(Guid tenantId, Guid customerId, string subject, string? description, string priority, Guid? assignedToUserId, CancellationToken cancellationToken = default);
    Task<CsCaseDto> CreateCaseAsync(Guid tenantId, Guid customerId, string caseType, string title, string? description, string priority, Guid? assignedToUserId, CancellationToken cancellationToken = default);
    Task<bool> CloseTicketAsync(Guid tenantId, Guid ticketId, CancellationToken cancellationToken = default);
    Task<PlaybookExecutionDto> RunPlaybookAsync(Guid tenantId, Guid customerId, string playbookType, Guid? assignedToUserId, CancellationToken cancellationToken = default);
}
