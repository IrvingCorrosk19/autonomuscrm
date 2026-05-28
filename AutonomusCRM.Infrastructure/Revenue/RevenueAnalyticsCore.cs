using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Users;

namespace AutonomusCRM.Infrastructure.Revenue;

internal static class RevenueAnalyticsCore
{
    public static decimal WeightedAmount(Deal d) => d.Amount * (d.Probability ?? 0) / 100m;

    public static double HistoricalWinRate(IEnumerable<Deal> deals)
    {
        var won = deals.Count(d => d.Stage == DealStage.ClosedWon);
        var lost = deals.Count(d => d.Stage == DealStage.ClosedLost);
        return (won + lost) > 0 ? won * 100.0 / (won + lost) : 50.0;
    }

    public static (DateTime Start, DateTime End) CurrentMonthlyPeriod()
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddDays(-1);
        return (start, end);
    }

    public static string? GetMetadataString(Dictionary<string, object> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var val) || val == null)
            return null;
        return val.ToString();
    }
}
