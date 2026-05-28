using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class AutonomousCustomerSuccessEngine : IAutonomousCustomerSuccessEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAutonomousRevenueDecisionEngine _decisionEngine;
    private readonly IAutonomousPlaybookEngine _playbookEngine;
    private readonly IOperationalTaskService _taskService;

    public AutonomousCustomerSuccessEngine(
        ICustomerRepository customerRepository,
        IAutonomousRevenueDecisionEngine decisionEngine,
        IAutonomousPlaybookEngine playbookEngine,
        IOperationalTaskService taskService)
    {
        _customerRepository = customerRepository;
        _decisionEngine = decisionEngine;
        _playbookEngine = playbookEngine;
        _taskService = taskService;
    }

    public async Task<int> RunAutonomousCycleAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP).Take(25);
        var executed = 0;

        foreach (var customer in customers)
        {
            var decision = await _decisionEngine.DecideForCustomerAsync(tenantId, customer.Id, cancellationToken);
            if (decision.DecisionType == AutonomousConstants.DecisionNoAction)
                continue;

            var playbook = decision.DecisionType switch
            {
                AutonomousConstants.DecisionRescue => CustomerSuccessConstants.PlaybookRescue,
                AutonomousConstants.DecisionRenewal => CustomerSuccessConstants.PlaybookRenewal,
                AutonomousConstants.DecisionExpansion => CustomerSuccessConstants.PlaybookExpansion,
                AutonomousConstants.DecisionUpsell => CustomerSuccessConstants.PlaybookExpansion,
                AutonomousConstants.DecisionReEngagement => CustomerSuccessConstants.PlaybookReEngagement,
                _ => CustomerSuccessConstants.PlaybookAdoption
            };

            await _playbookEngine.StartOrAdvanceAsync(tenantId, customer.Id, playbook, cancellationToken);
            await _decisionEngine.ExecuteDecisionAsync(tenantId, decision, cancellationToken);

            if (!await _taskService.ExistsOpenTaskAsync(tenantId, "Customer", customer.Id, AutonomousConstants.TaskAutonomous, cancellationToken))
            {
                await _taskService.CreateTaskAsync(
                    tenantId,
                    $"Autónomo: {decision.DecisionType}",
                    decision.Reason,
                    "Customer",
                    customer.Id,
                    null,
                    DateTime.UtcNow.AddDays(3),
                    decision.Score >= 80 ? "Urgent" : "High",
                    AutonomousConstants.TaskAutonomous,
                    cancellationToken);
            }

            executed++;
        }

        await _playbookEngine.ProcessDuePlaybooksAsync(tenantId, cancellationToken);
        return executed;
    }
}
