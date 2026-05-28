using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class ExecutiveCustomerDashboardService : IExecutiveCustomerDashboardService
{
    private readonly ICustomerKpiService _kpis;
    private readonly ICustomerHealthEngine _health;
    private readonly IChurnRiskEngine _churn;
    private readonly IRenewalEngine _renewal;
    private readonly IExpansionRevenueEngine _expansion;
    private readonly ICustomerJourneyEngine _journey;

    public ExecutiveCustomerDashboardService(
        ICustomerKpiService kpis,
        ICustomerHealthEngine health,
        IChurnRiskEngine churn,
        IRenewalEngine renewal,
        IExpansionRevenueEngine expansion,
        ICustomerJourneyEngine journey)
    {
        _kpis = kpis;
        _health = health;
        _churn = churn;
        _renewal = renewal;
        _expansion = expansion;
        _journey = journey;
    }

    public async Task<ExecutiveCustomerDashboardDto> GetDashboardAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var healthAll = await _health.CalculateAllAsync(tenantId, cancellationToken);
        return new ExecutiveCustomerDashboardDto(
            await _kpis.GetSnapshotAsync(tenantId, cancellationToken),
            healthAll.Take(25).ToList(),
            (await _churn.DetectSignalsAsync(tenantId, cancellationToken: cancellationToken)).Take(15).ToList(),
            (await _renewal.GetUpcomingRenewalsAsync(tenantId, cancellationToken)).Take(20).ToList(),
            (await _expansion.DetectOpportunitiesAsync(tenantId, cancellationToken)).Take(15).ToList(),
            await _journey.GetJourneyMetricsAsync(tenantId, cancellationToken),
            await _renewal.GetRenewalForecastAsync(tenantId, 90, cancellationToken));
    }
}
