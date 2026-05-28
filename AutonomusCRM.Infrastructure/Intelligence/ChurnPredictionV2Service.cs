using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class ChurnPredictionV2Service : IChurnPredictionV2
{
    private readonly IChurnPredictionModel? _churnMl;
    private readonly IChurnRiskEngine _churnRiskEngine;
    private readonly ICustomerAnalyticsSnapshotRepository _snapshotRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerHealthEngine _healthEngine;

    public ChurnPredictionV2Service(
        IChurnRiskEngine churnRiskEngine,
        ICustomerAnalyticsSnapshotRepository snapshotRepository,
        ICustomerRepository customerRepository,
        ICustomerHealthEngine healthEngine,
        IChurnPredictionModel churnMl)
    {
        _churnMl = churnMl;
        _churnRiskEngine = churnRiskEngine;
        _snapshotRepository = snapshotRepository;
        _customerRepository = customerRepository;
        _healthEngine = healthEngine;
    }

    public async Task<IReadOnlyList<ChurnPredictionV2Dto>> PredictAsync(
        Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        if (_churnMl != null)
        {
            var ml = await _churnMl.PredictAsync(tenantId, customerId, cancellationToken);
            if (ml.Any(m => m.UsedMlModel))
            {
                return ml.Select(m => new ChurnPredictionV2Dto(
                    m.CustomerId, m.CustomerName, m.ChurnProbabilityPercent,
                    m.ChurnProbabilityPercent >= 70 ? "Declining" : m.ChurnProbabilityPercent >= 40 ? "Stable" : "Improving",
                    m.Factors.ToList(), null, null)).ToList();
            }
        }

        var signals = await _churnRiskEngine.DetectSignalsAsync(tenantId, customerId, cancellationToken);
        var healthList = await _healthEngine.CalculateAllAsync(tenantId, cancellationToken);
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);

        var targetIds = customerId.HasValue
            ? new[] { customerId.Value }
            : healthList.Select(h => h.CustomerId).Distinct();

        var results = new List<ChurnPredictionV2Dto>();

        foreach (var cid in targetIds)
        {
            customers.TryGetValue(cid, out var name);
            var health = healthList.FirstOrDefault(h => h.CustomerId == cid);
            var history = (await _snapshotRepository.GetByCustomerAsync(tenantId, cid, 30, cancellationToken))
                .OrderBy(s => s.SnapshotDate).ToList();

            double? healthTrend = null;
            double? engagementTrend = null;
            if (history.Count >= 2)
            {
                healthTrend = history.Last().HealthScore - history.First().HealthScore;
                engagementTrend = history.Last().EngagementScore - history.First().EngagementScore;
            }

            var customerSignals = signals.Where(s => s.CustomerId == cid).ToList();
            var prob = 20;
            var factors = new List<string>();

            if (health?.Classification == CustomerSuccessConstants.HealthCritical) { prob += 35; factors.Add("Health Critical"); }
            else if (health?.Classification == CustomerSuccessConstants.HealthWarning) { prob += 15; factors.Add("Health Warning"); }

            foreach (var sig in customerSignals)
            {
                prob += sig.Severity switch { "High" => 15, "Medium" => 8, _ => 3 };
                factors.Add(sig.SignalType);
            }

            if (healthTrend < -10) { prob += 12; factors.Add("Health declining"); }
            if (engagementTrend < -10) { prob += 10; factors.Add("Engagement declining"); }
            if (history.Count >= 3 && history.TakeLast(3).All(s => s.ChurnRiskScore > 60)) { prob += 10; factors.Add("Sustained churn risk"); }

            prob = Math.Clamp(prob, 0, 100);
            var trend = healthTrend switch
            {
                < -5 => "Declining",
                > 5 => "Improving",
                _ => "Stable"
            };

            results.Add(new ChurnPredictionV2Dto(
                cid, name ?? "Cliente", prob, trend, factors.Distinct().ToList(),
                healthTrend, engagementTrend));
        }

        return results.OrderByDescending(r => r.ChurnProbability).ToList();
    }
}
