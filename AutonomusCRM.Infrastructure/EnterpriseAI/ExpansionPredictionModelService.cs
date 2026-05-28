using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class ExpansionPredictionModelService : IExpansionPredictionModel
{
    private readonly IMlModelVersionRepository _modelRepo;
    private readonly ICustomerRepository _customers;
    private readonly IExpansionIntelligence _expansionIntel;

    public ExpansionPredictionModelService(
        IMlModelVersionRepository modelRepo,
        ICustomerRepository customers,
        IExpansionIntelligence expansionIntel)
    {
        _modelRepo = modelRepo;
        _customers = customers;
        _expansionIntel = expansionIntel;
    }

    public async Task<IReadOnlyList<ExpansionMlPredictionDto>> PredictAsync(
        Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var active = await _modelRepo.GetActiveAsync(tenantId, EnterpriseAiConstants.ModelExpansion, cancellationToken);
        var intel = await _expansionIntel.AnalyzeAsync(tenantId, cancellationToken);
        var customers = (await _customers.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP);
        if (customerId.HasValue) customers = customers.Where(c => c.Id == customerId.Value);

        var results = new List<ExpansionMlPredictionDto>();
        foreach (var c in customers)
        {
            var exp = intel.FirstOrDefault(e => e.CustomerId == c.Id);
            var baseScore = exp?.ReadinessScore ?? 30;
            var signals = new List<string>();
            if (exp != null)
            {
                signals.Add(exp.ReadinessLevel);
                signals.Add(exp.OpportunityType);
                if (!string.IsNullOrEmpty(exp.Recommendation)) signals.Add(exp.Recommendation);
            }

            var prob = baseScore;
            var usedMl = false;
            var oppType = "upsell";

            if (active != null)
            {
                var (w, b) = MlFeatureExtractor.DictToWeights(active.Weights);
                var features = new Dictionary<string, object>
                {
                    ["health"] = 70,
                    ["expansion_score"] = baseScore,
                    ["ltv"] = c.LifetimeValue ?? 0
                };
                prob = (int)Math.Round(LogisticRegressionTrainer.PredictProbability(w, b, MlFeatureExtractor.ToVector(features)) * 100);
                prob = (int)Math.Round(prob * 0.6 + baseScore * 0.4);
                usedMl = true;
                signals.Add($"ML:{active.VersionTag}");
                oppType = prob >= 70 ? "cross-sell" : prob >= 50 ? "upsell" : "nurture";
            }

            if (prob >= 40)
                results.Add(new ExpansionMlPredictionDto(c.Id, c.Name, Math.Clamp(prob, 0, 100), oppType, usedMl, signals));
        }

        return results.OrderByDescending(r => r.ExpansionProbabilityPercent).ToList();
    }
}
