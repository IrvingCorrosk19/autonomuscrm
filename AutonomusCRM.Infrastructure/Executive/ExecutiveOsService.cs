using System.Net;
using System.Text;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Executive;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Executive;

public sealed class ExecutiveOsService : IExecutiveOsService
{
    private readonly IExecutiveAiDashboardService _ai;
    private readonly IRevenueOsService _revenue;
    private readonly IAbosOutcomeLearningService _learning;
    private readonly IAiCommandCenterService _command;
    private readonly IAiTrustService _trust;
    private readonly ApplicationDbContext _db;

    public ExecutiveOsService(
        IExecutiveAiDashboardService ai,
        IRevenueOsService revenue,
        IAbosOutcomeLearningService learning,
        IAiCommandCenterService command,
        IAiTrustService trust,
        ApplicationDbContext db)
    {
        _ai = ai;
        _revenue = revenue;
        _learning = learning;
        _command = command;
        _trust = trust;
        _db = db;
    }

    public async Task<ExecutiveOsDashboardDto> GetDashboardAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var ai = await _ai.GetDashboardAsync(tenantId, cancellationToken);
        var rev = await _revenue.GetDashboardAsync(tenantId, cancellationToken);
        AbosExecutiveLearningDto? learning = null;
        try { learning = await _learning.GetExecutiveLearningAsync(tenantId, cancellationToken); }
        catch { /* non-blocking */ }

        var inbox = await _trust.GetInboxAsync(tenantId, cancellationToken);
        var trustPending = inbox.Count(i => i.Status == "pending");

        var pulse = new ExecutivePulseDto(
            rev.Overview.RevenueGenerated + (learning?.RevenueGeneratedByActions ?? 0),
            rev.Overview.RevenueProtected + (learning?.RevenueProtected ?? 0),
            rev.Overview.RevenueAtRisk,
            ai.AtRiskCustomers,
            ai.ExpansionReady,
            ai.PendingDecisions + trustPending,
            ai.NextBestActions.Count);

        var aiImpact = await BuildAiImpactAsync(tenantId, rev, learning, cancellationToken);
        var chains = BuildOutcomeChains(rev, learning);
        var qbr = new ExecutiveQbrDto(
            await BuildQbrPeriodAsync(tenantId, "monthly", "Mensual", 30, cancellationToken),
            await BuildQbrPeriodAsync(tenantId, "quarterly", "Trimestral", 90, cancellationToken),
            await BuildQbrPeriodAsync(tenantId, "annual", "Anual", 365, cancellationToken));

        var hasData = rev.HasData || ai.RecentDecisions.Count > 0 || pulse.RevenueGenerated > 0;

