using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class PredictiveRevenueEngine : IPredictiveRevenueEngine
{
    private static readonly int[] Horizons = { 30, 60, 90, 180, 365 };

    private readonly IRevenueForecastEngine _forecast;
    private readonly ICustomerContractRepository _contracts;
    private readonly IChurnPredictionV2 _churn;
    private readonly IExpansionIntelligence _expansion;
    private readonly IDealRepository _dealRepository;

    public PredictiveRevenueEngine(
        IRevenueForecastEngine forecast,
        ICustomerContractRepository contracts,
        IChurnPredictionV2 churn,
        IExpansionIntelligence expansion,
        IDealRepository dealRepository)
    {
        _forecast = forecast;
        _contracts = contracts;
        _churn = churn;
        _expansion = expansion;
        _dealRepository = dealRepository;
    }

    public async Task<PredictiveRevenueForecastDto> ForecastAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var forecasts = await _forecast.GetForecastAsync(tenantId, cancellationToken);
        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var wonMonthly = deals.Where(d => d.Stage == DealStage.ClosedWon && d.ClosedAt >= DateTime.UtcNow.AddMonths(-3))
            .Sum(d => d.Amount) / 3m;
        var churnList = await _churn.PredictAsync(tenantId, cancellationToken: cancellationToken);
        var expansionList = await _expansion.AnalyzeAsync(tenantId, cancellationToken);
        var contracts = (await _contracts.GetByTenantAsync(tenantId, cancellationToken)).ToList();

        var horizons = new List<PredictiveHorizonDto>();
        foreach (var days in Horizons)
        {
            var factor = days / 30.0;
            var fc = forecasts.FirstOrDefault(f => f.HorizonDays == days)
                       ?? forecasts.OrderBy(f => Math.Abs(f.HorizonDays - days)).FirstOrDefault();
            var revenue = fc?.WeightedForecast ?? wonMonthly * (decimal)factor;
            var renewals = contracts
                .Where(c => c.RenewalDate <= DateTime.UtcNow.AddDays(days))
                .Sum(c => c.AnnualValue);
            var churnCount = churnList.Count(c => c.ChurnProbability >= 50 * (1 + days / 180.0));
            var expansion = expansionList.Where(e => e.ReadinessScore >= 60).Sum(e => e.ReadinessScore) * 100m;

            horizons.Add(new PredictiveHorizonDto(days, revenue, renewals, churnCount, expansion));
        }

        var confidence = forecasts.Any() ? Math.Min(95, 50 + churnList.Count * 2) : 45;
        return new PredictiveRevenueForecastDto(horizons, confidence);
    }
}
