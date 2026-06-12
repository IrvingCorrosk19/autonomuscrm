using AutonomusCRM.Application.Common;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface ILeadRepository : IRepository<Lead>
{
    Task<IEnumerable<Lead>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lead>> GetByStatusAsync(Guid tenantId, LeadStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lead>> GetByAssignedUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<Lead>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        LeadStatus? status,
        LeadSource? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<LeadListSummary> GetListSummaryAsync(
        Guid tenantId,
        string? search,
        LeadStatus? status,
        LeadSource? source,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeadSourceStat>> GetSourceStatsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<LeadConversionStats> GetConversionStatsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record LeadConversionStats(int TotalCount, int QualifiedCount, double ConversionPercent);

public sealed record LeadListSummary(
    int TotalCount,
    int QualifiedCount,
    int NewCount,
    int HighScoreCount,
    double? AvgScore);

public sealed record LeadSourceStat(LeadSource Source, int Count, int Qualified);

