using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class CustomerInsightsEngine : ICustomerInsightsEngine
{
    private readonly IProductUsageIntelligence _usageIntel;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IProductAnalyticsEngine _productAnalytics;

    public CustomerInsightsEngine(
        IProductUsageIntelligence usageIntel,
        ICustomerHealthEngine healthEngine,
        IProductAnalyticsEngine productAnalytics)
    {
        _usageIntel = usageIntel;
        _healthEngine = healthEngine;
        _productAnalytics = productAnalytics;
    }

    public async Task<IReadOnlyList<CustomerInsightDto>> GenerateInsightsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var insights = new List<CustomerInsightDto>();
        var usage = await _usageIntel.AnalyzeUsageAsync(tenantId, cancellationToken);
        var health = await _healthEngine.CalculateAllAsync(tenantId, cancellationToken);
        var product = await _productAnalytics.GetAnalyticsAsync(tenantId, cancellationToken);

        var mostActive = health.OrderByDescending(h => h.EngagementScore).FirstOrDefault();
        var leastActive = health.OrderBy(h => h.EngagementScore).FirstOrDefault();
        if (mostActive != null)
        {
            insights.Add(new CustomerInsightDto(
                "MostActive", "Cliente más activo", $"{mostActive.CustomerName} — engagement {mostActive.EngagementScore}",
                "Info", mostActive.CustomerId, null, true));
        }
        if (leastActive != null && leastActive.EngagementScore < 40)
        {
            insights.Add(new CustomerInsightDto(
                "LeastActive", "Cliente menos activo", $"{leastActive.CustomerName} requiere re-engagement",
                "High", leastActive.CustomerId, null, true));
        }

        foreach (var mod in usage.Where(u => u.IsAbandoned))
        {
            insights.Add(new CustomerInsightDto(
                "ModuleIgnored", "Módulo ignorado", $"El módulo {mod.Module} tiene uso mínimo ({mod.EventCount} eventos)",
                "Medium", null, mod.Module, true));
        }

        foreach (var mod in usage.Where(u => u.IsCritical))
        {
            insights.Add(new CustomerInsightDto(
                "ModuleCritical", "Módulo crítico", $"{mod.Module} con alta adopción ({mod.EventCount} eventos)",
                "Info", null, mod.Module, false));
        }

        if (product.Stickiness < 20 && product.Mau > 0)
        {
            insights.Add(new CustomerInsightDto(
                "LowStickiness", "Stickiness bajo",
                $"DAU/MAU = {product.Stickiness}% — riesgo de abandono de producto",
                "High", null, null, true));
        }

        var adoptionPattern = health.Count(h => h.AdoptionScore >= 70);
        if (health.Any())
        {
            insights.Add(new CustomerInsightDto(
                "AdoptionPattern", "Patrón de adopción",
                $"{adoptionPattern} de {health.Count} cuentas con adopción ≥ 70%",
                "Info", null, null, false));
        }

        return insights.OrderByDescending(i => i.Severity == "High").ThenBy(i => i.Title).ToList();
    }
}
