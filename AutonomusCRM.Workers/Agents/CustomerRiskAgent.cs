using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

/// <summary>
/// Agente autónomo que evalúa el riesgo de clientes
/// </summary>
public class CustomerRiskAgent
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CustomerRiskAgent> _logger;

    public CustomerRiskAgent(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<CustomerRiskAgent> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessCustomerCreatedEvent(CustomerCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CustomerRiskAgent processing CustomerCreatedEvent for Customer {CustomerId}",
            domainEvent.CustomerId);

        if (domainEvent.TenantId == null)
            return;

        var customer = await _customerRepository.GetByIdAsync(domainEvent.CustomerId, cancellationToken);
        if (customer == null || customer.TenantId != domainEvent.TenantId)
            return;

        // Calcular riesgo inicial
        var riskScore = CalculateRiskScore(customer);
        customer.UpdateRiskScore(riskScore);

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventBus.PublishAsync(customer.DomainEvents.First(), cancellationToken);
        customer.ClearDomainEvents();

        _logger.LogInformation(
            "CustomerRiskAgent calculated risk score {RiskScore} for Customer {CustomerId}",
            riskScore,
            customer.Id);
    }

    private int CalculateRiskScore(Customer customer)
    {
        var riskScore = 50; // Riesgo base medio

        // Ajustar riesgo basado en información disponible
        if (string.IsNullOrWhiteSpace(customer.Email))
            riskScore += 10; // Mayor riesgo sin email
        if (string.IsNullOrWhiteSpace(customer.Phone))
            riskScore += 5; // Mayor riesgo sin teléfono

        // Si tiene empresa, menor riesgo
        if (!string.IsNullOrWhiteSpace(customer.Company))
            riskScore -= 15;

        return Math.Max(0, Math.Min(100, riskScore));
    }
}

