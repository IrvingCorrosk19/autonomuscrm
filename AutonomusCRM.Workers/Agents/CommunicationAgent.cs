using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Leads.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

public class CommunicationAgent
{
    private readonly IEmailAutomationEngine _emailEngine;
    private readonly IWhatsAppAutomationEngine _whatsAppEngine;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<CommunicationAgent> _logger;

    public CommunicationAgent(
        IEmailAutomationEngine emailEngine,
        IWhatsAppAutomationEngine whatsAppEngine,
        ICustomerRepository customerRepository,
        ILeadRepository leadRepository,
        IAgentConfigurationService agentConfig,
        ILogger<CommunicationAgent> logger)
    {
        _emailEngine = emailEngine;
        _whatsAppEngine = whatsAppEngine;
        _customerRepository = customerRepository;
        _leadRepository = leadRepository;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task ProcessCustomerCreatedEvent(CustomerCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.TenantId == null)
            return;

        var config = await _agentConfig.GetConfigAsync(domainEvent.TenantId.Value, "CommunicationAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var customer = await _customerRepository.GetByIdAsync(domainEvent.CustomerId, cancellationToken);
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
            return;

        await _emailEngine.SendTemplatedAsync(
            domainEvent.TenantId.Value,
            "Welcome",
            "welcome",
            customer.Email,
            customer.Id,
            variables: new Dictionary<string, string> { ["name"] = customer.Name },
            cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(customer.Phone))
        {
            await _whatsAppEngine.SendTemplatedAsync(
                domainEvent.TenantId.Value,
                "Welcome",
                "welcome",
                customer.Phone,
                customer.Id,
                variables: new Dictionary<string, string> { ["name"] = customer.Name },
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("CommunicationAgent: welcome sent for customer {CustomerId}", domainEvent.CustomerId);
    }

    public async Task ProcessLeadCreatedEvent(LeadCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.TenantId == null)
            return;

        var config = await _agentConfig.GetConfigAsync(domainEvent.TenantId.Value, "CommunicationAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var lead = await _leadRepository.GetByIdAsync(domainEvent.LeadId, cancellationToken);
        if (lead == null || string.IsNullOrWhiteSpace(lead.Email))
            return;

        await _emailEngine.SendTemplatedAsync(
            domainEvent.TenantId.Value,
            "LeadWelcome",
            "followup",
            lead.Email,
            leadId: lead.Id,
            variables: new Dictionary<string, string> { ["name"] = lead.Name },
            cancellationToken: cancellationToken);

        _logger.LogInformation("CommunicationAgent: lead follow-up email for {LeadId}", domainEvent.LeadId);
    }
}
