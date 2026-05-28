using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class ChurnPredictionModelService : IChurnPredictionModel
{
    private readonly IMlModelVersionRepository _modelRepo;
    private readonly ICustomerRepository _customers;
    private readonly IChurnRiskEngine _churnRisk;
    private readonly ICustomerHealthEngine _health;

    public ChurnPredictionModelService(
        IMlModelVersionRepository modelRepo,
        ICustomerRepository customers,
        IChurnRiskEngine churnRisk,
        ICustomerHealthEngine health)
    {
        _modelRepo = modelRepo;
        _customers = customers;
        _churnRisk = churnRisk;
        _health = health;
    }

    public async Task<IReadOnlyList<ChurnMlPredictionDto>> PredictAsync(
        Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var activeEntity = await _modelRepo.GetActiveAsync(tenantId, EnterpriseAiConstants.ModelChurn, cancellationToken);
        var healthMap = (await _health.CalculateAllAsync(tenantId, cancellationToken)).ToDictionary(h => h.CustomerId);
        var signals = await _churnRisk.DetectSignalsAsync(tenantId, customerId, cancellationToken);

        var (weights, bias) = activeEntity != null
            ? MlFeatureExtractor.DictToWeights(activeEntity.Weights)
            : (Array.Empty<double>(), 0.0);

        var customers = (await _customers.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP or CustomerStatus.Churned);
        if (customerId.HasValue) customers = customers.Where(c => c.Id == customerId.Value);

        var results = new List<ChurnMlPredictionDto>();
        foreach (var c in customers)
        {
            healthMap.TryGetValue(c.Id, out var health);
            var customerSignals = signals.Where(s => s.CustomerId == c.Id).ToList();
            var heuristicProb = 20;
            var factors = new List<string>();
            if (health?.Classification == CustomerSuccessConstants.HealthCritical) { heuristicProb += 35; factors.Add("Health Critical"); }
            foreach (var sig in customerSignals)
            {
                heuristicProb += sig.Severity switch { "High" => 15, "Medium" => 8, _ => 3 };
                factors.Add(sig.SignalType);
            }
            heuristicProb = Math.Clamp(heuristicProb, 0, 100);

            var features = new Dictionary<string, object>
            {
                ["health"] = health?.HealthScore ?? 50,
                ["engagement"] = health?.EngagementScore ?? 50,
                ["adoption"] = health?.AdoptionScore ?? 50,
                ["ltv"] = c.LifetimeValue ?? 0,
                ["risk"] = c.RiskScore ?? 50,
                ["churn_prob"] = heuristicProb
            };

            var usedMl = activeEntity != null;
            var version = activeEntity?.VersionTag ?? "heuristic";
            int finalProb;
            if (activeEntity != null)
            {
                var vec = MlFeatureExtractor.ToVector(features);
                var mlProb = (int)Math.Round(LogisticRegressionTrainer.PredictProbability(weights, bias, vec) * 100);
                finalProb = (int)Math.Round(mlProb * 0.65 + heuristicProb * 0.35);
                factors.Add($"ML model {version}");
            }
            else
                finalProb = heuristicProb;

            results.Add(new ChurnMlPredictionDto(c.Id, c.Name, Math.Clamp(finalProb, 0, 100), usedMl, version, factors));
        }

        return results.OrderByDescending(r => r.ChurnProbabilityPercent).ToList();
    }
}
