using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AutonomusCRM.Infrastructure.Revenue;

public sealed class RevenueOsService : IRevenueOsService
{
    private const string RevenueKey = "outcomeFabric.revenueImpact";
    private static readonly TimeSpan DashboardCacheTtl = TimeSpan.FromMinutes(3);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IAiCommandCenterService _commandCenter;
    private readonly IRevenueKpiService _kpis;
    private readonly IPredictiveRevenueEngine _predictive;
    private readonly IWinLossAnalyticsService _winLoss;
    private readonly INextBestActionEngine _nba;
    private readonly IChurnPredictionV2 _churn;
    private readonly IExpansionIntelligence _expansion;
    private readonly IOutcomeFabricService _outcomeFabric;

    public RevenueOsService(
        ApplicationDbContext db,
        IMemoryCache cache,
        IAiCommandCenterService commandCenter,
        IRevenueKpiService kpis,
        IPredictiveRevenueEngine predictive,
        IWinLossAnalyticsService winLoss,
        INextBestActionEngine nba,
        IChurnPredictionV2 churn,
        IExpansionIntelligence expansion,
        IOutcomeFabricService outcomeFabric)
    {
        _db = db;
        _cache = cache;
        _commandCenter = commandCenter;
        _kpis = kpis;
        _predictive = predictive;
        _winLoss = winLoss;
        _nba = nba;
        _churn = churn;
        _expansion = expansion;
        _outcomeFabric = outcomeFabric;
    }

    public async Task<RevenueOsDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"revenue-os:dashboard:{tenantId}";
        if (_cache.TryGetValue(cacheKey, out RevenueOsDashboardDto? cached) && cached is not null)
            return cached;

        var dashboard = await BuildDashboardAsync(tenantId, cancellationToken);
        _cache.Set(cacheKey, dashboard, DashboardCacheTtl);
        return dashboard;
    }

    private async Task<RevenueOsDashboardDto> BuildDashboardAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var since90 = DateTime.UtcNow.AddDays(-90);
        var outcomes = await _commandCenter.GetOutcomesSummaryAsync(tenantId, 90, cancellationToken);
        var kpis = await _kpis.GetSnapshotAsync(tenantId, cancellationToken);
        var forecast = await _predictive.ForecastAsync(tenantId, cancellationToken);
        var lossAnalysis = await _winLoss.GetAnalysisAsync(tenantId, "reason", cancellationToken);
        var nba = await _nba.GetForTenantAsync(tenantId, cancellationToken);
        var churnList = await _churn.PredictAsync(tenantId, cancellationToken: cancellationToken);
        var expansionList = await _expansion.AnalyzeAsync(tenantId, cancellationToken);

        var openDeals = await _db.Deals.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open)
            .ToListAsync(cancellationToken);
        var openPipeline = openDeals.Sum(d => d.Amount * (d.Probability ?? 0) / 100m);

        var atRiskPipeline = await _db.Deals.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open && (d.Probability ?? 0) < 50)
            .SumAsync(d => d.Amount, cancellationToken);

        var renewalValue = await _db.CustomerContracts.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.RenewalDate <= DateTime.UtcNow.AddDays(90))
            .SumAsync(c => c.AnnualValue, cancellationToken);

        var expansionPotential = expansionList.Where(e => e.ReadinessScore >= 60).Sum(e => e.ReadinessScore) * 50m;

        var overview = new RevenueExecutiveOverviewDto(
            outcomes.RevenueGenerated,
            outcomes.RevenueProtected,
            atRiskPipeline + (decimal)churnList.Where(c => c.ChurnProbability >= 50).Sum(c => c.ChurnProbability),
            expansionPotential,
            renewalValue,
            kpis.LostRevenue,
            kpis.RecoveryPipelineWeighted,
            outcomes.RevenueGenerated > 0 || kpis.RevenueClosed > 0 || openPipeline > 0);

        var health = BuildHealth(kpis, churnList.Count, expansionList.Count, forecast.ConfidencePercent);

        var audits = await _db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= since90)
            .OrderByDescending(a => a.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        var attribution = new List<OutcomeAttributionRowDto>();
        foreach (var a in audits)
        {
            var fabric = await _outcomeFabric.GetStatusAsync(a.Id, cancellationToken);
            attribution.Add(new OutcomeAttributionRowDto(
                a.Id, a.DecisionType, a.Action, a.Status,
                GetRevenue(a.Evidence), fabric?.LearningStatus ?? "pending", a.CreatedAt));
        }

        var insights = BuildInsights(nba, churnList, expansionList);
        var wonDeals = (await _db.Deals.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedWon)
            .CountAsync(cancellationToken));
        IReadOnlyList<WinLossBreakdownDto> winBreakdown = wonDeals > 0
            ? new List<WinLossBreakdownDto> { new("won", "ClosedWon", wonDeals, kpis.RevenueClosed, 100) }
            : Array.Empty<WinLossBreakdownDto>();

        var hasData = overview.HasData || attribution.Count > 0 || lossAnalysis.Count > 0;

        return new RevenueOsDashboardDto(
            overview, health, attribution, insights, forecast, lossAnalysis, winBreakdown, kpis, hasData);
    }

    private static RevenueHealthDto BuildHealth(RevenueKpiSnapshotDto kpis, int churnCount, int expansionCount, double confidence)
    {
        var win = (int)Math.Clamp(kpis.WinRate, 0, 100);
        var coverage = (int)Math.Clamp(kpis.PipelineCoveragePercent, 0, 100);
        var risk = Math.Clamp(churnCount * 8, 0, 100);
        var expansion = Math.Clamp(expansionCount * 10, 0, 100);
        var renewal = (int)Math.Clamp(confidence, 0, 100);
        var health = (int)Math.Clamp((win + coverage + (100 - risk)) / 3, 0, 100);
        var stability = (int)Math.Clamp(coverage, 0, 100);
        return new RevenueHealthDto(health, stability, risk, expansion, renewal);
    }

    private static List<RevenueInsightDto> BuildInsights(
        IReadOnlyList<NextBestActionDto> nba,
        IReadOnlyList<ChurnPredictionV2Dto> churn,
        IReadOnlyList<ExpansionIntelligenceDto> expansion)
    {
        var list = new List<RevenueInsightDto>();
        foreach (var c in churn.OrderByDescending(x => x.ChurnProbability).Take(5))
            list.Add(new("risk", c.CustomerName ?? "Cliente", $"Churn {c.ChurnProbability:F0}%", c.ChurnProbability, c.CustomerId));
        foreach (var e in expansion.OrderByDescending(x => x.ReadinessScore).Take(5))
            list.Add(new("expansion", e.CustomerName ?? "Cliente", e.ReadinessLevel, e.ReadinessScore, e.CustomerId));
        foreach (var n in nba.Take(5))
            list.Add(new("opportunity", n.EntityName, n.RecommendedAction, n.PriorityScore,
                n.EntityType == "Customer" ? n.EntityId : null));
        return list;
    }

    private static decimal? GetRevenue(Dictionary<string, object> evidence)
    {
        if (!evidence.TryGetValue(RevenueKey, out var v)) return null;
        return v switch
        {
            decimal d => d,
            double dbl => (decimal)dbl,
            int i => i,
            _ => null
        };
    }
}
