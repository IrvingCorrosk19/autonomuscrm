using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class CustomerJourneyEngine : ICustomerJourneyEngine
{
    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerHealthEngine _healthEngine;

    public CustomerJourneyEngine(
        ILeadRepository leadRepository,
        IDealRepository dealRepository,
        ICustomerRepository customerRepository,
        ICustomerHealthEngine healthEngine)
    {
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
        _customerRepository = customerRepository;
        _healthEngine = healthEngine;
    }

    public async Task<IReadOnlyList<JourneyStageMetricDto>> GetJourneyMetricsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var leadCount = await _leadRepository.CountByTenantAsync(tenantId, cancellationToken);
        var customerCounts = await _customerRepository.GetJourneyCustomerCountsAsync(tenantId, cancellationToken);
        var dealMetrics = await _dealRepository.GetJourneyDealMetricsAsync(tenantId, cancellationToken);
        var avgHealth = await _healthEngine.GetAverageHealthScoreAsync(tenantId, cancellationToken);

        double? leadToDeal = leadCount > 0
            ? dealMetrics.DealsWithLeadMetadataCount * 100.0 / leadCount
            : null;

        double? dealToCustomer = dealMetrics.ClosedOutcomeCount > 0
            ? dealMetrics.WonCount * 100.0 / dealMetrics.ClosedOutcomeCount
            : null;

        return
        [
            new("Lead", leadCount, null, null, null),
            new("Deal", dealMetrics.OpenDealCount, dealMetrics.AverageCycleDays, leadToDeal, null),
            new("Customer", customerCounts.ActiveCustomerCount, null, dealToCustomer, avgHealth),
            new("Onboarding", customerCounts.OnboardingCount > 0 ? customerCounts.OnboardingCount : dealMetrics.WonCount, 30, null, avgHealth),
            new("Active", customerCounts.ActiveCustomerCount, null, null, avgHealth),
            new("Renewal", customerCounts.RenewalCount, 90, null, avgHealth),
            new("Expansion", customerCounts.ExpansionMetadataCount, null, null, avgHealth)
        ];
    }
}
