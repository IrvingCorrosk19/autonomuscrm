using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class RetentionAutomationEngine : IRetentionAutomationEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICustomerContractRepository _contractRepository;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IChurnRiskEngine _churnRiskEngine;
    private readonly IRenewalEngine _renewalEngine;
    private readonly ICustomerPlaybookService _playbooks;
    private readonly IEmailAutomationEngine _emailEngine;
    private readonly IWhatsAppAutomationEngine _whatsAppEngine;
    private readonly IExpansionRevenueEngine _expansionEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RetentionAutomationEngine> _logger;

    public RetentionAutomationEngine(
        ICustomerRepository customerRepository,
        IDealRepository dealRepository,
        ICustomerContractRepository contractRepository,
        ICustomerHealthEngine healthEngine,
        IChurnRiskEngine churnRiskEngine,
        IRenewalEngine renewalEngine,
        ICustomerPlaybookService playbooks,
        IEmailAutomationEngine emailEngine,
        IWhatsAppAutomationEngine whatsAppEngine,
        IExpansionRevenueEngine expansionEngine,
        IUnitOfWork unitOfWork,
        ILogger<RetentionAutomationEngine> logger)
    {
        _customerRepository = customerRepository;
        _dealRepository = dealRepository;
        _contractRepository = contractRepository;
        _healthEngine = healthEngine;
        _churnRiskEngine = churnRiskEngine;
        _renewalEngine = renewalEngine;
        _playbooks = playbooks;
        _emailEngine = emailEngine;
        _whatsAppEngine = whatsAppEngine;
        _expansionEngine = expansionEngine;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId == null)
            return;

        var tenantId = domainEvent.TenantId.Value;

        switch (domainEvent)
        {
            case CustomerCreatedEvent cce:
                await OnCustomerCreatedAsync(tenantId, cce.CustomerId, cancellationToken);
                break;
            case DealClosedEvent dce:
                await OnDealWonAsync(tenantId, dce.DealId, cancellationToken);
                break;
            case CustomerRiskScoreUpdatedEvent rse when rse.RiskScore >= 70:
                await _playbooks.ExecutePlaybookAsync(tenantId, rse.CustomerId, CustomerSuccessConstants.PlaybookRescue, cancellationToken: cancellationToken);
                break;
        }
    }

    public async Task RunPeriodicRetentionScanAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var healthList = await _healthEngine.CalculateAllAsync(tenantId, cancellationToken);
        foreach (var h in healthList)
            await _healthEngine.PersistHealthAsync(tenantId, h.CustomerId, cancellationToken);

        foreach (var critical in healthList.Where(h => h.Classification == CustomerSuccessConstants.HealthCritical))
        {
            await _playbooks.ExecutePlaybookAsync(tenantId, critical.CustomerId, CustomerSuccessConstants.PlaybookRescue, cancellationToken: cancellationToken);
            var customer = await _customerRepository.GetByIdAsync(critical.CustomerId, cancellationToken);
            if (customer != null && !string.IsNullOrWhiteSpace(customer.Email))
            {
                await _emailEngine.SendTemplatedAsync(
                    tenantId, "Risk", "risk", customer.Email, customer.Id,
                    variables: new Dictionary<string, string> { ["name"] = customer.Name },
                    cancellationToken: cancellationToken);
            }
        }

        await _renewalEngine.EnforceRenewalWindowsAsync(tenantId, cancellationToken);
        await _churnRiskEngine.EnforceAlertsAndPlaybooksAsync(tenantId, cancellationToken);

        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        foreach (var c in customers.Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.Inactive))
        {
            if (!c.LastContactAt.HasValue || (DateTime.UtcNow - c.LastContactAt.Value).TotalDays > 45)
            {
                await _playbooks.ExecutePlaybookAsync(tenantId, c.Id, CustomerSuccessConstants.PlaybookReEngagement, cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(c.Phone))
                {
                    await _whatsAppEngine.SendTemplatedAsync(
                        tenantId, "ReEngagement", "recovery", c.Phone, c.Id,
                        variables: new Dictionary<string, string> { ["name"] = c.Name },
                        cancellationToken: cancellationToken);
                }
            }
        }

        await _expansionEngine.CreateExpansionTasksAsync(tenantId, cancellationToken);
        _logger.LogInformation("Retention scan completed for tenant {TenantId}", tenantId);
    }

    private async Task OnCustomerCreatedAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
            return;

        customer.ChangeStatus(CustomerStatus.Customer);
        customer.UpdateMetadata("JourneyStage", "Customer");
        customer.UpdateMetadata("OnboardingStarted", DateTime.UtcNow);
        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _playbooks.ExecutePlaybookAsync(tenantId, customerId, CustomerSuccessConstants.PlaybookOnboarding, cancellationToken: cancellationToken);
    }

    private async Task OnDealWonAsync(Guid tenantId, Guid dealId, CancellationToken cancellationToken)
    {
        var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
        if (deal == null || deal.Stage != DealStage.ClosedWon)
            return;

        var customer = await _customerRepository.GetByIdAsync(deal.CustomerId, cancellationToken);
        if (customer == null)
            return;

        customer.ChangeStatus(CustomerStatus.Customer);
        customer.UpdateLifetimeValue((customer.LifetimeValue ?? 0) + deal.Amount);
        customer.RecordPurchase(DateTime.UtcNow);
        customer.UpdateMetadata("JourneyStage", "Onboarding");
        customer.UpdateMetadata("OnboardingStarted", DateTime.UtcNow);
        await _customerRepository.UpdateAsync(customer, cancellationToken);

        var existing = (await _contractRepository.GetActiveByCustomerAsync(tenantId, customer.Id, cancellationToken)).ToList();
        if (!existing.Any())
        {
            var contract = CustomerContract.Create(
                tenantId,
                customer.Id,
                dealId,
                DateTime.UtcNow,
                deal.Amount * 12,
                12);
            await _contractRepository.AddAsync(contract, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _healthEngine.PersistHealthAsync(tenantId, customer.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            await _emailEngine.SendTemplatedAsync(
                tenantId, "Onboarding", "onboarding", customer.Email, customer.Id,
                variables: new Dictionary<string, string> { ["name"] = customer.Name },
                cancellationToken: cancellationToken);
        }
    }
}
