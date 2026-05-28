using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class BusinessKnowledgeGraphService : IBusinessKnowledgeGraphService
{
    private readonly IBusinessKnowledgeGraphEdgeRepository _edges;
    private readonly ICustomerRepository _customers;
    private readonly IDealRepository _deals;
    private readonly IChurnPredictionModel _churn;
    private readonly IExpansionPredictionModel _expansion;
    private readonly IUnitOfWork _unitOfWork;

    public BusinessKnowledgeGraphService(
        IBusinessKnowledgeGraphEdgeRepository edges,
        ICustomerRepository customers,
        IDealRepository deals,
        IChurnPredictionModel churn,
        IExpansionPredictionModel expansion,
        IUnitOfWork unitOfWork)
    {
        _edges = edges;
        _customers = customers;
        _deals = deals;
        _churn = churn;
        _expansion = expansion;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> RebuildGraphAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var existing = await _edges.GetByTenantAsync(tenantId, 5000, cancellationToken);
        foreach (var e in existing)
            await _edges.DeleteAsync(e, cancellationToken);

        var created = 0;
        var customers = (await _customers.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP).Take(100);

        var churnPreds = await _churn.PredictAsync(tenantId, cancellationToken: cancellationToken);
        var expansionPreds = await _expansion.PredictAsync(tenantId, cancellationToken: cancellationToken);
        var deals = (await _deals.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(d => d.Status == DealStatus.Open).Take(100);

        foreach (var c in customers)
        {
            var churn = churnPreds.FirstOrDefault(p => p.CustomerId == c.Id);
            if (churn != null && churn.ChurnProbabilityPercent >= 50)
            {
                await _edges.AddAsync(BusinessKnowledgeGraphEdge.Link(
                    tenantId, EnterpriseAiConstants.GraphCustomer, c.Id,
                    EnterpriseAiConstants.GraphChurnRisk, c.Id, "at_risk",
                    churn.ChurnProbabilityPercent / 100m), cancellationToken);
                created++;
            }

            var exp = expansionPreds.FirstOrDefault(p => p.CustomerId == c.Id);
            if (exp != null && exp.ExpansionProbabilityPercent >= 50)
            {
                await _edges.AddAsync(BusinessKnowledgeGraphEdge.Link(
                    tenantId, EnterpriseAiConstants.GraphCustomer, c.Id,
                    EnterpriseAiConstants.GraphExpansion, c.Id, exp.OpportunityType,
                    exp.ExpansionProbabilityPercent / 100m), cancellationToken);
                created++;
            }
        }

        foreach (var d in deals)
        {
            await _edges.AddAsync(BusinessKnowledgeGraphEdge.Link(
                tenantId, EnterpriseAiConstants.GraphCustomer, d.CustomerId,
                EnterpriseAiConstants.GraphDeal, d.Id, "has_opportunity", d.Amount / 10000m), cancellationToken);
            created++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<KnowledgeGraphDto> GetGraphAsync(
        Guid tenantId, int maxNodes = 100, CancellationToken cancellationToken = default)
    {
        var edgeList = (await _edges.GetByTenantAsync(tenantId, 2000, cancellationToken)).Take(maxNodes * 2).ToList();
        var nodes = new Dictionary<string, KnowledgeGraphNodeDto>();

        foreach (var e in edgeList)
        {
            AddNode(nodes, e.SourceType, e.SourceId);
            AddNode(nodes, e.TargetType, e.TargetId);
        }

        var dtoEdges = edgeList.Select(e => new KnowledgeGraphEdgeDto(
            e.SourceType, e.SourceId, e.TargetType, e.TargetId, e.RelationType, e.Weight)).ToList();

        return new KnowledgeGraphDto(nodes.Values.Take(maxNodes).ToList(), dtoEdges);
    }

    private static void AddNode(Dictionary<string, KnowledgeGraphNodeDto> nodes, string type, Guid id)
    {
        var key = $"{type}:{id}";
        if (!nodes.ContainsKey(key))
            nodes[key] = new KnowledgeGraphNodeDto(type, id, $"{type} {id:N}", 1m);
    }
}
