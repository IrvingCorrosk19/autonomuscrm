using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class RevenuePredictionModelService : IRevenuePredictionModel
{
    private static readonly int[] Horizons = { 30, 60, 90, 180, 365 };

    private readonly IMlModelVersionRepository _modelRepo;
    private readonly IRevenueForecastEngine _forecast;
    private readonly IDealRepository _deals;

    public RevenuePredictionModelService(
        IMlModelVersionRepository modelRepo,
        IRevenueForecastEngine forecast,
        IDealRepository deals)
    {
        _modelRepo = modelRepo;
        _forecast = forecast;
        _deals = deals;
    }

    public async Task<RevenueMlForecastDto> ForecastAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var active = await _modelRepo.GetActiveAsync(tenantId, EnterpriseAiConstants.ModelRevenue, cancellationToken);
        var forecasts = await _forecast.GetForecastAsync(tenantId, cancellationToken);
        var wonMonthly = await _deals.GetWonRevenueMonthlyAverageAsync(tenantId, 3, cancellationToken);
        var openPipeline = await _deals.GetOpenPipelineAmountSumAsync(tenantId, cancellationToken);

        var version = active?.VersionTag ?? "heuristic";
        var horizons = new List<RevenueMlHorizonDto>();

        foreach (var days in Horizons)
        {
            var factor = days / 30.0;
            var fc = forecasts.FirstOrDefault(f => f.HorizonDays == days)
                       ?? forecasts.OrderBy(f => Math.Abs(f.HorizonDays - days)).FirstOrDefault();
            var baseRev = fc?.WeightedForecast ?? wonMonthly * (decimal)factor;
            var confidence = 72.0;

            if (active != null)
            {
                var (w, b) = MlFeatureExtractor.DictToWeights(active.Weights);
                var features = new Dictionary<string, object>
                {
                    ["revenue_velocity"] = (double)wonMonthly,
                    ["deal_value"] = (double)openPipeline
                };
                var mlFactor = LogisticRegressionTrainer.PredictProbability(w, b, MlFeatureExtractor.ToVector(features));
                baseRev *= (decimal)(0.85 + mlFactor * 0.3);
                confidence = Math.Min(95, active.Metrics.TryGetValue("accuracy", out var acc) ? MlFeatureExtractor.ToNumeric(acc) * 100 : 78);
            }

            horizons.Add(new RevenueMlHorizonDto(days, baseRev, confidence, active != null));
        }

        return new RevenueMlForecastDto(horizons, version);
    }
}
