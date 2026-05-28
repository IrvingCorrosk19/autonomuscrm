using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class CustomerSuccessIntelligenceService : ICustomerSuccessIntelligenceService
{
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IChurnRiskEngine _churnRiskEngine;
    private readonly IRenewalEngine _renewalEngine;
    private readonly IExpansionRevenueEngine _expansionEngine;
    private readonly ICustomerPlaybookService _playbooks;

    public CustomerSuccessIntelligenceService(
        ICustomerHealthEngine healthEngine,
        IChurnRiskEngine churnRiskEngine,
        IRenewalEngine renewalEngine,
        IExpansionRevenueEngine expansionEngine,
        ICustomerPlaybookService playbooks)
    {
        _healthEngine = healthEngine;
        _churnRiskEngine = churnRiskEngine;
        _renewalEngine = renewalEngine;
        _expansionEngine = expansionEngine;
        _playbooks = playbooks;
    }

    public async Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunHealthIntelligenceAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var actions = new List<CustomerIntelligenceActionDto>();
        await _healthEngine.PersistHealthAsync(tenantId, customerId, cancellationToken);
        var health = await _healthEngine.CalculateHealthAsync(tenantId, customerId, cancellationToken);

        if (health.Classification == CustomerSuccessConstants.HealthCritical)
        {
            var pb = await _playbooks.ExecutePlaybookAsync(tenantId, customerId, CustomerSuccessConstants.PlaybookRescue, cancellationToken: cancellationToken);
            actions.Add(new CustomerIntelligenceActionDto(
                "CustomerHealthAgent", customerId, "PlaybookRescue",
                $"Health {health.HealthScore} — playbook Rescue ({pb.TasksCreated} tareas)",
                pb.TasksCreated > 0));
        }
        else if (health.Classification == CustomerSuccessConstants.HealthWarning)
        {
            var pb = await _playbooks.ExecutePlaybookAsync(tenantId, customerId, CustomerSuccessConstants.PlaybookAdoption, cancellationToken: cancellationToken);
            actions.Add(new CustomerIntelligenceActionDto(
                "CustomerHealthAgent", customerId, "PlaybookAdoption",
                $"Health {health.HealthScore} — playbook Adoption",
                pb.TasksCreated > 0));
        }

        return actions;
    }

    public async Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunChurnIntelligenceAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var signals = await _churnRiskEngine.DetectSignalsAsync(tenantId, customerId, cancellationToken);
        var acted = await _churnRiskEngine.EnforceAlertsAndPlaybooksAsync(tenantId, cancellationToken);
        return signals.Select(s => new CustomerIntelligenceActionDto(
            "ChurnRiskAgent", customerId, s.SignalType, s.Description, acted > 0)).ToList();
    }

    public async Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunRenewalIntelligenceAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var alerts = (await _renewalEngine.GetUpcomingRenewalsAsync(tenantId, cancellationToken))
            .Where(a => a.CustomerId == customerId).ToList();
        var tasks = await _renewalEngine.EnforceRenewalWindowsAsync(tenantId, cancellationToken);

        var actions = alerts.Select(a => new CustomerIntelligenceActionDto(
            "RenewalAgent", customerId, "RenewalAlert",
            $"Renovación en {a.DaysUntilRenewal}d — ventana {a.Window}",
            tasks > 0)).ToList();

        if (alerts.Any(a => a.DaysUntilRenewal <= 90))
        {
            var pb = await _playbooks.ExecutePlaybookAsync(tenantId, customerId, CustomerSuccessConstants.PlaybookRenewal, cancellationToken: cancellationToken);
            actions.Add(new CustomerIntelligenceActionDto(
                "RenewalAgent", customerId, "PlaybookRenewal",
                $"Playbook Renewal ({pb.TasksCreated} tareas)", pb.TasksCreated > 0));
        }

        return actions;
    }

    public async Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunExpansionIntelligenceAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var opps = (await _expansionEngine.DetectOpportunitiesAsync(tenantId, cancellationToken))
            .Where(o => o.CustomerId == customerId).ToList();
        var created = await _expansionEngine.CreateExpansionTasksAsync(tenantId, cancellationToken);

        return opps.Select(o => new CustomerIntelligenceActionDto(
            "ExpansionAgent", customerId, o.OpportunityType, o.Recommendation, created > 0)).ToList();
    }
}
