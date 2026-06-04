using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class NextBestActionEngine : INextBestActionEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAutonomousRevenueDecisionEngine _decisionEngine;
    private readonly IRenewalEngine _renewalEngine;
    private readonly INextBestActionMlScorer _mlScorer;
    private readonly ISemanticMemoryService _semanticMemory;

    public NextBestActionEngine(
        ICustomerRepository customerRepository,
        IDealRepository dealRepository,
        IUserRepository userRepository,
        IAutonomousRevenueDecisionEngine decisionEngine,
        IRenewalEngine renewalEngine,
        INextBestActionMlScorer mlScorer,
        ISemanticMemoryService semanticMemory)
    {
        _customerRepository = customerRepository;
        _dealRepository = dealRepository;
        _userRepository = userRepository;
        _decisionEngine = decisionEngine;
        _renewalEngine = renewalEngine;
        _mlScorer = mlScorer;
        _semanticMemory = semanticMemory;
    }

    public async Task<IReadOnlyList<NextBestActionDto>> GetForTenantAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var actions = new List<NextBestActionDto>();
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP).Take(15);

        foreach (var c in customers)
        {
            var nba = await GetForCustomerAsync(tenantId, c.Id, cancellationToken);
            if (nba != null) actions.Add(nba);
        }

        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(d => d.Status == DealStatus.Open).Take(20);
        foreach (var d in deals)
        {
            var nba = await GetForDealAsync(tenantId, d.Id, cancellationToken);
            if (nba != null) actions.Add(nba);
        }

        return actions.OrderByDescending(a => a.PriorityScore).Take(40).ToList();
    }

    public async Task<NextBestActionDto?> GetForCustomerAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null) return null;

        var decision = await _decisionEngine.DecideForCustomerAsync(tenantId, customerId, cancellationToken);
        var renewals = await _renewalEngine.GetUpcomingRenewalsAsync(tenantId, cancellationToken);
        var renewal = renewals.FirstOrDefault(r => r.CustomerId == customerId);

        var (channel, due) = decision.DecisionType switch
        {
            AutonomousConstants.DecisionRescue => ("Phone", DateTime.UtcNow.AddDays(1)),
            AutonomousConstants.DecisionRenewal => ("Email", renewal?.RenewalDate.AddDays(-14) ?? DateTime.UtcNow.AddDays(7)),
            AutonomousConstants.DecisionExpansion => ("Meeting", DateTime.UtcNow.AddDays(7)),
            AutonomousConstants.DecisionReEngagement => ("WhatsApp", DateTime.UtcNow.AddDays(2)),
            _ => ("Email", DateTime.UtcNow.AddDays(5))
        };

        var mlBoost = _mlScorer.ScoreAction(decision.Action, channel, "Customer", tenantId);
        var reason = decision.Reason;
        var hits = await _semanticMemory.FindSimilarMemoriesAsync(
            tenantId, $"playbook {decision.Action} channel {channel} customer {customerId}", 3, cancellationToken);
        if (hits.Count > 0)
            reason += $" | Historical: {hits[0].Text[..Math.Min(100, hits[0].Text.Length)]}";

        return new NextBestActionDto(
            "Customer", customerId, customer.Name,
            decision.Action, channel, due, decision.Score + mlBoost, reason);
    }

    public async Task<NextBestActionDto?> GetForDealAsync(
        Guid tenantId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
        if (deal == null || deal.Status != DealStatus.Open) return null;

        var nba = await GetForCustomerAsync(tenantId, deal.CustomerId, cancellationToken);
        if (nba == null) return null;

        return nba with
        {
            EntityType = "Deal",
            EntityId = dealId,
            EntityName = deal.Title,
            RecommendedAction = deal.Stage == DealStage.Negotiation ? "CloseDeal" : nba.RecommendedAction,
            PriorityScore = nba.PriorityScore + (deal.Amount > 10000 ? 10 : 0)
        };
    }
}
