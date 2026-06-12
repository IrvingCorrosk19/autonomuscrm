using AutonomusCRM.Application.Common;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByStatusAsync(Guid tenantId, CustomerStatus status, CancellationToken cancellationToken = default);
    Task<PagedResult<Customer>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        CustomerStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<CustomerListSummary> GetListSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<CustomerJourneyCounts> GetJourneyCustomerCountsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerHealthProjection>> GetHealthEligibleProjectionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpansionCustomerProjection>> GetExpansionCustomerProjectionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record CustomerJourneyCounts(
    int ActiveCustomerCount,
    int OnboardingCount,
    int RenewalCount,
    int ExpansionMetadataCount);

public sealed record CustomerHealthProjection(
    Guid Id,
    string Name,
    DateTime? LastContactAt,
    decimal? LifetimeValue,
    int? RiskScore);

public sealed record ExpansionCustomerProjection(
    Guid Id,
    string Name,
    CustomerStatus Status,
    bool ProductLineHasComma);

public sealed record CustomerListSummary(
    int TotalCount,
    decimal AvgLtv,
    int HighLtvCount,
    int HighRiskCount,
    double? AvgRisk,
    int LowRiskCount);