        return new ExecutiveOsDashboardDto(
            pulse, aiImpact, chains, qbr, ai, rev, learning, trustPending, hasData);
    }

    public async Task<string> BuildExportHtmlAsync(
        Guid tenantId, string exportType, CancellationToken cancellationToken = default)
    {
        var os = await GetDashboardAsync(tenantId, cancellationToken);
        return exportType.Equals("board", StringComparison.OrdinalIgnoreCase)
            ? ExecutiveOsExportHtml.BuildBoardSummary(os)
            : ExecutiveOsExportHtml.BuildExecutiveSummary(os);
    }

    private async Task<AiImpactSummaryDto> BuildAiImpactAsync(
        Guid tenantId,
        RevenueOsDashboardDto rev,
        AbosExecutiveLearningDto? learning,
        CancellationToken cancellationToken)
    {
        var since90 = DateTime.UtcNow.AddDays(-90);
        var audits = await _db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= since90)
            .ToListAsync(cancellationToken);

        var approvals = await _db.AiApprovalRequests.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= since90)
            .ToListAsync(cancellationToken);

        return new AiImpactSummaryDto(
            rev.Overview.RevenueGenerated + (learning?.RevenueGeneratedByActions ?? 0),
            rev.Overview.RevenueProtected + (learning?.RevenueProtected ?? 0),
            audits.Count(a => a.Status == AutonomousConstants.AuditExecuted),
            audits.Count(a => a.Status == AutonomousConstants.AuditPending),
            approvals.Count(a => a.Status == "approved"),
            approvals.Count(a => a.Status == "rejected"));
    }

    private static IReadOnlyList<OutcomeAttributionChainDto> BuildOutcomeChains(
        RevenueOsDashboardDto rev, AbosExecutiveLearningDto? learning)
    {
        var chains = rev.AttributionChains
            .Select(a => new OutcomeAttributionChainDto(
                a.Action,
                a.LearningStatus == "pending" ? "Pendiente" : a.Status,
                a.RevenueImpact ?? 0,
                a.LearningStatus,
                a.CreatedAt,
                null))
            .ToList();

        if (learning != null)
        {
            foreach (var action in learning.TopActions.Take(5))
            {
                if (chains.Any(c => c.Action == action.Label)) continue;
                chains.Add(new OutcomeAttributionChainDto(
                    action.Label,
                    $"{action.SuccessRate:F0}% éxito",
                    action.RevenueImpact,
                    "learned",
                    DateTime.UtcNow,
                    null));
            }
        }

        return chains.OrderByDescending(c => c.OccurredAt).Take(12).ToList();
    }

    private async Task<QbrPeriodDto> BuildQbrPeriodAsync(
        Guid tenantId, string key, string label, int days, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var outcomes = await _command.GetOutcomesSummaryAsync(tenantId, days, cancellationToken);

        var dealsWon = await _db.Deals.AsNoTracking()
            .CountAsync(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedWon
                && d.ClosedAt >= since, cancellationToken);

        var dealsLost = await _db.Deals.AsNoTracking()
            .CountAsync(d => d.TenantId == tenantId && d.Stage == DealStage.ClosedLost
                && d.ClosedAt >= since, cancellationToken);

        var aiExecuted = await _db.AiDecisionAudits.AsNoTracking()
            .CountAsync(a => a.TenantId == tenantId && a.CreatedAt >= since
                && a.Status == AutonomousConstants.AuditExecuted, cancellationToken);

        var humanApproved = await _db.AiApprovalRequests.AsNoTracking()
            .CountAsync(a => a.TenantId == tenantId && a.CreatedAt >= since && a.Status == "approved",
                cancellationToken);

        var lost = await _db.BusinessMemoryOutcomes.AsNoTracking()
            .Where(o => o.TenantId == tenantId && !o.Succeeded && o.CreatedAt >= since)
            .SumAsync(o => Math.Abs(o.RevenueDelta), cancellationToken);

        var headline = outcomes.RevenueGenerated > 0
            ? $"${outcomes.RevenueGenerated:N0} generados · ${outcomes.RevenueProtected:N0} protegidos"
            : dealsWon > 0
                ? $"{dealsWon} deals ganados en {label.ToLowerInvariant()}"
                : "Sin outcomes registrados — ejecute acciones ABOS";

        return new QbrPeriodDto(
            key, label, days,
            outcomes.RevenueGenerated,
            outcomes.RevenueProtected,
            0,
            lost,
            dealsWon,
            dealsLost,
            aiExecuted,
            humanApproved,
            outcomes.CompleteChains,
            headline);
    }
}

internal static class ExecutiveOsExportHtml
{
    public static string BuildExecutiveSummary(ExecutiveOsDashboardDto os)
        => Wrap("Executive Summary", os, includeBoardDetail: false);

    public static string BuildBoardSummary(ExecutiveOsDashboardDto os)
        => Wrap("Board Summary", os, includeBoardDetail: true);

