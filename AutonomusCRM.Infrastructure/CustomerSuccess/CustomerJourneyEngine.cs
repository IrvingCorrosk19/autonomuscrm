using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;

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
        var leads = (await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var health = await _healthEngine.CalculateAllAsync(tenantId, cancellationToken);
        var avgHealth = health.Any() ? health.Average(h => h.HealthScore) : (double?)null;

        var won = deals.Where(d => d.Stage == DealStage.ClosedWon).ToList();
        var activeCustomers = customers.Count(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP);
        var onboarding = customers.Count(c => c.Metadata.ContainsKey("OnboardingStarted"));
        var renewing = customers.Count(c => c.Metadata.ContainsKey("RenewalInProgress"));
        var expansion = customers.Count(c => c.Metadata.ContainsKey("ExpansionOpportunity"));

        double? leadToDeal = leads.Count > 0
            ? deals.Count(d => d.Metadata.ContainsKey("LeadId")) * 100.0 / leads.Count
            : null;

        double? dealToCustomer = deals.Count > 0
            ? won.Count * 100.0 / deals.Count(d => d.Stage == DealStage.ClosedWon || d.Stage == DealStage.ClosedLost)
            : null;

        var cycleDays = won.Where(d => d.ClosedAt.HasValue)
            .Select(d => (d.ClosedAt!.Value - d.CreatedAt).TotalDays)
            .Where(d => d >= 0).ToList();

        return
        [
            new("Lead", leads.Count, null, null, null),
            new("Deal", deals.Count(d => d.Status == DealStatus.Open), cycleDays.Any() ? cycleDays.Average() : null, leadToDeal, null),
            new("Customer", activeCustomers, null, dealToCustomer, avgHealth),
            new("Onboarding", onboarding > 0 ? onboarding : won.Count, 30, null, avgHealth),
            new("Active", activeCustomers, null, null, avgHealth),
            new("Renewal", renewing, 90, null, avgHealth),
            new("Expansion", expansion, null, null, avgHealth)
        ];
    }
}
