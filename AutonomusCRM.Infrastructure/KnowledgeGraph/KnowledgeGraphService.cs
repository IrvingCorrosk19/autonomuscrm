using System.Security.Cryptography;
using System.Text;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

public sealed class KnowledgeGraphService : IKnowledgeGraphService
{
    private readonly IKnowledgeGraphRepository _graph;
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IChurnPredictionModel _churn;
    private readonly IExpansionPredictionModel _expansion;
    private readonly ILogger<KnowledgeGraphService> _logger;

    public KnowledgeGraphService(
        IKnowledgeGraphRepository graph,
        ApplicationDbContext db,
        IUnitOfWork uow,
        IChurnPredictionModel churn,
        IExpansionPredictionModel expansion,
        ILogger<KnowledgeGraphService> logger)
    {
        _graph = graph;
        _db = db;
        _uow = uow;
        _churn = churn;
        _expansion = expansion;
        _logger = logger;
    }

    public async Task<int> BuildGraphAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await _graph.DeleteAllForTenantAsync(tenantId, cancellationToken);
        var builder = new GraphBuilder(tenantId, _graph);
        var created = 0;

        var customers = await _db.Customers.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Take(200)
            .ToListAsync(cancellationToken);

        var churnPreds = await _churn.PredictAsync(tenantId, cancellationToken: cancellationToken);
        var expansionPreds = await _expansion.PredictAsync(tenantId, cancellationToken: cancellationToken);

        foreach (var c in customers)
        {
            if (!string.IsNullOrWhiteSpace(c.Company))
            {
                var companyId = DeterministicId($"company:{tenantId}:{c.Company}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, c.Id, KnowledgeGraphNodeTypes.Company, companyId,
                    KnowledgeGraphRelations.BelongsToCompany, 1m, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(c.Email))
            {
                var contactId = DeterministicId($"contact:{tenantId}:{c.Email}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, c.Id, KnowledgeGraphNodeTypes.Contact, contactId,
                    KnowledgeGraphRelations.HasContact, 1m, cancellationToken);
            }

            var churn = churnPreds.FirstOrDefault(p => p.CustomerId == c.Id);
            if (churn is { ChurnProbabilityPercent: >= 50 })
            {
                var riskId = DeterministicId($"risk:{c.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, c.Id, KnowledgeGraphNodeTypes.Outcome, riskId,
                    KnowledgeGraphRelations.AtRisk, churn.ChurnProbabilityPercent / 100m, cancellationToken);
            }

            var exp = expansionPreds.FirstOrDefault(p => p.CustomerId == c.Id);
            if (exp is { ExpansionProbabilityPercent: >= 50 })
            {
                var expId = DeterministicId($"expansion:{c.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, c.Id, KnowledgeGraphNodeTypes.Revenue, expId,
                    KnowledgeGraphRelations.ExpansionReady, exp.ExpansionProbabilityPercent / 100m, cancellationToken);
            }

            if (c.LifetimeValue is > 0)
            {
                var revId = DeterministicId($"ltv:{c.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, c.Id, KnowledgeGraphNodeTypes.Revenue, revId,
                    KnowledgeGraphRelations.GeneratedRevenue, (decimal)c.LifetimeValue / 10000m, cancellationToken);
            }
        }

        var deals = await _db.Deals.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .Take(300)
            .ToListAsync(cancellationToken);

        foreach (var d in deals)
        {
            await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, d.CustomerId, KnowledgeGraphNodeTypes.Deal, d.Id,
                KnowledgeGraphRelations.HasDeal, d.Amount / 10000m, cancellationToken);

            var productId = DeterministicId($"product:{d.Id}");
            await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, d.CustomerId, KnowledgeGraphNodeTypes.Product, productId,
                KnowledgeGraphRelations.BoughtProduct, 1m, cancellationToken);

