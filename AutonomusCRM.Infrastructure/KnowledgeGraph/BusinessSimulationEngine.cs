using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

public sealed class BusinessSimulationEngine : IBusinessSimulationEngine
{
    private static readonly string[] Scenarios =
    [
        "customer_loss", "renewal", "expansion", "deal_won", "deal_lost",
        "churn_increase", "campaign_executed"
    ];

    private readonly IKnowledgeGraphService _graph;
    private readonly IGraphReasoningEngine _reasoning;
    private readonly ApplicationDbContext _db;

    public BusinessSimulationEngine(IKnowledgeGraphService graph, IGraphReasoningEngine reasoning, ApplicationDbContext db)
    {
        _graph = graph;
        _reasoning = reasoning;
        _db = db;
    }

    public IReadOnlyList<string> GetAvailableScenarios() => Scenarios;

    public async Task<SimulationScenarioResultDto> RunScenarioAsync(
        Guid tenantId, string scenarioKey, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var baseline = await RevenueSimulationCalculator.LoadBaselineAsync(_db, tenantId, cancellationToken);
        var historical = await _db.BusinessMemoryLearnings.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.SuccessRate)
            .Take(5)
            .ToListAsync(cancellationToken);

        var effects = new List<string>
        {
            $"MRR baseline: {baseline.Mrr:C}",
            $"ARR baseline: {baseline.Arr:C}",
            $"Open pipeline: {baseline.OpenPipeline:C}",
            $"Win rate: {baseline.WinRate:P1}",
            $"Churn rate: {baseline.ChurnRate:P1}",
            $"Lead velocity/mo: {baseline.LeadVelocityPerMonth:F1}"
        };
        effects.AddRange(historical.Select(l => $"{l.StrategyKey}: {l.SuccessRate:F0}% historical success"));

        var impact = RevenueSimulationCalculator.ProjectScenarioImpact(scenarioKey, baseline);
        var (title, narrative) = DescribeScenario(scenarioKey, baseline);

        if (customerId.HasValue)
        {
            var exp = await _reasoning.DetectExpansionPathAsync(tenantId, customerId.Value, cancellationToken);
            effects.Add(exp.Summary);
        }

        var hasHistory = baseline.HistoricalOutcomeCount > 0 || historical.Count > 0;
        return new SimulationScenarioResultDto(scenarioKey, title, narrative, effects, Math.Round(impact, 2), hasHistory);
    }

    private static (string Title, string Narrative) DescribeScenario(string key, RevenueSimulationBaseline b) => key switch
    {
        "customer_loss" => ("Pérdida de cliente", $"Proyecta pérdida MRR usando churn rate observado ({b.ChurnRate:P1})"),
        "renewal" => ("Renovación", $"Proyecta renovación usando win rate ({b.WinRate:P1}) sobre MRR {b.Mrr:C}"),
        "expansion" => ("Expansión", "Expansión estimada como 15% MRR ajustado por win rate y señales de grafo"),
        "deal_won" => ("Deal ganado", $"Impacto = avg deal {b.AvgDealSize:C} × win rate {b.WinRate:P1}"),
        "deal_lost" => ("Deal perdido", $"Pérdida esperada desde pipeline y win rate inverso"),
        "churn_increase" => ("Aumento churn", $"Escenario adverso: 3× churn rate base ({b.ChurnRate:P1})"),
        "campaign_executed" => ("Campaña ejecutada", $"Impacto desde lead velocity ({b.LeadVelocityPerMonth:F1}/mo) × deal size × win rate"),
        _ => ("Unknown scenario", "No simulation")
    };
}
