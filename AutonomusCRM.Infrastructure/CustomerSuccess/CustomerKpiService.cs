using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class CustomerKpiService : ICustomerKpiService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerContractRepository _contractRepository;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IChurnRiskEngine _churnRiskEngine;
    private readonly IDealRepository _dealRepository;

    public CustomerKpiService(
        ICustomerRepository customerRepository,
        ICustomerContractRepository contractRepository,
        ICustomerHealthEngine healthEngine,
        IChurnRiskEngine churnRiskEngine,
        IDealRepository dealRepository)
    {
        _customerRepository = customerRepository;
        _contractRepository = contractRepository;
        _healthEngine = healthEngine;
        _churnRiskEngine = churnRiskEngine;
        _dealRepository = dealRepository;
    }

    public async Task<CustomerKpiSnapshotDto> GetSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var active = customers.Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP).ToList();
        var health = await _healthEngine.CalculateAllAsync(tenantId, cancellationToken);
        var churnSignals = await _churnRiskEngine.DetectSignalsAsync(tenantId, cancellationToken: cancellationToken);
        var contracts = (await _contractRepository.GetByTenantAsync(tenantId, cancellationToken)).ToList();

        var avgHealth = health.Any() ? health.Average(h => h.HealthScore) : 0;
        var atRisk = health.Count(h => h.Classification != CustomerSuccessConstants.HealthHealthy);
        var churnPct = active.Count > 0 ? atRisk * 100.0 / active.Count : 0;

        var renewed = contracts.Count(c => c.Status == CustomerSuccessConstants.ContractActive && c.UpdatedAt > c.CreatedAt);
        var renewalRate = contracts.Count > 0 ? renewed * 100.0 / contracts.Count : 100;
        var retained = active.Count;
        var churned = customers.Count(c => c.Status == CustomerStatus.Churned);
        var retentionRate = (retained + churned) > 0 ? retained * 100.0 / (retained + churned) : 100;

        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var expansionMeta = customers.Where(c => c.Metadata.ContainsKey("ExpansionOpportunity")).ToList();
        var upsell = expansionMeta.Count(c => c.Metadata.GetValueOrDefault("ExpansionOpportunity")?.ToString() == "Upsell");
        var crossSell = expansionMeta.Count(c => c.Metadata.GetValueOrDefault("ExpansionOpportunity")?.ToString() == "CrossSell");
        var expansionDeals = deals.Where(d => d.Metadata.ContainsKey("ExpansionDeal")).Sum(d => d.Amount);

        var ltv = customers.Sum(c => c.LifetimeValue ?? 0);
        var avgAdoption = health.Any() ? health.Average(h => h.AdoptionScore) : 0;
        var avgEngagement = health.Any() ? health.Average(h => h.EngagementScore) : 0;

        return new CustomerKpiSnapshotDto(
            Math.Round(avgHealth, 1),
            atRisk,
            Math.Round(churnPct, 1),
            Math.Round(renewalRate, 1),
            Math.Round(retentionRate, 1),
            expansionDeals,
            upsell * 1000m,
            crossSell * 1000m,
            ltv,
            Math.Round(avgAdoption, 1),
            Math.Round(avgEngagement, 1));
    }
}
