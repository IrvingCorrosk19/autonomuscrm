using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.BusinessMemory;

public sealed class BusinessMemoryPipeline : IBusinessMemoryPipeline
{
    private readonly IBusinessMemoryRepository _repo;
    private readonly IAiDecisionAuditRepository _audits;
    private readonly IUnitOfWork _uow;
    private readonly ISemanticMemoryService _semantic;
    private readonly IKnowledgeGraphRepository _graphRepo;
    private readonly ILogger<BusinessMemoryPipeline> _logger;

    public BusinessMemoryPipeline(
        IBusinessMemoryRepository repo,
        IAiDecisionAuditRepository audits,
        IUnitOfWork uow,
        ISemanticMemoryService semantic,
        IKnowledgeGraphRepository graphRepo,
        ILogger<BusinessMemoryPipeline> logger)
    {
        _repo = repo;
        _audits = audits;
        _uow = uow;
        _semantic = semantic;
        _graphRepo = graphRepo;
        _logger = logger;
    }

    public async Task CaptureFromDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId is not Guid tenantId)
            return;

        var mapped = MapEvent(domainEvent);
        if (mapped is null)
            return;

        var episodeKey = $"{domainEvent.EventType}:{domainEvent.Id}";
        var existing = await _repo.GetByEpisodeKeyAsync(tenantId, episodeKey, cancellationToken);
        if (existing is not null)
            return;

        var memory = BusinessMemoryRoot.CreateEpisode(
            tenantId,
            mapped.SubjectType,
            mapped.SubjectId,
            episodeKey,
            mapped.Title,
            mapped.Summary,
            mapped.Importance,
            tags: mapped.Tags);

        await _repo.AddMemoryAsync(memory, cancellationToken);

        var memEvent = BusinessMemoryEvent.FromDomain(
            memory.Id,
            tenantId,
            domainEvent.EventType,
            mapped.Why,
            domainEvent.Id,
            domainEvent.OccurredOn,
            mapped.Payload);
        await _repo.AddEventAsync(memEvent, cancellationToken);

        await _repo.AddContextAsync(
            BusinessMemoryContext.Capture(memory.Id, tenantId, "event", mapped.Payload),
            cancellationToken);

        foreach (var (key, value) in mapped.Facts)
        {
            await _repo.AddFactAsync(
                BusinessMemoryFact.Create(memory.Id, tenantId, key, value),
                cancellationToken);
        }

        if (mapped.Outcome is { } outcome)
        {
            await _repo.AddOutcomeAsync(
                BusinessMemoryOutcome.Record(
                    memory.Id, tenantId, outcome.Category, outcome.Succeeded, outcome.Narrative,
                    outcome.RevenueDelta, outcome.CustomerImpact, outcome.TrustImpact),
                cancellationToken);

            await ApplyLearningAsync(tenantId, mapped.LearningKey, mapped.LearningAction, outcome.Succeeded, outcome.Narrative, cancellationToken);

            if (outcome.Succeeded && outcome.RevenueDelta > 0)
            {
                await _repo.AddInsightAsync(
                    BusinessMemoryInsight.Create(
                        tenantId, "revenue_pattern",
                        $"Estrategia '{mapped.LearningAction}' generó impacto positivo ({outcome.RevenueDelta:C}).",
                        0.75,
                        mapped.SubjectType == BusinessMemoryConstants.SubjectCustomer ? mapped.SubjectId : null,
                        memory.Id),
                    cancellationToken);
            }
        }

        if (mapped.Relationships is { Count: > 0 })
        {
            foreach (var rel in mapped.Relationships)
            {
                await _repo.AddRelationshipAsync(
                    BusinessMemoryRelationship.Link(
                        tenantId, rel.FromType, rel.FromId, rel.ToType, rel.ToId, rel.RelationType, memory.Id),
                    cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        try
        {
            await _semantic.StoreMemoryAsync(
                tenantId,
                SemanticMemoryConstants.SourceEpisode,
                memory.Id,
                $"{memory.Title}. {memory.Summary}",
                memory.Importance / 10.0,
                cancellationToken);

            if (mapped.Outcome is { } oc)
            {
                await _semantic.StoreMemoryAsync(
                    tenantId,
                    SemanticMemoryConstants.SourceOutcome,
                    memory.Id,
                    $"{oc.Category} succeeded={oc.Succeeded}: {oc.Narrative}",
                    oc.Succeeded ? 0.85 : 0.4,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic index skipped for episode {EpisodeKey}", episodeKey);
        }

        try
        {
            if (mapped.SubjectType == BusinessMemoryConstants.SubjectCustomer)
            {
                await _graphRepo.AddEdgeAsync(BusinessKnowledgeGraphEdge.Link(
                    tenantId,
                    KnowledgeGraphNodeTypes.Memory, memory.Id,
                    KnowledgeGraphNodeTypes.Customer, mapped.SubjectId,
                    KnowledgeGraphRelations.LinkedToMemory,
                    memory.Importance / 10m), cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Knowledge graph link skipped for episode {EpisodeKey}", episodeKey);
        }

        _logger.LogInformation("Business memory captured {EpisodeKey} tenant {TenantId}", episodeKey, tenantId);
    }

    public async Task CaptureFromDecisionAuditAsync(Guid auditId, CancellationToken cancellationToken = default)
    {
        var audit = await _audits.GetByIdAsync(auditId, cancellationToken);
        if (audit is null)
            return;

        var existing = await _repo.GetDecisionByAuditAsync(audit.TenantId, auditId, cancellationToken);
        if (existing is not null)
            return;

        var episodeKey = $"decision:{auditId}";
        var subjectType = audit.CustomerId.HasValue
            ? BusinessMemoryConstants.SubjectCustomer
            : audit.DealId.HasValue ? BusinessMemoryConstants.SubjectDeal : BusinessMemoryConstants.SubjectTenant;
        var subjectId = audit.CustomerId ?? audit.DealId ?? audit.TenantId;

        var memory = BusinessMemoryRoot.CreateEpisode(
            audit.TenantId,
            subjectType,
            subjectId,
            episodeKey,
            $"Decisión IA: {audit.Action}",
            audit.Reason,
            Math.Clamp(audit.DecisionScore / 10, 1, 10),
            "ai_decision",
            new[] { audit.DecisionType, audit.AgentName ?? "agent" }.Where(s => !string.IsNullOrWhiteSpace(s)));

        await _repo.AddMemoryAsync(memory, cancellationToken);

        var ctx = new Dictionary<string, object>
        {
            ["decisionType"] = audit.DecisionType,
            ["action"] = audit.Action,
            ["score"] = audit.DecisionScore,
            ["status"] = audit.Status
        };
        foreach (var kv in audit.Evidence)
            ctx[$"evidence.{kv.Key}"] = kv.Value;

        await _repo.AddContextAsync(
            BusinessMemoryContext.Capture(memory.Id, audit.TenantId, "decision", ctx),
            cancellationToken);

        var decision = BusinessMemoryDecision.FromAudit(
            memory.Id, audit.TenantId, auditId, audit.DecisionType, audit.Action, audit.Reason, audit.DecisionScore, audit.Evidence);
        if (audit.BusinessSucceeded.HasValue)
            decision.SetOutcome(audit.BusinessSucceeded.Value);

        await _repo.AddDecisionAsync(decision, cancellationToken);

        if (!string.IsNullOrWhiteSpace(audit.AgentName))
        {
            await _repo.AddRelationshipAsync(
                BusinessMemoryRelationship.Link(
                    audit.TenantId,
                    BusinessMemoryConstants.SubjectAgent,
                    audit.Id,
                    subjectType,
                    subjectId,
                    BusinessMemoryConstants.RelationInvolves,
                    memory.Id),
                cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        try
        {
            await _semantic.StoreMemoryAsync(
                audit.TenantId,
                SemanticMemoryConstants.SourceDecision,
                decision.Id,
                $"{audit.DecisionType} {audit.Action}: {audit.Reason}",
                audit.BusinessSucceeded == true ? 0.9 : 0.6,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic index skipped for decision audit {AuditId}", auditId);
        }
    }

    private async Task ApplyLearningAsync(
        Guid tenantId, string? strategyKey, string? action, bool success, string outcomeLabel,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(strategyKey) || string.IsNullOrWhiteSpace(action))
            return;

        var learning = await _repo.GetLearningAsync(tenantId, strategyKey, cancellationToken);
        if (learning is null)
        {
            learning = BusinessMemoryLearning.Start(tenantId, strategyKey, action);
            await _repo.AddLearningAsync(learning, cancellationToken);
        }

        learning.ApplyOutcome(success, outcomeLabel);
        await _repo.UpdateLearningAsync(learning, cancellationToken);
    }

    private static EventMemoryMap? MapEvent(IDomainEvent e) => e switch
    {
        DealClosedEvent closed => new(
            BusinessMemoryConstants.SubjectDeal,
            closed.DealId,
            "Deal ganado",
            $"Deal cerrado exitosamente por {closed.FinalAmount:C}.",
            "Pipeline cerró en etapa Won tras negociación.",
            9,
            Payload(e, ("dealId", closed.DealId), ("amount", closed.FinalAmount)),
            new[] { ("deal.amount", closed.FinalAmount.ToString("F2")) },
            new OutcomeMap("revenue", true, $"Won {closed.FinalAmount:C}", closed.FinalAmount, 10, 5),
            "deal.won",
            "close_won",
            new[] { new RelMap(BusinessMemoryConstants.SubjectDeal, closed.DealId, BusinessMemoryConstants.SubjectTenant, closed.TenantId!.Value, BusinessMemoryConstants.RelationResultedIn) },
            new[] { "revenue", "deal" }),

        DealLostEvent lost => new(
            BusinessMemoryConstants.SubjectDeal,
            lost.DealId,
            "Deal perdido",
            $"Deal perdido: {lost.Reason ?? "sin razón"}.",
            "Oportunidad no convertida; registrar para aprendizaje.",
            7,
            Payload(e, ("dealId", lost.DealId), ("reason", lost.Reason ?? "")),
            new[] { ("deal.lost", "true"), ("deal.reason", lost.Reason ?? "") },
            new OutcomeMap("revenue", false, lost.Reason ?? "lost", 0, -5, 0),
            "deal.lost",
            "pipeline_review",
            null,
            new[] { "revenue", "loss" }),

        LeadCreatedEvent lead => new(
            BusinessMemoryConstants.SubjectLead,
            lead.LeadId,
            "Lead creado",
            $"Nuevo lead en el pipeline.",
            "Entrada al funnel de adquisición.",
            4,
            Payload(e, ("leadId", lead.LeadId)),
            Array.Empty<(string, string)>(),
            null,
            null,
            null,
            null,
            new[] { "lead" }),

        CustomerCreatedEvent cust => new(
            BusinessMemoryConstants.SubjectCustomer,
            cust.CustomerId,
            "Cliente creado",
            "Nuevo cliente en la base.",
            "Onboarding y expansión posibles.",
            6,
            Payload(e, ("customerId", cust.CustomerId)),
            new[] { ("customer.new", "true") },
            new OutcomeMap("customer", true, "Customer created", 0, 5, 0),
            "customer.acquired",
            "onboard",
            null,
            new[] { "customer" }),

        CustomerStatusChangedEvent status => new(
            BusinessMemoryConstants.SubjectCustomer,
            status.CustomerId,
            "Estado cliente cambió",
            $"Nuevo estado: {status.NewStatus}.",
            "Cambio de ciclo de vida del cliente.",
            6,
            Payload(e, ("customerId", status.CustomerId), ("status", status.NewStatus.ToString())),
            new[] { ("customer.status", status.NewStatus.ToString()) },
            status.NewStatus.ToString().Contains("Churn", StringComparison.OrdinalIgnoreCase)
                ? new OutcomeMap("retention", false, "Churn risk", 0, -10, -3)
                : new OutcomeMap("retention", true, "Status improved", 0, 3, 1),
            $"customer.status.{status.NewStatus}",
            "lifecycle",
            null,
            new[] { "customer", "retention" }),

        _ => null
    };

    private static Dictionary<string, object> Payload(IDomainEvent e, params (string key, object value)[] extra)
    {
        var d = new Dictionary<string, object>
        {
            ["eventType"] = e.EventType,
            ["occurredOn"] = e.OccurredOn,
            ["correlationId"] = e.CorrelationId?.ToString() ?? ""
        };
        foreach (var (key, value) in extra)
            d[key] = value;
        return d;
    }

    private sealed record OutcomeMap(
        string Category, bool Succeeded, string Narrative,
        decimal RevenueDelta, int CustomerImpact, int TrustImpact);

    private sealed record RelMap(string FromType, Guid FromId, string ToType, Guid ToId, string RelationType);

    private sealed record EventMemoryMap(
        string SubjectType,
        Guid SubjectId,
        string Title,
        string Summary,
        string Why,
        int Importance,
        Dictionary<string, object> Payload,
        IEnumerable<(string Key, string Value)> Facts,
        OutcomeMap? Outcome,
        string? LearningKey,
        string? LearningAction,
        IReadOnlyList<RelMap>? Relationships,
        string[] Tags);
}
