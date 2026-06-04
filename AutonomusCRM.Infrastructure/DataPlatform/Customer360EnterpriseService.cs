using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Application.Voice;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataPlatform;

public sealed class Customer360EnterpriseService : ICustomer360EnterpriseService
{
    private readonly ICustomer360Service _c360;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IChurnPredictionV2 _churn;
    private readonly IKnowledgeGraphService _knowledgeGraph;
    private readonly ICustomerSuccessOsService _csOs;

    public Customer360EnterpriseService(
        ICustomer360Service c360,
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IChurnPredictionV2 churn,
        IKnowledgeGraphService knowledgeGraph,
        ICustomerSuccessOsService csOs)
    {
        _c360 = c360;
        _dbFactory = dbFactory;
        _churn = churn;
        _knowledgeGraph = knowledgeGraph;
        _csOs = csOs;
    }

    public async Task<Customer360EnterpriseDto?> GetEnterpriseViewAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var profile = await _c360.GetAsync(tenantId, customerId, cancellationToken);
        if (profile == null) return null;

        var customerTask = LoadCustomerAsync(tenantId, customerId, cancellationToken);
        var dealsTask = LoadDealsAsync(tenantId, customerId, cancellationToken);
        var auditsTask = LoadAuditsAsync(tenantId, customerId, cancellationToken);
        var playbooksTask = LoadPlaybooksAsync(tenantId, customerId, cancellationToken);
        var approvalsTask = LoadApprovalsAsync(tenantId, customerId, cancellationToken);
        var commLogsTask = LoadCommLogsAsync(tenantId, customerId, cancellationToken);
        var voiceCallsTask = LoadVoiceCallsAsync(tenantId, customerId, cancellationToken);
        var churnTask = _churn.PredictAsync(tenantId, customerId, cancellationToken);
        var kgTask = LoadKnowledgeGraphSafeAsync(tenantId, customerId, cancellationToken);
        var csTask = _csOs.GetCustomerPanelAsync(tenantId, customerId, cancellationToken);

        await Task.WhenAll(
            customerTask, dealsTask, auditsTask, playbooksTask, approvalsTask,
            commLogsTask, voiceCallsTask, churnTask, kgTask, csTask);

        var customer = await customerTask;
        var deals = await dealsTask;
        var audits = await auditsTask;
        var playbooks = await playbooksTask;
        var approvals = await approvalsTask;
        var commLogs = await commLogsTask;
        var voiceCalls = await voiceCallsTask;
        var churnList = await churnTask;
        var kg = await kgTask;
        var csPanel = await csTask;

        var timeline = BuildTimeline(customer, deals, audits, playbooks, approvals, commLogs, voiceCalls);
        var churn = churnList.FirstOrDefault();
        var health = new CustomerHealthCenterDto(
            ComputeHealth(profile, churn?.ChurnProbability),
            profile.ChurnRisk ?? churn?.ChurnProbability,
            profile.OpenPipeline > profile.WonRevenue ? 70 : 40,
            profile.UsageEvents30d,
            churn != null && churn.ChurnProbability >= 60 ? "Alto" : churn != null && churn.ChurnProbability >= 35 ? "Medio" : "Bajo");

        var journey = BuildJourney(customer, deals, profile);
        var summary = BuildSummary(profile, churn, audits);
        var comms = timeline.Where(t => t.Category is "Comms" or "Voice").OrderByDescending(t => t.OccurredAt).ToList();
        var (nodes, edges) = BuildRelationshipGraph(profile, customer, deals, churn);

