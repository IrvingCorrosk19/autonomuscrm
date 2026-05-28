using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AutonomusCRM.Workers.Agents;

public class CustomerRiskAgent
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<CustomerRiskAgent> _logger;

    public CustomerRiskAgent(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IAgentConfigurationService agentConfig,
        ILogger<CustomerRiskAgent> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task ProcessCustomerCreatedEvent(CustomerCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.TenantId == null)
            return;

        var config = await _agentConfig.GetConfigAsync(domainEvent.TenantId.Value, "CustomerRiskAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var customer = await _customerRepository.GetByIdAsync(domainEvent.CustomerId, cancellationToken);
        if (customer == null || customer.TenantId != domainEvent.TenantId)
            return;

        var riskScore = CalculateRiskScore(customer, config);
        customer.UpdateRiskScore(riskScore);

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var evt in customer.DomainEvents)
            await _eventBus.PublishAsync(evt, cancellationToken);
        customer.ClearDomainEvents();

        _logger.LogInformation("CustomerRiskAgent: Customer {CustomerId} risk={Risk}", customer.Id, riskScore);
    }

    private int CalculateRiskScore(Customer customer, Dictionary<string, object> config)
    {
        var riskScore = _agentConfig.GetValue(config, "BaseRiskScore", 50);
        var adjustments = GetAdjustments(config);

        if (string.IsNullOrWhiteSpace(customer.Email))
            riskScore += adjustments.GetValueOrDefault("NoEmail", 10);
        if (string.IsNullOrWhiteSpace(customer.Phone))
            riskScore += adjustments.GetValueOrDefault("NoPhone", 5);
        if (!string.IsNullOrWhiteSpace(customer.Company))
            riskScore += adjustments.GetValueOrDefault("HasCompany", -15);

        return Math.Max(0, Math.Min(100, riskScore));
    }

    private static Dictionary<string, int> GetAdjustments(Dictionary<string, object> config)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!config.TryGetValue("RiskAdjustments", out var raw) || raw is not JsonElement je || je.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in je.EnumerateObject())
        {
            if (prop.Value.TryGetInt32(out var v))
                result[prop.Name] = v;
        }

        return result;
    }
}
