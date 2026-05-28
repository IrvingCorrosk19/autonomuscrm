using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class AutonomousCommunicationsEngine : IAutonomousCommunicationsEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailAutomationEngine _email;
    private readonly IWhatsAppAutomationEngine _whatsApp;

    public AutonomousCommunicationsEngine(
        ICustomerRepository customerRepository,
        IEmailAutomationEngine email,
        IWhatsAppAutomationEngine whatsApp)
    {
        _customerRepository = customerRepository;
        _email = email;
        _whatsApp = whatsApp;
    }

    public async Task<int> ExecuteForDecisionAsync(
        Guid tenantId, AutonomousDecisionDto decision, CancellationToken cancellationToken = default)
    {
        if (!decision.CustomerId.HasValue)
            return 0;

        var customer = await _customerRepository.GetByIdAsync(decision.CustomerId.Value, cancellationToken);
        if (customer == null) return 0;

        var vars = new Dictionary<string, string> { ["name"] = customer.Name };
        var sent = 0;

        switch (decision.DecisionType)
        {
            case AutonomousConstants.DecisionRescue:
                if (!string.IsNullOrWhiteSpace(customer.Email))
                {
                    await _email.SendTemplatedAsync(tenantId, "Risk", "risk", customer.Email, customer.Id, variables: vars, cancellationToken: cancellationToken);
                    sent++;
                }
                break;
            case AutonomousConstants.DecisionRenewal:
                if (!string.IsNullOrWhiteSpace(customer.Email))
                {
                    vars["renewal_date"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
                    await _email.SendTemplatedAsync(tenantId, "Renewal", "renewal", customer.Email, customer.Id, variables: vars, cancellationToken: cancellationToken);
                    sent++;
                }
                break;
            case AutonomousConstants.DecisionReEngagement:
                if (!string.IsNullOrWhiteSpace(customer.Phone))
                {
                    await _whatsApp.SendTemplatedAsync(tenantId, "Recovery", "recovery", customer.Phone, customer.Id, variables: vars, cancellationToken: cancellationToken);
                    sent++;
                }
                break;
            case AutonomousConstants.DecisionExpansion:
            case AutonomousConstants.DecisionUpsell:
                if (!string.IsNullOrWhiteSpace(customer.Email))
                {
                    await _email.SendTemplatedAsync(tenantId, "FollowUp", "followup", customer.Email, customer.Id, variables: vars, cancellationToken: cancellationToken);
                    sent++;
                }
                break;
        }

        return sent;
    }
}
