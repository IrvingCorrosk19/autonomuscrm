using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Persistence.Seed;

/// <summary>
/// Tenant CEO_DEMO — datos ejecutivos listos para demo comercial (sin motores nuevos).
/// </summary>
public static class CeoDemoSeeder
{
    public const string TenantName = "CEO_DEMO";
    private const string RevenueKey = "outcomeFabric.revenueImpact";

    public static async Task EnsureCeoDemoTenantAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == TenantIds.CeoDemo, cancellationToken);
        if (tenant is null)
        {
            tenant = Tenant.CreateWithId(TenantIds.CeoDemo, TenantName, "Demo ejecutivo AutonomusFlow — revenue, trust, memory");
            await db.Tenants.AddAsync(tenant, cancellationToken);
            logger.LogInformation("Created {TenantName} tenant {TenantId}", TenantName, TenantIds.CeoDemo);
        }

        if (await db.Customers.CountAsync(c => c.TenantId == TenantIds.CeoDemo, cancellationToken) >= 50)
        {
            await EnsureDemoUsersAsync(db, logger, cancellationToken);
            await EnsureCsOsDemoDataAsync(db, logger, cancellationToken);
            await EnsureAbosLearningDemoDataAsync(db, logger, cancellationToken);
            return;
        }

        logger.LogInformation("Seeding {TenantName} executive demo dataset…", TenantName);

        await EnsureDemoUsersAsync(db, logger, cancellationToken);

        var customers = new List<Customer>();
        for (var i = 1; i <= 50; i++)
        {
            var c = Customer.Create(
                TenantIds.CeoDemo,
                $"Cliente Demo {i:D2}",
                $"cliente{i}@ceo-demo.local",
                $"+507600{i:D5}",
                $"Empresa Demo {i}");
            c.ChangeStatus(i <= 6 ? CustomerStatus.VIP : CustomerStatus.Customer);
            if (i <= 10)
            {
                c.UpdateRiskScore(72 + i);
                c.RecordContact(DateTime.UtcNow.AddDays(-90 - i));
            }
            else
            {
                c.RecordContact(DateTime.UtcNow.AddDays(-7));
            }
            if (i <= 6)
                c.UpdateMetadata("ProductLine", "Core,Analytics,Automation");
            customers.Add(c);
        }
        await db.Customers.AddRangeAsync(customers, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var rnd = new Random(42);
        var deals = new List<Deal>();
        var stages = new[] { DealStage.Prospecting, DealStage.Qualification, DealStage.Proposal, DealStage.Negotiation };

        for (var i = 0; i < 25; i++)
        {
            var customer = customers[i % customers.Count];
            var deal = Deal.Create(
                TenantIds.CeoDemo,
                customer.Id,
                $"Oportunidad {i + 1} — {customer.Name}",
                15_000m + i * 2_500m,
                "Pipeline demo");
            deal.UpdateStage(stages[i % stages.Length], 35 + (i % 4) * 15);
            deal.SetExpectedCloseDate(DateTime.UtcNow.AddDays(15 + i * 3));
            deals.Add(deal);
        }

        for (var i = 0; i < 15; i++)
        {
            var customer = customers[10 + i];
            var deal = Deal.Create(
                TenantIds.CeoDemo,
                customer.Id,
                $"Ganado — {customer.Name}",
                40_000m + i * 5_000m,
                "Deal cerrado demo");
            deal.UpdateStage(DealStage.Negotiation, 90);
            deal.Close(DateTime.UtcNow.AddDays(-30 + i), deal.Amount);
            deals.Add(deal);
        }

        for (var i = 0; i < 5; i++)
        {
            var customer = customers[30 + i];
            var deal = Deal.Create(
                TenantIds.CeoDemo,
                customer.Id,
                $"Perdido — {customer.Name}",
                20_000m + i * 3_000m,
                "Deal perdido demo");
            deal.UpdateStage(DealStage.Proposal, 40);
            deal.Lose("Presupuesto", "Price", DealStage.Proposal);
            deals.Add(deal);
        }

        await db.Deals.AddRangeAsync(deals, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var audits = new List<AiDecisionAudit>();
        var approvalSpecs = new (string Type, string Action, string Agent, decimal Rev, bool Protect)[]
        {
            ("ChurnRescue", "PreventChurn", "Churn Agent", 85_000m, true),
            ("Renewal", "RenewContract", "Renewal Agent", 52_000m, false),
            ("Expansion", "UpsellPremium", "Expansion Agent", 38_000m, false),
            ("DealStrategy", "AccelerateClose", "Sales Agent", 25_000m, false),
            ("CustomerRisk", "ExecutiveOutreach", "Customer Agent", 0m, true),
            ("ReEngagement", "WinBack", "Sales Agent", 18_000m, false),
            ("Renewal", "EarlyRenewal", "Renewal Agent", 44_000m, false),
            ("Expansion", "CrossSellModule", "Expansion Agent", 31_000m, false),
            ("ChurnRescue", "RescueAccount", "Churn Agent", 62_000m, true),
            ("Automation", "OptimizeWorkflow", "Operations Agent", 12_000m, false),
            ("DealStrategy", "CloseDeal", "Sales Agent", 55_000m, false),
            ("Renewal", "RenewVIP", "Renewal Agent", 72_000m, false),
        };

        for (var i = 0; i < approvalSpecs.Length; i++)
        {
            var spec = approvalSpecs[i];
            var customer = customers[i % customers.Count];
            var evidence = new Dictionary<string, object>();
            if (spec.Rev > 0)
                evidence[RevenueKey] = spec.Rev;

            var audit = AiDecisionAudit.Create(
                TenantIds.CeoDemo,
                spec.Type,
                spec.Action,
                75 + (i % 20),
                $"Demo decision {i + 1} — {spec.Action}",
                evidence,
                customer.Id,
                agentName: spec.Agent);

            if (i >= 8)
            {
                audit.MarkExecuted("Demo executed");
                audit.MarkBusinessOutcome(true, spec.Protect ? "Revenue protected" : "Revenue generated");
            }

            audits.Add(audit);
        }

        await db.AiDecisionAudits.AddRangeAsync(audits, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var audit in audits.Take(8))
        {
            var approval = AiApprovalRequest.Create(
                TenantIds.CeoDemo,
                audit.Id,
                audit.DecisionType,
                audit.Action,
                audit.Reason);
            await db.AiApprovalRequests.AddAsync(approval, cancellationToken);
        }

        for (var i = 0; i < 8; i++)
        {
            var won = deals.Where(d => d.Stage == DealStage.ClosedWon).ElementAt(i);
            var contract = CustomerContract.Create(
                TenantIds.CeoDemo,
                won.CustomerId,
                won.Id,
                DateTime.UtcNow.AddMonths(-10),
                48_000m + i * 6_000m,
                12);
            contract.MarkPendingRenewal();
            await db.CustomerContracts.AddAsync(contract, cancellationToken);
        }

        for (var i = 0; i < 30; i++)
        {
            var customer = customers[i % customers.Count];
            var root = BusinessMemoryRoot.CreateEpisode(
                TenantIds.CeoDemo,
                "Customer",
                customer.Id,
                $"ceo-demo-ep-{i}",
                $"Episodio demo {i + 1}",
                $"Memoria empresarial: interacción con {customer.Name} — outcome registrado.",
                importance: 4 + (i % 6),
                tags: new[] { "demo", "ceo" });
            await db.BusinessMemoryRoots.AddAsync(root, cancellationToken);

            var evt = BusinessMemoryEvent.FromDomain(
                root.Id,
                TenantIds.CeoDemo,
                "customer.interaction",
                $"Evento demo {i + 1} para {customer.Name}",
                null,
                DateTime.UtcNow.AddDays(-i),
                new Dictionary<string, object> { ["demo"] = true });
            await db.BusinessMemoryEvents.AddAsync(evt, cancellationToken);

            var vector = Enumerable.Range(0, 8).Select(j => (float)(rnd.NextDouble() * 0.5)).ToArray();
            var embedding = MemoryEmbedding.Create(
                TenantIds.CeoDemo,
                i % 3 == 0 ? SemanticMemoryConstants.SourceDecision : SemanticMemoryConstants.SourceEpisode,
                root.Id,
                $"Memoria semántica demo {i + 1}: {customer.Name} success playbook",
                vector,
                "demo-hash",
                0.85);
            embedding.SetScores(0.7 + (i % 5) * 0.05, 0.8);
            await db.MemoryEmbeddings.AddAsync(embedding, cancellationToken);
        }

        for (var i = 0; i < 4; i++)
        {
            var learning = BusinessMemoryLearning.Start(
                TenantIds.CeoDemo,
                $"strategy.demo.{i}",
                i % 2 == 0 ? "RenewContract" : "PreventChurn",
                new Dictionary<string, object> { ["segment"] = "enterprise" });
            for (var j = 0; j < 5; j++)
                learning.ApplyOutcome(j % 4 != 0, j % 4 != 0 ? "won" : "lost");
            await db.BusinessMemoryLearnings.AddAsync(learning, cancellationToken);
        }

        for (var i = 0; i < 40; i++)
        {
            var from = customers[i % customers.Count];
            var to = customers[(i + 7) % customers.Count];
            await db.BusinessKnowledgeGraphEdges.AddAsync(
                BusinessKnowledgeGraphEdge.Link(
                    TenantIds.CeoDemo,
                    "Customer", from.Id,
                    "Customer", to.Id,
                    i % 2 == 0 ? "influences" : "renewed_with",
                    0.5m + (i % 5) * 0.1m),
                cancellationToken);

            await db.BusinessMemoryRelationships.AddAsync(
                BusinessMemoryRelationship.Link(
                    TenantIds.CeoDemo,
                    "Customer", from.Id,
                    "Deal", deals[i % deals.Count].Id,
                    "has_deal"),
                cancellationToken);
        }

        for (var i = 0; i < 10; i++)
        {
            var customer = customers[i];
            await db.CustomerAnalyticsSnapshots.AddAsync(
                CustomerAnalyticsSnapshot.Create(
                    TenantIds.CeoDemo,
                    customer.Id,
                    DateTime.UtcNow.AddDays(-i),
                    healthScore: 35 + i,
                    churnRiskScore: 65 + i,
                    npsScore: 6,
                    csatScore: 3.2m,
                    revenueAmount: 40_000m + i * 5_000m,
                    expansionScore: 50 + i * 3,
                    IntelligenceConstants.SegmentAtRisk,
                    engagementScore: 30 + i,
                    adoptionScore: 25 + i,
                    activeUsers: 2 + i),
                cancellationToken);
        }

        var ticketSubjects = new[]
        {
            "Incidencia facturación", "Baja adopción", "Solicitud capacitación", "Error integración",
            "Renovación pendiente", "Escalamiento ejecutivo", "CSAT bajo", "Onboarding retrasado",
            "Soporte técnico API", "Queja servicio", "Consulta contrato", "Upgrade plan"
        };
        await EnsureCsOsDemoDataAsync(db, logger, cancellationToken, customers, ticketSubjects);
        await EnsureAbosLearningDemoDataAsync(db, logger, cancellationToken, customers);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "{TenantName} seed complete: 50 customers, {Deals} deals, {Audits} AI audits, 8 trust approvals, CS tickets/cases",
            TenantName, deals.Count, audits.Count);
    }

    private static async Task EnsureCsOsDemoDataAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken,
        List<Customer>? customers = null,
        string[]? ticketSubjects = null)
    {
        var hasTickets = await db.WorkflowTasks.AnyAsync(
            t => t.TenantId == TenantIds.CeoDemo && t.TaskType == CustomerSuccessOsConstants.Ticket,
            cancellationToken);
        if (hasTickets)
            return;

        customers ??= await db.Customers.Where(c => c.TenantId == TenantIds.CeoDemo).Take(50).ToListAsync(cancellationToken);
        if (customers.Count == 0)
            return;

        ticketSubjects ??=
        [
            "Incidencia facturación", "Baja adopción", "Solicitud capacitación", "Error integración",
            "Renovación pendiente", "Escalamiento ejecutivo", "CSAT bajo", "Onboarding retrasado",
            "Soporte técnico API", "Queja servicio", "Consulta contrato", "Upgrade plan"
        ];

        for (var i = 0; i < ticketSubjects.Length; i++)
        {
            var customer = customers[i % customers.Count];
            var ticket = WorkflowTask.Create(
                TenantIds.CeoDemo,
                OperationalConstants.SystemWorkflowId,
                ticketSubjects[i],
                "Ticket demo Customer Success OS",
                customer.Id,
                "Customer",
                null,
                DateTime.UtcNow.AddDays(1 + i),
                i < 3 ? "Urgent" : "High",
                CustomerSuccessOsConstants.Ticket);
            if (i >= 10)
                ticket.Complete();
            await db.WorkflowTasks.AddAsync(ticket, cancellationToken);
        }

        var caseSpecs = new (string Type, string Title)[]
        {
            (CustomerSuccessOsConstants.CaseAtRisk, "Intervención churn Cliente Demo 01"),
            (CustomerSuccessOsConstants.CaseAtRisk, "Health crítico — escalamiento"),
            (CustomerSuccessOsConstants.CaseRenewal, "Renovación Q3 contrato anual"),
            (CustomerSuccessOsConstants.CaseRenewal, "Renovación VIP Enterprise"),
            (CustomerSuccessOsConstants.CaseExpansion, "Upsell módulo Analytics"),
            (CustomerSuccessOsConstants.CaseExpansion, "Cross-sell seats adicionales"),
            (CustomerSuccessOsConstants.CaseRecovery, "Plan recuperación post-incidente"),
            (CustomerSuccessOsConstants.CaseRecovery, "Recuperación NPS detractor")
        };
        for (var i = 0; i < caseSpecs.Length; i++)
        {
            var customer = customers[(i + 5) % customers.Count];
            var spec = caseSpecs[i];
            var csCase = WorkflowTask.Create(
                TenantIds.CeoDemo,
                OperationalConstants.SystemWorkflowId,
                spec.Title,
                "Caso demo CS OS",
                customer.Id,
                "Customer",
                null,
                DateTime.UtcNow.AddDays(5 + i),
                "High",
                spec.Type);
            if (i >= 6)
                csCase.Complete();
            await db.WorkflowTasks.AddAsync(csCase, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{TenantName}: seeded CS OS tickets and cases", TenantName);
    }

    private static async Task EnsureAbosLearningDemoDataAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken,
        List<Customer>? customers = null)
    {
        if (await db.BusinessMemoryLearnings.AnyAsync(
                l => l.TenantId == TenantIds.CeoDemo
                     && l.StrategyKey.StartsWith(AbosOutcomeLearningConstants.StrategyPrefixRecommendation),
                cancellationToken))
            return;

        customers ??= await db.Customers
            .Where(c => c.TenantId == TenantIds.CeoDemo)
            .OrderBy(c => c.Name)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (customers.Count == 0) return;

        var actionSpecs = new[]
        {
            ("Call", "Llamada de retención", "retention", true, 48_000m),
            ("Proposal", "Propuesta expansión", "expansion", true, 25_000m),
            ("Renewal", "Renovación contrato", "renewal", true, 15_000m),
            ("Email", "Email seguimiento", "retention", false, 0m),
            ("Call", "Llamada rescate", "retention", true, 32_000m),
            ("Proposal", "Upsell analytics", "expansion", true, 18_000m),
            ("Playbook:Rescue", "Playbook rescate", "retention", true, 12_000m),
            ("TrustApproval", "Aprobación humana", "retention", true, 8_000m)
        };

        for (var i = 0; i < actionSpecs.Length; i++)
        {
            var customer = customers[i % customers.Count];
            var (actionType, detail, category, succeeded, revenue) = actionSpecs[i];
            var root = BusinessMemoryRoot.CreateEpisode(
                TenantIds.CeoDemo,
                BusinessMemoryConstants.SubjectCustomer,
                customer.Id,
                $"ceo-demo-abos-action-{i}",
                $"Acción: {detail}",
                $"Recomendación ABOS demo — {detail}",
                importance: 6,
                sourceChannel: AbosOutcomeLearningConstants.SourceChannel,
                tags: new[] { AbosOutcomeLearningConstants.TagAction, AbosOutcomeLearningConstants.TagResolved, "demo" });
            await db.BusinessMemoryRoots.AddAsync(root, cancellationToken);

            await db.BusinessMemoryFacts.AddAsync(
                BusinessMemoryFact.Create(root.Id, TenantIds.CeoDemo, "action.type", actionType), cancellationToken);
            await db.BusinessMemoryFacts.AddAsync(
                BusinessMemoryFact.Create(root.Id, TenantIds.CeoDemo, "action.recommendation", detail), cancellationToken);
            await db.BusinessMemoryFacts.AddAsync(
                BusinessMemoryFact.Create(root.Id, TenantIds.CeoDemo, "action.resolved", succeeded ? "true" : "false"),
                cancellationToken);

            await db.BusinessMemoryOutcomes.AddAsync(
                BusinessMemoryOutcome.Record(
                    root.Id, TenantIds.CeoDemo, category, succeeded,
                    succeeded ? "Outcome positivo demo" : "Sin conversión demo",
                    revenue, succeeded ? 5 : -3, succeeded ? 2 : -1),
                cancellationToken);

            var learning = BusinessMemoryLearning.Start(
                TenantIds.CeoDemo,
                $"{AbosOutcomeLearningConstants.StrategyPrefixRecommendation}{actionType.ToLowerInvariant()}",
                detail);
            for (var j = 0; j < 4 + i % 3; j++)
                learning.ApplyOutcome(j % 3 != 0 || succeeded, j % 3 != 0 ? "won" : "lost");
            await db.BusinessMemoryLearnings.AddAsync(learning, cancellationToken);

            await db.NbaOutcomeRecords.AddAsync(
                NbaOutcomeRecord.FromAction(
                    TenantIds.CeoDemo,
                    BusinessMemoryConstants.SubjectCustomer,
                    customer.Id,
                    detail,
                    actionType,
                    succeeded,
                    revenue),
                cancellationToken);
        }

        var playbookLearning = BusinessMemoryLearning.Start(
            TenantIds.CeoDemo,
            $"{AbosOutcomeLearningConstants.StrategyPrefixPlaybook}rescue",
            "Playbook rescate");
        for (var j = 0; j < 6; j++)
            playbookLearning.ApplyOutcome(j % 4 != 0, "rescued");
        await db.BusinessMemoryLearnings.AddAsync(playbookLearning, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{TenantName}: seeded ABOS outcome learning demo data", TenantName);
    }

    private static async Task EnsureDemoUsersAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var demo in DemoRoleUsers.All)
        {
            var existing = await db.Users
                .FirstOrDefaultAsync(u => u.TenantId == TenantIds.CeoDemo && u.Email == demo.Email, cancellationToken);
            if (existing is not null)
                continue;

            var user = User.Create(
                TenantIds.CeoDemo,
                demo.Email,
                BCrypt.Net.BCrypt.HashPassword(DemoRoleUsers.PasswordFor(demo.Role)),
                demo.FirstName,
                demo.LastName);
            user.AddRole(demo.Role);
            await db.Users.AddAsync(user, cancellationToken);
            logger.LogInformation("CEO_DEMO user {Email} ({Role})", demo.Email, demo.Role);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
