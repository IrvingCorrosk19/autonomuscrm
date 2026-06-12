using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class ExpansionRevenueEngine : IExpansionRevenueEngine
{
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IDealRepository _dealRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOperationalTaskService _taskService;

    public ExpansionRevenueEngine(
        ICustomerRepository customerRepository,
        ICustomerHealthEngine healthEngine,
        IDealRepository dealRepository,
        IOperationalTaskService taskService)
    {
        _customerRepository = customerRepository;
        _healthEngine = healthEngine;
        _dealRepository = dealRepository;
        _taskService = taskService;
    }

    public async Task<IReadOnlyList<ExpansionOpportunityDto>> DetectOpportunitiesAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var health = await _healthEngine.CalculateAllAsync(tenantId, cancellationToken);
        var customers = (await _customerRepository.GetExpansionCustomerProjectionsAsync(tenantId, cancellationToken))
            .ToDictionary(c => c.Id);
        var wonByCustomer = await _dealRepository.GetWonAmountByCustomerAsync(tenantId, cancellationToken);
        var opps = new List<ExpansionOpportunityDto>();

        foreach (var h in health.Where(x => x.Classification == CustomerSuccessConstants.HealthHealthy))
        {
            if (!customers.TryGetValue(h.CustomerId, out var customer))
                continue;

            var wonSum = wonByCustomer.GetValueOrDefault(customer.Id);

            if (customer.Status == CustomerStatus.VIP || wonSum >= 50_000)
            {
                opps.Add(new ExpansionOpportunityDto(
                    customer.Id,
                    customer.Name,
                    "Upsell",
                    "Cliente saludable con alto valor — proponer plan superior o módulos premium.",
                    wonSum * 0.2m));
            }

            if (customer.ProductLineHasComma)
            {
                opps.Add(new ExpansionOpportunityDto(
                    customer.Id,
                    customer.Name,
                    "CrossSell",
                    "Múltiples líneas de producto — oportunidad cross-sell.",
                    wonSum * 0.15m));
            }
            else if (h.AdoptionScore >= 70 && h.EngagementScore >= 70)
            {
                opps.Add(new ExpansionOpportunityDto(
                    customer.Id,
                    customer.Name,
                    "Expansion",
                    "Alta adopción y engagement — explorar expansión de licencias.",
                    wonSum * 0.1m));
            }
        }

        return opps;
    }

    public async Task<int> CreateExpansionTasksAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var opps = await DetectOpportunitiesAsync(tenantId, cancellationToken);
        var created = 0;
        foreach (var opp in opps.Take(20))
        {
            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Customer", opp.CustomerId, CustomerSuccessConstants.TaskExpansion, cancellationToken))
                continue;

            await _taskService.CreateTaskAsync(
                tenantId,
                $"Expansión ({opp.OpportunityType}): {opp.CustomerName}",
                opp.Recommendation,
                "Customer",
                opp.CustomerId,
                null,
                DateTime.UtcNow.AddDays(14),
                "Normal",
                CustomerSuccessConstants.TaskExpansion,
                cancellationToken);
            created++;
        }

        return created;
    }
}
