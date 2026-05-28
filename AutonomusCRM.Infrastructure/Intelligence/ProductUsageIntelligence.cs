using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class ProductUsageIntelligence : IProductUsageIntelligence
{
    private readonly IProductUsageEventRepository _usageRepository;

    public ProductUsageIntelligence(IProductUsageEventRepository usageRepository)
    {
        _usageRepository = usageRepository;
    }

    public async Task<IReadOnlyList<ProductUsageInsightDto>> AnalyzeUsageAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var events = (await _usageRepository.GetByTenantAsync(tenantId, DateTime.UtcNow.AddDays(-30), cancellationToken: cancellationToken)).ToList();
        if (!events.Any())
        {
            return IntelligenceConstants.CrmModules.Select(m => new ProductUsageInsightDto(
                m, 0, 0, 0, m is "Deals" or "Customers", true)).ToList();
        }

        var total = events.Count;
        var avgPerModule = total / (double)Math.Max(1, events.Select(e => e.Module).Distinct().Count());

        return events.GroupBy(e => e.Module).Select(g =>
        {
            var count = g.Count();
            var isCritical = count >= avgPerModule * 1.5 || g.Key is "Deals" or "Customers";
            var isAbandoned = count < avgPerModule * 0.25;
            return new ProductUsageInsightDto(
                g.Key, count,
                g.Select(e => e.UserId).Distinct().Count(),
                g.Select(e => e.CustomerId).Distinct().Count(),
                isCritical, isAbandoned);
        }).OrderByDescending(x => x.EventCount).ToList();
    }
}
