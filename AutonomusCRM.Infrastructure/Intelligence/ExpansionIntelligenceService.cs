using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class ExpansionIntelligenceService : IExpansionIntelligence
{
    private readonly IExpansionRevenueEngine _expansionEngine;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IProductUsageIntelligence _usageIntel;

    public ExpansionIntelligenceService(
        IExpansionRevenueEngine expansionEngine,
        ICustomerHealthEngine healthEngine,
        IProductUsageIntelligence usageIntel)
    {
        _expansionEngine = expansionEngine;
        _healthEngine = healthEngine;
        _usageIntel = usageIntel;
    }

    public async Task<IReadOnlyList<ExpansionIntelligenceDto>> AnalyzeAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var opps = await _expansionEngine.DetectOpportunitiesAsync(tenantId, cancellationToken);
        var health = (await _healthEngine.CalculateAllAsync(tenantId, cancellationToken))
            .ToDictionary(h => h.CustomerId);
        var usage = await _usageIntel.AnalyzeUsageAsync(tenantId, cancellationToken);
        var moduleScore = usage.Any() ? usage.Average(u => u.EventCount) : 0;

        return opps.Select(o =>
        {
            health.TryGetValue(o.CustomerId, out var h);
            var readiness = 40;
            if (h != null)
            {
                readiness += h.HealthScore / 5;
                readiness += h.AdoptionScore / 10;
                readiness += h.EngagementScore / 10;
            }
            readiness = Math.Clamp(readiness, 0, 100);
            var level = readiness switch
            {
                >= 80 => "Ready",
                >= 60 => "Warm",
                _ => "Nurture"
            };

            return new ExpansionIntelligenceDto(
                o.CustomerId, o.CustomerName, level, o.OpportunityType,
                readiness, o.Recommendation);
        }).OrderByDescending(x => x.ReadinessScore).ToList();
    }
}