            if (d.Stage == DealStage.ClosedWon)
            {
                var revId = DeterministicId($"deal-rev:{d.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Deal, d.Id, KnowledgeGraphNodeTypes.Revenue, revId,
                    KnowledgeGraphRelations.GeneratedRevenue, d.Amount / 10000m, cancellationToken);

                var invoiceId = DeterministicId($"invoice:{d.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Deal, d.Id, KnowledgeGraphNodeTypes.Invoice, invoiceId,
                    KnowledgeGraphRelations.GeneratedRevenue, 1m, cancellationToken);

                var paymentId = DeterministicId($"payment:{d.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Invoice, invoiceId, KnowledgeGraphNodeTypes.Payment, paymentId,
                    KnowledgeGraphRelations.GeneratedRevenue, 1m, cancellationToken);
            }
        }

        var contracts = await _db.CustomerContracts.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Take(100)
            .ToListAsync(cancellationToken);
        foreach (var contract in contracts)
        {
            var invId = DeterministicId($"contract-inv:{contract.Id}");
            await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, contract.CustomerId, KnowledgeGraphNodeTypes.Invoice, invId,
                KnowledgeGraphRelations.GeneratedRevenue, 1m, cancellationToken);
        }

        var audits = await _db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (var a in audits)
        {
            if (a.CustomerId.HasValue)
            {
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, a.CustomerId.Value, KnowledgeGraphNodeTypes.Decision, a.Id,
                    KnowledgeGraphRelations.ContextFor, a.DecisionScore / 100m, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(a.AgentName))
            {
                var agentId = DeterministicId($"agent:{tenantId}:{a.AgentName}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Agent, agentId, KnowledgeGraphNodeTypes.Decision, a.Id,
                    KnowledgeGraphRelations.ExecutedDecision, 1m, cancellationToken);
                if (a.CustomerId.HasValue)
                    await builder.LinkAsync(KnowledgeGraphNodeTypes.Agent, agentId, KnowledgeGraphNodeTypes.Customer, a.CustomerId.Value,
                        KnowledgeGraphRelations.InfluencedByAgent, 1m, cancellationToken);
            }

            if (a.BusinessSucceeded.HasValue)
            {
                var outcomeId = DeterministicId($"audit-outcome:{a.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Decision, a.Id, KnowledgeGraphNodeTypes.Outcome, outcomeId,
                    KnowledgeGraphRelations.ProducedOutcome, a.BusinessSucceeded.Value ? 1m : 0.3m, cancellationToken);

                if (a.BusinessSucceeded.Value && a.CustomerId.HasValue)
                {
                    var revId = DeterministicId($"outcome-rev:{a.Id}");
                    await builder.LinkAsync(KnowledgeGraphNodeTypes.Outcome, outcomeId, KnowledgeGraphNodeTypes.Revenue, revId,
                        KnowledgeGraphRelations.GeneratedRevenue, 1m, cancellationToken);
                    await builder.LinkAsync(KnowledgeGraphNodeTypes.Outcome, outcomeId, KnowledgeGraphNodeTypes.Customer, a.CustomerId.Value,
                        KnowledgeGraphRelations.GeneratedRevenue, 1m, cancellationToken);
                }
            }
        }

        var memories = await _db.BusinessMemoryRoots.AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Take(200)
            .ToListAsync(cancellationToken);
        foreach (var m in memories)
        {
            if (m.SubjectType == BusinessMemoryConstants.SubjectCustomer)
            {
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Memory, m.Id, KnowledgeGraphNodeTypes.Customer, m.SubjectId,
                    KnowledgeGraphRelations.LinkedToMemory, m.Importance / 10m, cancellationToken);
            }

            var decisions = await _db.BusinessMemoryDecisions.AsNoTracking()
                .Where(d => d.MemoryId == m.Id)
                .ToListAsync(cancellationToken);
            foreach (var d in decisions)
            {
                if (d.AiDecisionAuditId.HasValue)
                {
                    await builder.LinkAsync(KnowledgeGraphNodeTypes.Memory, m.Id, KnowledgeGraphNodeTypes.Decision, d.AiDecisionAuditId.Value,
                        KnowledgeGraphRelations.SupportsDecision, 1m, cancellationToken);
                }
            }
        }

        var learnings = await _db.BusinessMemoryLearnings.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .Take(100)
            .ToListAsync(cancellationToken);
        var allOutcomes = await _db.BusinessMemoryOutcomes.AsNoTracking()
            .Where(o => o.TenantId == tenantId)
            .Take(200)
            .ToListAsync(cancellationToken);
        foreach (var l in learnings)
        {
            var prefix = l.StrategyKey.Split('.')[0];
            foreach (var o in allOutcomes.Where(o => o.OutcomeCategory.Contains(prefix, StringComparison.OrdinalIgnoreCase)).Take(3))
            {
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Learning, l.Id, KnowledgeGraphNodeTypes.Outcome, o.Id,
                    KnowledgeGraphRelations.DerivedFromOutcome, l.SuccessRate / 100m, cancellationToken);
            }
        }

        var memRels = await _db.BusinessMemoryRelationships.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .Take(300)
            .ToListAsync(cancellationToken);
        foreach (var r in memRels)
        {
            await builder.LinkAsync(
                MapSubjectToNode(r.FromType), r.FromId,
                MapSubjectToNode(r.ToType), r.ToId,
                r.RelationType, (decimal)r.Weight, cancellationToken);
        }

        var playbooks = await _db.AutonomousPlaybookStates.AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .Take(100)
            .ToListAsync(cancellationToken);
        foreach (var p in playbooks)
        {
            var campaignId = DeterministicId($"campaign:{p.PlaybookType}");
            await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, p.CustomerId, KnowledgeGraphNodeTypes.Campaign, campaignId,
                KnowledgeGraphRelations.RanCampaign, 1m, cancellationToken);
        }

        var nbaOutcomes = await _db.NbaOutcomeRecords.AsNoTracking()
            .Where(n => n.TenantId == tenantId)
            .Take(200)
            .ToListAsync(cancellationToken);
        foreach (var n in nbaOutcomes)
        {
            if (n.EntityType == "Customer")
            {
                var outcomeId = DeterministicId($"nba:{n.Id}");
                await builder.LinkAsync(KnowledgeGraphNodeTypes.Customer, n.EntityId, KnowledgeGraphNodeTypes.Outcome, outcomeId,
                    KnowledgeGraphRelations.ProducedOutcome, n.Converted ? 1m : 0.4m, cancellationToken);
                if (n.Converted)
                {
                    var revId = DeterministicId($"nba-rev:{n.Id}");
                    await builder.LinkAsync(KnowledgeGraphNodeTypes.Outcome, outcomeId, KnowledgeGraphNodeTypes.Revenue, revId,
                        KnowledgeGraphRelations.GeneratedRevenue, n.ImpactScore, cancellationToken);
                }
            }
        }

        created = builder.CreatedCount;
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Knowledge graph built for tenant {TenantId}: {Edges} edges", tenantId, created);
        return created;
    }

    public async Task<CustomerKnowledgeGraphDto> GetCustomerGraphAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == customerId, cancellationToken);
        var name = customer?.Name ?? customerId.ToString();

        var edges = (await _graph.GetEdgesForCustomerAsync(tenantId, customerId, 500, cancellationToken)).ToList();
        var (nodes, dtoEdges) = Materialize(edges, 80);

        var influences = edges
            .Where(e => e.SourceType == KnowledgeGraphNodeTypes.Agent || e.RelationType == KnowledgeGraphRelations.InfluencedByAgent)
            .Select(e => $"{e.SourceType} → {e.RelationType}")
            .Distinct()
            .Take(10)
            .ToList();

        var outcomes = edges
            .Where(e => e.TargetType == KnowledgeGraphNodeTypes.Outcome || e.RelationType == KnowledgeGraphRelations.ProducedOutcome)
            .Select(e => e.RelationType)
            .Distinct()
            .Take(10)
            .ToList();

        var revenue = edges
            .Where(e => e.TargetType == KnowledgeGraphNodeTypes.Revenue || e.RelationType == KnowledgeGraphRelations.GeneratedRevenue)
            .Select(e => $"{e.SourceType}→{e.TargetType} ({e.Weight:N1})")
            .Take(10)
            .ToList();

        var risks = edges.Where(e => e.RelationType == KnowledgeGraphRelations.AtRisk).Select(e => "Riesgo churn elevado").ToList();
        var expansion = edges.Where(e => e.RelationType == KnowledgeGraphRelations.ExpansionReady).Select(e => "Expansión detectada").ToList();

        var exploration = BuildCustomerExploration(customer, edges, audits: await LoadAuditsForCustomer(tenantId, customerId, cancellationToken));

        return new CustomerKnowledgeGraphDto(
            customerId, name, nodes, dtoEdges, influences, outcomes, revenue, risks, expansion, exploration);
    }

    public async Task<BusinessKnowledgeGraphViewDto> GetBusinessGraphAsync(
        Guid tenantId, int maxNodes = 150, CancellationToken cancellationToken = default)
    {
        var edges = (await _graph.GetEdgesAsync(tenantId, maxNodes * 3, cancellationToken)).ToList();
        var (nodes, dtoEdges) = Materialize(edges, maxNodes);
        var exploration = new GraphExplorationDto(new[]
        {
            new GraphExplorationAnswerDto(
                "¿Qué decisiones generaron revenue?",
                SummarizeRevenueDecisions(edges),
                edges.Where(e => e.RelationType == KnowledgeGraphRelations.GeneratedRevenue).Take(5).Select(e => e.SourceId.ToString()).ToList())
        });
        return new BusinessKnowledgeGraphViewDto(tenantId, nodes, dtoEdges, edges.Count, exploration);
    }

    public async Task<DecisionKnowledgeGraphDto?> GetDecisionGraphAsync(
        Guid tenantId, Guid decisionAuditId, CancellationToken cancellationToken = default)
    {
        var audit = await _db.AiDecisionAudits.AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == decisionAuditId, cancellationToken);
        if (audit is null) return null;

        var edges = (await _graph.GetEdgesAsync(tenantId, 2000, cancellationToken))
            .Where(e => e.SourceId == decisionAuditId || e.TargetId == decisionAuditId ||
                        (e.SourceType == KnowledgeGraphNodeTypes.Decision && e.SourceId == decisionAuditId) ||
                        (e.TargetType == KnowledgeGraphNodeTypes.Decision && e.TargetId == decisionAuditId))
            .ToList();

        if (audit.CustomerId.HasValue)
            edges.AddRange(await _graph.GetEdgesForCustomerAsync(tenantId, audit.CustomerId.Value, 100, cancellationToken));

        edges = edges.DistinctBy(e => e.Id).ToList();
        var (nodes, dtoEdges) = Materialize(edges, 40);

        var memoryLinks = edges.Where(e => e.SourceType == KnowledgeGraphNodeTypes.Memory || e.RelationType == KnowledgeGraphRelations.SupportsDecision)
            .Select(e => e.RelationType).ToList();
        var outcomeLinks = edges.Where(e => e.TargetType == KnowledgeGraphNodeTypes.Outcome).Select(e => e.RelationType).ToList();
        var revenueLinks = edges.Where(e => e.TargetType == KnowledgeGraphNodeTypes.Revenue).Select(e => e.RelationType).ToList();
        var learningLinks = edges.Where(e => e.SourceType == KnowledgeGraphNodeTypes.Learning).Select(e => e.RelationType).ToList();

        return new DecisionKnowledgeGraphDto(
            decisionAuditId, audit.DecisionType, audit.Action, nodes, dtoEdges,
            new[] { audit.Reason },
            memoryLinks, outcomeLinks, revenueLinks, learningLinks);
    }

    public async Task<OutcomeKnowledgeGraphDto?> GetOutcomeGraphAsync(
        Guid tenantId, Guid outcomeId, bool fromMemoryOutcome = true, CancellationToken cancellationToken = default)
    {
        BusinessMemoryOutcome? memOutcome = null;
        if (fromMemoryOutcome)
            memOutcome = await _db.BusinessMemoryOutcomes.AsNoTracking()
                .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == outcomeId, cancellationToken);

        var category = memOutcome?.OutcomeCategory ?? "outcome";
        var succeeded = memOutcome?.Succeeded ?? true;

        var edges = (await _graph.GetEdgesAsync(tenantId, 2000, cancellationToken))
            .Where(e => e.SourceId == outcomeId || e.TargetId == outcomeId)
            .ToList();

        var (nodes, dtoEdges) = Materialize(edges, 30);
        var trail = edges
            .Where(e => e.RelationType == KnowledgeGraphRelations.GeneratedRevenue)
            .Select(e => $"{e.SourceType} → {e.TargetType}")
            .ToList();

        return new OutcomeKnowledgeGraphDto(outcomeId, category, succeeded, nodes, dtoEdges, trail);
    }

    public async Task<RevenueKnowledgeGraphDto> GetRevenueGraphAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var edges = (await _graph.GetEdgesAsync(tenantId, 2000, cancellationToken))
            .Where(e => e.TargetType == KnowledgeGraphNodeTypes.Revenue ||
                        e.RelationType == KnowledgeGraphRelations.GeneratedRevenue)
            .ToList();

        var (nodes, dtoEdges) = Materialize(edges, 100);
        var total = edges.Sum(e => e.Weight);
        var chain = edges.Take(15)
            .Select(e => $"Revenue ← {e.SourceType} ({e.RelationType}) ← … ← Decision/Agent")
            .ToList();

        return new RevenueKnowledgeGraphDto(tenantId, total, nodes, dtoEdges, chain);
    }

    public async Task<GraphSearchResultDto> SearchGraphAsync(
        Guid tenantId, string query, int take = 40, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new GraphSearchResultDto(Array.Empty<GraphNodeDto>(), Array.Empty<GraphEdgeDto>(), query);

        var q = query.ToLowerInvariant();
        var edges = (await _graph.GetEdgesAsync(tenantId, 3000, cancellationToken))
            .Where(e => e.RelationType.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        e.SourceType.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        e.TargetType.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(take * 2)
            .ToList();

        var (nodes, dtoEdges) = Materialize(edges, take);
        return new GraphSearchResultDto(nodes, dtoEdges, query);
    }

    public async Task LinkMemoryToDecisionAsync(
        Guid tenantId, Guid memoryId, Guid decisionAuditId, CancellationToken cancellationToken = default)
    {
        await _graph.AddEdgeAsync(BusinessKnowledgeGraphEdge.Link(
            tenantId,
            KnowledgeGraphNodeTypes.Memory, memoryId,
            KnowledgeGraphNodeTypes.Decision, decisionAuditId,
            KnowledgeGraphRelations.SupportsDecision, 1m), cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<AiDecisionAudit>> LoadAuditsForCustomer(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken)
        => await _db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CustomerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

    private static GraphExplorationDto BuildCustomerExploration(
        Customer? customer, List<BusinessKnowledgeGraphEdge> edges, List<AiDecisionAudit> audits)
    {
        var won = edges.Any(e => e.RelationType == KnowledgeGraphRelations.GeneratedRevenue && e.Weight >= 0.8m);
        var churn = edges.Any(e => e.RelationType == KnowledgeGraphRelations.AtRisk);
        var agents = edges.Where(e => e.SourceType == KnowledgeGraphNodeTypes.Agent).Select(e => e.SourceId.ToString()).Distinct().Count();
        var campaigns = edges.Count(e => e.TargetType == KnowledgeGraphNodeTypes.Campaign);

        var answers = new List<GraphExplorationAnswerDto>
        {
            new("¿Por qué este cliente renovó?",
                won ? "El grafo muestra outcomes positivos y señales de revenue tras decisiones/playbooks." : "No hay cadena de revenue suficiente; revisar outcomes y deals won.",
                edges.Where(e => e.RelationType == KnowledgeGraphRelations.GeneratedRevenue).Take(3).Select(e => e.Id.ToString()).ToList()),
            new("¿Por qué este cliente canceló?",
                churn ? "Nodos AT_RISK y outcomes negativos vinculados al cliente." : "Sin señal fuerte de churn en el grafo actual.",
                edges.Where(e => e.RelationType == KnowledgeGraphRelations.AtRisk).Take(3).Select(e => e.Id.ToString()).ToList()),
            new("¿Qué agentes influyeron?",
                agents > 0 ? $"{agents} agente(s) con aristas EXECUTED_DECISION / INFLUENCED_BY_AGENT." : "Sin agentes enlazados; ejecutar BuildGraph.",
                audits.Where(a => !string.IsNullOrWhiteSpace(a.AgentName)).Select(a => a.AgentName!).Take(5).ToList()),
            new("¿Qué campañas funcionaron?",
                campaigns > 0 ? $"{campaigns} playbook/campaña(s) RAN_CAMPAIGN en el grafo." : "Sin campañas registradas.",
                edges.Where(e => e.TargetType == KnowledgeGraphNodeTypes.Campaign).Take(3).Select(e => e.TargetId.ToString()).ToList()),
            new("¿Qué decisiones generaron revenue?",
                SummarizeRevenueDecisions(edges),
                edges.Where(e => e.SourceType == KnowledgeGraphNodeTypes.Decision && e.TargetType == KnowledgeGraphNodeTypes.Revenue).Take(5).Select(e => e.SourceId.ToString()).ToList())
        };

        return new GraphExplorationDto(answers);
    }

    private static string SummarizeRevenueDecisions(List<BusinessKnowledgeGraphEdge> edges)
    {
        var count = edges.Count(e =>
            e.SourceType == KnowledgeGraphNodeTypes.Decision &&
            (e.TargetType == KnowledgeGraphNodeTypes.Revenue || e.RelationType == KnowledgeGraphRelations.GeneratedRevenue));
        return count > 0
            ? $"{count} arista(s) Decision→Revenue/Outcome en el grafo."
            : "Ejecute BuildGraph o registre outcomes de negocio en Trust.";
    }

    private static (IReadOnlyList<GraphNodeDto> Nodes, IReadOnlyList<GraphEdgeDto> Edges) Materialize(
        List<BusinessKnowledgeGraphEdge> edges, int maxNodes)
    {
        var nodeMap = new Dictionary<string, GraphNodeDto>();
        foreach (var e in edges)
        {
            AddNode(nodeMap, e.SourceType, e.SourceId, e.Weight);
            AddNode(nodeMap, e.TargetType, e.TargetId, e.Weight);
        }

        var nodes = nodeMap.Values.Take(maxNodes).ToList();
        var dtoEdges = edges.Take(maxNodes * 2).Select(e => new GraphEdgeDto(
            e.SourceType, e.SourceId, e.TargetType, e.TargetId, e.RelationType, e.Weight)).ToList();
        return (nodes, dtoEdges);
    }

    private static void AddNode(Dictionary<string, GraphNodeDto> map, string type, Guid id, decimal weight)
    {
        var key = $"{type}:{id}";
        if (!map.ContainsKey(key))
            map[key] = new GraphNodeDto(type, id, $"{type.Replace("Node", "")} {id.ToString()[..8]}", weight);
    }

    private static string MapSubjectToNode(string subjectType) => subjectType switch
    {
        BusinessMemoryConstants.SubjectCustomer => KnowledgeGraphNodeTypes.Customer,
        BusinessMemoryConstants.SubjectDeal => KnowledgeGraphNodeTypes.Deal,
        BusinessMemoryConstants.SubjectAgent => KnowledgeGraphNodeTypes.Agent,
        _ => KnowledgeGraphNodeTypes.Memory
    };

    private static Guid DeterministicId(string key)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }

    private sealed class GraphBuilder
    {
        private readonly Guid _tenantId;
        private readonly IKnowledgeGraphRepository _repo;
        private readonly HashSet<string> _keys = new();

        public int CreatedCount { get; private set; }

        public GraphBuilder(Guid tenantId, IKnowledgeGraphRepository repo)
        {
            _tenantId = tenantId;
            _repo = repo;
        }

        public async Task LinkAsync(
            string fromType, Guid fromId, string toType, Guid toId, string relation, decimal weight,
            CancellationToken cancellationToken)
        {
            var key = $"{fromType}:{fromId}:{toType}:{toId}:{relation}";
            if (!_keys.Add(key)) return;

            await _repo.AddEdgeAsync(BusinessKnowledgeGraphEdge.Link(
                _tenantId, fromType, fromId, toType, toId, relation, weight), cancellationToken);
            CreatedCount++;
        }
    }
}
