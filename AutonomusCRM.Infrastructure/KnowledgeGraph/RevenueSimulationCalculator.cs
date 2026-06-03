using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

public sealed class RevenueSimulationBaseline
{
    public decimal Mrr { get; init; }
    public decimal Arr => Mrr * 12;
    public decimal OpenPipeline { get; init; }
    public double WinRate { get; init; }
    public double ChurnRate { get; init; }
    public double LeadVelocityPerMonth { get; init; }
    public int CustomerCount { get; init; }
    public decimal AvgDealSize { get; init; }
    public int HistoricalOutcomeCount { get; init; }
}

public static class RevenueSimulationCalculator
{
    public static async Task<RevenueSimulationBaseline> LoadBaselineAsync(
        ApplicationDbContext db, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var since365 = DateTime.UtcNow.AddDays(-365);
        var since90 = DateTime.UtcNow.AddDays(-90);

        var deals = await db.Deals.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var won = deals.Where(d => d.Stage == DealStage.ClosedWon).ToList();
        var lost = deals.Where(d => d.Stage == DealStage.ClosedLost).ToList();
        var closedCount = won.Count + lost.Count;
        var winRate = closedCount > 0 ? won.Count / (double)closedCount : 0.25;

        var recentWon = won.Where(d =>
            (d.ClosedAt ?? d.CreatedAt) >= since365).ToList();
        var mrr = recentWon.Count > 0
            ? recentWon.Sum(d => d.Amount) / 12m
            : won.Sum(d => d.Amount) / Math.Max(1, 12);

        var openPipeline = deals
            .Where(d => d.Status == DealStatus.Open)
            .Sum(d => d.Amount * (d.Probability ?? 50) / 100m);

        var customers = await db.Customers.AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId, cancellationToken);

        var leads90 = await db.Leads.AsNoTracking()
            .CountAsync(l => l.TenantId == tenantId && l.CreatedAt >= since90, cancellationToken);
        var leadVelocity = leads90 / 3.0;

        var negativeOutcomes = await db.BusinessMemoryOutcomes.AsNoTracking()
            .CountAsync(o => o.TenantId == tenantId && !o.Succeeded, cancellationToken);
        var totalOutcomes = await db.BusinessMemoryOutcomes.AsNoTracking()
            .CountAsync(o => o.TenantId == tenantId, cancellationToken);
        var churnRate = customers > 0 && totalOutcomes > 0
            ? Math.Min(0.5, negativeOutcomes / (double)Math.Max(customers, 1))
            : 0.05;

        var avgDeal = deals.Count > 0 ? deals.Average(d => d.Amount) : 0m;

        return new RevenueSimulationBaseline
        {
            Mrr = Math.Round(mrr, 2),
            OpenPipeline = Math.Round(openPipeline, 2),
            WinRate = Math.Round(winRate, 4),
            ChurnRate = Math.Round(churnRate, 4),
            LeadVelocityPerMonth = Math.Round(leadVelocity, 2),
            CustomerCount = customers,
            AvgDealSize = Math.Round(avgDeal, 2),
            HistoricalOutcomeCount = totalOutcomes
        };
    }

    public static decimal ProjectScenarioImpact(string scenarioKey, RevenueSimulationBaseline baseline)
    {
        return scenarioKey switch
        {
            "customer_loss" => -baseline.Mrr * (decimal)Math.Clamp(baseline.ChurnRate * 2, 0.05, 0.35),
            "renewal" => baseline.Mrr * (decimal)Math.Clamp(baseline.WinRate * 0.8, 0.1, 0.9),
            "expansion" => baseline.Mrr * 0.15m * (decimal)Math.Clamp(baseline.WinRate + 0.2, 0.2, 1.0),
            "deal_won" => baseline.AvgDealSize > 0
                ? baseline.AvgDealSize * (decimal)baseline.WinRate
                : baseline.Mrr * 0.25m,
            "deal_lost" => baseline.AvgDealSize > 0
                ? -baseline.AvgDealSize * (1 - (decimal)baseline.WinRate)
                : -baseline.Mrr * 0.2m,
            "churn_increase" => -baseline.Mrr * (decimal)Math.Clamp(baseline.ChurnRate * 3, 0.1, 0.5),
            "campaign_executed" => (decimal)baseline.LeadVelocityPerMonth * baseline.AvgDealSize * (decimal)baseline.WinRate * 0.1m,
            _ => 0m
        };
    }
}
