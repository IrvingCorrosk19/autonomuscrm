using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Revenue;

public class WinLossAnalyticsService : IWinLossAnalyticsService
{
    private readonly IDealRepository _dealRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;

    public WinLossAnalyticsService(
        IDealRepository dealRepository,
        IUserRepository userRepository,
        ICustomerRepository customerRepository)
    {
        _dealRepository = dealRepository;
        _userRepository = userRepository;
        _customerRepository = customerRepository;
    }

    public async Task<IReadOnlyList<WinLossBreakdownDto>> GetAnalysisAsync(
        Guid tenantId, string groupBy = "reason", CancellationToken cancellationToken = default)
    {
        var lost = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(d => d.Stage == DealStage.ClosedLost).ToList();
        var total = lost.Count;
        if (total == 0)
            return Array.Empty<WinLossBreakdownDto>();

        var users = (await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToDictionary(u => u.Id, u => u.Email);
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToDictionary(c => c.Id);

        IEnumerable<IGrouping<string, Deal>> groups = groupBy.ToLowerInvariant() switch
        {
            "rep" => lost.GroupBy(d => d.AssignedToUserId.HasValue && users.ContainsKey(d.AssignedToUserId.Value)
                ? users[d.AssignedToUserId.Value] : "Sin asignar"),
            "stage" => lost.GroupBy(d => RevenueAnalyticsCore.GetMetadataString(d.Metadata, "StageAtLoss") ?? d.Stage.ToString()),
            "industry" => lost.GroupBy(d =>
            {
                if (customers.TryGetValue(d.CustomerId, out var c) && !string.IsNullOrWhiteSpace(c.Company))
                    return c.Company!;
                return "Sin industria";
            }),
            "amount" => lost.GroupBy(d => d.Amount switch
            {
                < 5000 => "<5K",
                < 25000 => "5K-25K",
                < 100000 => "25K-100K",
                _ => "100K+"
            }),
            _ => lost.GroupBy(d =>
                RevenueAnalyticsCore.GetMetadataString(d.Metadata, "LossCategory")
                ?? RevenueAnalyticsCore.GetMetadataString(d.Metadata, "LossReason")
                ?? "Sin motivo")
        };

        return groups.Select(g => new WinLossBreakdownDto(
            groupBy,
            g.Key,
            g.Count(),
            g.Sum(d => d.Amount),
            Math.Round(g.Count() * 100.0 / total, 1))).OrderByDescending(x => x.Count).ToList();
    }
}