        return new Customer360EnterpriseDto(profile, timeline, health, journey, summary, comms, nodes, edges, kg, csPanel);
    }

    private async Task<Customer?> LoadCustomerAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId, ct);
    }

    private async Task<List<Deal>> LoadDealsAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Deals.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.CustomerId == customerId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(20)
            .ToListAsync(ct);
    }

    private async Task<List<AiDecisionAudit>> LoadAuditsAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CustomerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(15)
            .ToListAsync(ct);
    }

    private async Task<List<AutonomousPlaybookState>> LoadPlaybooksAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AutonomousPlaybookStates.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.CustomerId == customerId)
            .Take(10)
            .ToListAsync(ct);
    }

    private async Task<List<AiApprovalRequest>> LoadApprovalsAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var auditIds = db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CustomerId == customerId)
            .Select(a => a.Id);
        return await db.AiApprovalRequests.AsNoTracking()
            .Where(ap => ap.TenantId == tenantId && auditIds.Contains(ap.AuditId))
            .OrderByDescending(ap => ap.CreatedAt)
            .Take(10)
            .ToListAsync(ct);
    }

    private async Task<List<CustomerCommunicationLog>> LoadCommLogsAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.CustomerCommunicationLogs.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.CustomerId == customerId)
            .OrderByDescending(c => c.SentAt ?? c.CreatedAt)
            .Take(25)
            .ToListAsync(ct);
    }

    private async Task<List<VoiceCallLog>> LoadVoiceCallsAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.VoiceCallLogs.AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.CustomerId == customerId)
            .OrderByDescending(v => v.StartedAt)
            .Take(15)
            .ToListAsync(ct);
    }

    private async Task<CustomerKnowledgeGraphDto?> LoadKnowledgeGraphSafeAsync(
        Guid tenantId, Guid customerId, CancellationToken ct)
    {
        try
        {
            return await _knowledgeGraph.GetCustomerGraphAsync(tenantId, customerId, ct);
        }
        catch
        {
            return null;
        }
    }

    private static List<CustomerTimelineEventDto> BuildTimeline(
        Customer? customer,
        List<Deal> deals,
        List<AiDecisionAudit> audits,
        List<AutonomousPlaybookState> playbooks,
        List<AiApprovalRequest> approvals,
        List<CustomerCommunicationLog> commLogs,
        List<VoiceCallLog> voiceCalls)
    {
        var timeline = new List<CustomerTimelineEventDto>();

        if (customer != null)
            timeline.Add(new(customer.CreatedAt, "CRM", "Cliente creado", customer.Name, "info"));

        foreach (var d in deals)
            timeline.Add(new(d.CreatedAt, "Revenue", $"Deal: {d.Title}", $"{d.Stage} · ${d.Amount:N0}", d.Stage == DealStage.ClosedLost ? "danger" : "info"));

        foreach (var a in audits)
            timeline.Add(new(a.CreatedAt, "Trust", a.DecisionType, a.Action, a.DecisionScore >= 85 ? "warning" : "info"));

        foreach (var p in playbooks)
            timeline.Add(new(p.CreatedAt, "Playbook", p.PlaybookType, $"Paso {p.CurrentStepIndex}/{p.TotalSteps} · {p.Status}", "info"));

        foreach (var ap in approvals)
            timeline.Add(new(ap.CreatedAt, "Trust", "Aprobación HITL", $"{ap.DecisionType} · {ap.Status}", ap.Status == "pending" ? "warning" : "info"));

        foreach (var log in commLogs)
        {
            var at = log.SentAt ?? log.CreatedAt;
            var channel = log.Channel switch
            {
                "email" or "Email" => "Email",
                "whatsapp" or "WhatsApp" => "WhatsApp",
                _ => log.Channel
            };
            timeline.Add(new(at, "Comms", $"{channel}: {log.EventType}", $"{log.Status} · {log.Recipient}", log.Status == "Failed" ? "danger" : "info"));
        }

        foreach (var call in voiceCalls)
            timeline.Add(new(call.StartedAt, "Voice", $"Llamada {call.Direction}", $"{call.Outcome} · {call.DurationSeconds}s", "info"));

        return timeline.OrderByDescending(t => t.OccurredAt).Take(50).ToList();
    }

    private static (List<RelationshipNodeDto>, List<RelationshipEdgeDto>) BuildRelationshipGraph(
        Customer360Dto profile,
        Customer? customer,
        List<Deal> deals,
        ChurnPredictionV2Dto? churn)
    {
        var nodes = new List<RelationshipNodeDto>();
        var edges = new List<RelationshipEdgeDto>();
        var hubId = profile.CustomerId.ToString();
        nodes.Add(new(hubId, profile.Name, "customer", profile.Email));

        foreach (var d in deals.Take(8))
        {
            var id = d.Id.ToString();
            var type = d.Stage == DealStage.ClosedWon ? "deal-won" : d.Stage == DealStage.ClosedLost ? "risk" : "deal-open";
            nodes.Add(new(id, d.Title, type, $"${d.Amount:N0}"));
            edges.Add(new(hubId, id, d.Stage.ToString()));
        }

        if (churn != null && churn.ChurnProbability >= 40)
        {
            var riskId = "risk-" + hubId;
            nodes.Add(new(riskId, $"Churn {churn.ChurnProbability:F0}%", "risk", churn.TrendDirection));
            edges.Add(new(hubId, riskId, "Riesgo"));
        }

        if (profile.OpenPipeline > profile.WonRevenue && profile.OpenPipeline > 0)
        {
            var expId = "exp-" + hubId;
            nodes.Add(new(expId, $"Expansión ${profile.OpenPipeline:N0}", "deal-open", "Pipeline"));
            edges.Add(new(hubId, expId, "Expansión"));
        }

        return (nodes, edges);
    }

    private static int ComputeHealth(Customer360Dto p, double? churnProb)
    {
        var baseScore = 70;
        if (p.WonRevenue > 0) baseScore += 10;
        if (p.UsageEvents30d > 5) baseScore += 10;
        if (churnProb >= 60) baseScore -= 30;
        else if (churnProb >= 35) baseScore -= 15;
        return Math.Clamp(baseScore, 0, 100);
    }

    private static List<CustomerJourneyStageDto> BuildJourney(
        Customer? customer,
        List<Deal> deals,
        Customer360Dto profile)
    {
        return
        [
            new("Lead", customer != null ? "completed" : "pending", customer?.CreatedAt),
            new("Customer", profile.WonRevenue > 0 || profile.OpenPipeline > 0 ? "completed" : "active", customer?.CreatedAt),
            new("Expansion", profile.OpenPipeline > profile.WonRevenue ? "active" : "pending", null),
            new("Renewal", deals.Any(d => d.Stage == DealStage.ClosedWon) ? "active" : "pending", null),
            new("Advocate", profile.WonRevenue > 10000 ? "active" : "pending", null)
        ];
    }

    private static List<string> BuildSummary(Customer360Dto p, ChurnPredictionV2Dto? churn, List<AiDecisionAudit> audits)
    {
        var bullets = new List<string>();
        if (churn != null && churn.ChurnProbability >= 50)
            bullets.Add($"Riesgo de churn elevado ({churn.ChurnProbability}%).");
        if (p.OpenPipeline > 0)
            bullets.Add($"Pipeline abierto ${p.OpenPipeline:N0}.");
        if (p.WonRevenue > 0)
            bullets.Add($"Ingresos ganados ${p.WonRevenue:N0}.");
        if (audits.Any(a => a.Status == AutonomousConstants.AuditPending))
            bullets.Add("Decisiones IA pendientes de supervisión.");
        if (bullets.Count == 0)
            bullets.Add("Sin alertas críticas; continuar monitoreo de uso y renovación.");
        return bullets;
    }
}
