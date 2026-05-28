using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class RevenueAutonomousAgent : IRevenueAutonomousAgent
{
    private readonly ISalesIntelligenceService _intel;
    private readonly IDealRepository _deals;
    private readonly IAutonomousRevenueDecisionEngine _decisions;

    public RevenueAutonomousAgent(ISalesIntelligenceService intel, IDealRepository deals, IAutonomousRevenueDecisionEngine decisions)
    {
        _intel = intel;
        _deals = deals;
        _decisions = decisions;
    }

    public async Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default)
    {
        var open = (await _deals.GetByTenantIdAsync(tenantId, ct)).Where(d => d.Status == DealStatus.Open).Take(10);
        var tasks = 0;
        foreach (var deal in open)
        {
            var actions = await _intel.AnalyzeAndActAsync(tenantId, deal.Id, ct);
            tasks += actions.Count;
        }
        return new AgentRunResultDto("RevenueAgent", open.Count(), tasks, tasks);
    }
}

public class RenewalAutonomousAgent : IRenewalAutonomousAgent
{
    private readonly IRenewalEngine _renewal;
    private readonly IAutonomousRevenueDecisionEngine _decisions;
    private readonly ICustomerRepository _customers;

    public RenewalAutonomousAgent(IRenewalEngine renewal, IAutonomousRevenueDecisionEngine decisions, ICustomerRepository customers)
    {
        _renewal = renewal;
        _decisions = decisions;
        _customers = customers;
    }

    public async Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default)
    {
        var created = await _renewal.EnforceRenewalWindowsAsync(tenantId, ct);
        var upcoming = await _renewal.GetUpcomingRenewalsAsync(tenantId, ct);
        var executed = 0;
        foreach (var r in upcoming.Where(x => x.DaysUntilRenewal <= 60).Take(10))
        {
            var d = await _decisions.DecideForCustomerAsync(tenantId, r.CustomerId, ct);
            if (d.DecisionType == AutonomousConstants.DecisionRenewal)
            {
                await _decisions.ExecuteDecisionAsync(tenantId, d, ct);
                executed++;
            }
        }
        return new AgentRunResultDto("RenewalAgent", upcoming.Count, executed, created);
    }
}

public class ChurnAutonomousAgent : IChurnAutonomousAgent
{
    private readonly IChurnRiskEngine _churn;
    private readonly IChurnPredictionV2 _churnV2;
    private readonly IAutonomousRevenueDecisionEngine _decisions;

    public ChurnAutonomousAgent(IChurnRiskEngine churn, IChurnPredictionV2 churnV2, IAutonomousRevenueDecisionEngine decisions)
    {
        _churn = churn;
        _churnV2 = churnV2;
        _decisions = decisions;
    }

    public async Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default)
    {
        var alerts = await _churn.EnforceAlertsAndPlaybooksAsync(tenantId, ct);
        var preds = await _churnV2.PredictAsync(tenantId, cancellationToken: ct);
        var executed = 0;
        foreach (var p in preds.Where(x => x.ChurnProbability >= 70).Take(10))
        {
            var d = await _decisions.DecideForCustomerAsync(tenantId, p.CustomerId, ct);
            await _decisions.ExecuteDecisionAsync(tenantId, d, ct);
            executed++;
        }
        return new AgentRunResultDto("ChurnAgent", preds.Count, executed, alerts);
    }
}

public class ExpansionAutonomousAgent : IExpansionAutonomousAgent
{
    private readonly IExpansionIntelligence _expansion;
    private readonly IExpansionRevenueEngine _expansionEngine;
    private readonly IAutonomousRevenueDecisionEngine _decisions;

    public ExpansionAutonomousAgent(
        IExpansionIntelligence expansion,
        IExpansionRevenueEngine expansionEngine,
        IAutonomousRevenueDecisionEngine decisions)
    {
        _expansion = expansion;
        _expansionEngine = expansionEngine;
        _decisions = decisions;
    }

    public async Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tasks = await _expansionEngine.CreateExpansionTasksAsync(tenantId, ct);
        var ready = (await _expansion.AnalyzeAsync(tenantId, ct)).Where(e => e.ReadinessLevel == "Ready").Take(8);
        var executed = 0;
        foreach (var e in ready)
        {
            var d = await _decisions.DecideForCustomerAsync(tenantId, e.CustomerId, ct);
            if (d.DecisionType is AutonomousConstants.DecisionExpansion or AutonomousConstants.DecisionUpsell)
            {
                await _decisions.ExecuteDecisionAsync(tenantId, d, ct);
                executed++;
            }
        }
        return new AgentRunResultDto("ExpansionAgent", ready.Count(), executed, tasks);
    }
}

public class CustomerAutonomousAgent : ICustomerAutonomousAgent
{
    private readonly IAutonomousCustomerSuccessEngine _cs;
    private readonly ICustomerInsightsAgentService _insights;

    public CustomerAutonomousAgent(IAutonomousCustomerSuccessEngine cs, ICustomerInsightsAgentService insights)
    {
        _cs = cs;
        _insights = insights;
    }

    public async Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default)
    {
        var executed = await _cs.RunAutonomousCycleAsync(tenantId, ct);
        var actions = await _insights.AnalyzeAndActAsync(tenantId, ct);
        return new AgentRunResultDto("CustomerAgent", executed, executed, actions.Count);
    }
}

public class OperationsAutonomousAgent : IOperationsAutonomousAgent
{
    private readonly IRevenueAutomationEngine _revenue;
    private readonly IDataQualityRevenueService _dq;
    private readonly IIntelligenceAutomationEngine _intel;

    public OperationsAutonomousAgent(
        IRevenueAutomationEngine revenue,
        IDataQualityRevenueService dq,
        IIntelligenceAutomationEngine intel)
    {
        _revenue = revenue;
        _dq = dq;
        _intel = intel;
    }

    public async Task<AgentRunResultDto> RunAsync(Guid tenantId, CancellationToken ct = default)
    {
        await _revenue.RunPeriodicRevenueScanAsync(tenantId, ct);
        var dqTasks = await _dq.ScanAndCreateTasksAsync(tenantId, ct);
        await _intel.RunPeriodicIntelligenceScanAsync(tenantId, ct);
        return new AgentRunResultDto("OperationsAgent", 3, 3, dqTasks);
    }
}