    private static string Wrap(string title, ExecutiveOsDashboardDto os, bool includeBoardDetail)
    {
        var p = os.Pulse;
        var ai = os.AiImpact;
        var q = os.Qbr.Quarterly;
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<title>").Append(WebUtility.HtmlEncode(title)).Append("</title>");
        sb.Append("""
            <style>
            body{font-family:Inter,Segoe UI,sans-serif;margin:40px;color:#111;max-width:900px}
            h1{font-size:22px;margin:0 0 4px} .sub{color:#666;font-size:13px;margin-bottom:24px}
            .grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin:20px 0}
            .metric{border:1px solid #e5e7eb;border-radius:8px;padding:12px}
            .metric label{font-size:11px;color:#666;text-transform:uppercase;display:block}
            .metric strong{font-size:20px}
            table{width:100%;border-collapse:collapse;font-size:13px;margin-top:16px}
            th,td{border-bottom:1px solid #eee;padding:8px;text-align:left}
            @media print{body{margin:20px}}
            </style></head><body>
            """);
        sb.Append("<h1>").Append(WebUtility.HtmlEncode(title)).Append(" — AutonomusFlow</h1>");
        sb.Append("<p class=\"sub\">Generado ").Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC")).Append("</p>");

        sb.Append("<div class=\"grid\">");
        Metric(sb, "Revenue generado", $"${p.RevenueGenerated:N0}");
        Metric(sb, "Revenue protegido", $"${p.RevenueProtected:N0}");
        Metric(sb, "Revenue en riesgo", $"${p.RevenueAtRisk:N0}");
        Metric(sb, "Clientes en riesgo", p.AtRiskCustomers.ToString());
        Metric(sb, "Listos expansión", p.ExpansionReady.ToString());
        Metric(sb, "Decisiones pendientes", p.PendingDecisions.ToString());
        sb.Append("</div>");

        sb.Append("<h2>Impacto IA (90d)</h2><div class=\"grid\">");
        Metric(sb, "Generado por IA", $"${ai.RevenueGeneratedByAi:N0}");
        Metric(sb, "Protegido por IA", $"${ai.RevenueProtectedByAi:N0}");
        Metric(sb, "Decisiones IA ejecutadas", ai.AiDecisionsExecuted.ToString());
        Metric(sb, "Aprobaciones humanas", ai.HumanApprovals.ToString());
        sb.Append("</div>");

        sb.Append("<h2>QBR Trimestral</h2><p>").Append(WebUtility.HtmlEncode(q.Headline)).Append("</p>");
        sb.Append("<table><tr><th>Métrica</th><th>Valor</th></tr>");
        Row(sb, "Revenue generado", $"${q.RevenueGenerated:N0}");
        Row(sb, "Revenue protegido", $"${q.RevenueProtected:N0}");
        Row(sb, "Deals ganados", q.DealsWon.ToString());
        Row(sb, "Deals perdidos", q.DealsLost.ToString());
        Row(sb, "Cadenas outcome completas", q.CompleteOutcomeChains.ToString());
        sb.Append("</table>");

        if (includeBoardDetail && os.OutcomeChains.Count > 0)
        {
            sb.Append("<h2>Outcome Attribution</h2><table><tr><th>Acción</th><th>Resultado</th><th>Revenue</th></tr>");
            foreach (var c in os.OutcomeChains.Take(10))
            {
                sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(c.Action));
                sb.Append("</td><td>").Append(WebUtility.HtmlEncode(c.Outcome));
                sb.Append("</td><td>$").Append(c.Revenue.ToString("N0")).Append("</td></tr>");
            }
            sb.Append("</table>");
        }

        sb.Append("<p class=\"sub\">Exportación print-ready — use Imprimir → Guardar como PDF en el navegador.</p>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void Metric(StringBuilder sb, string label, string value)
    {
        sb.Append("<div class=\"metric\"><label>").Append(WebUtility.HtmlEncode(label));
        sb.Append("</label><strong>").Append(WebUtility.HtmlEncode(value)).Append("</strong></div>");
    }

    private static void Row(StringBuilder sb, string k, string v)
    {
        sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(k));
        sb.Append("</td><td>").Append(WebUtility.HtmlEncode(v)).Append("</td></tr>");
    }
}
