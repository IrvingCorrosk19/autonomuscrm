using System.Security.Claims;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Communications;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages;

/// <summary>Wires insight CTAs to existing Tasks, Deals, Comms and Trust services.</summary>
public class FlowActionsModel : PageModel
{
    private readonly IOperationalTaskService _tasks;
    private readonly ICommunicationDeliveryService _comms;
    private readonly ICustomerRepository _customers;
    private readonly IAiTrustService _trust;
    private readonly IAbosOutcomeLearningService _learning;
    private readonly IServiceProvider _sp;

    public FlowActionsModel(
        IOperationalTaskService tasks,
        ICommunicationDeliveryService comms,
        ICustomerRepository customers,
        IAiTrustService trust,
        IAbosOutcomeLearningService learning,
        IServiceProvider sp)
    {
        _tasks = tasks;
        _comms = comms;
        _customers = customers;
        _trust = trust;
        _learning = learning;
        _sp = sp;
    }

    public async Task<IActionResult> OnPostCreateTaskAsync(
        Guid customerId,
        string title,
        string taskType,
        string? returnUrl,
        string? insightType,
        string? recommendation,
        string? rationale,
        string priority = "High")
    {
        var tenantId = await GetTenantIdAsync();
        if (tenantId == Guid.Empty) return BadRequest();

        await _tasks.CreateTaskAsync(
            tenantId, title, null, "Customer", customerId, null,
            DateTime.UtcNow.AddDays(taskType is "Call" or "Meeting" ? 1 : 3),
            priority, taskType);

        await RecordAbosActionAsync(
            taskType,
            title,
            insightType ?? FlowInsightTypes.Risk,
            recommendation ?? FlowInsightTypes.DefaultRecommendation(insightType ?? FlowInsightTypes.Risk, taskType),
            rationale ?? "Acción desde insight ABOS",
            customerId);

        SetSuccess("Tarea creada.");
        return Redirect(SafeReturn(returnUrl));
    }

    public async Task<IActionResult> OnPostCreatePlanAsync(
        string title,
        string? returnUrl,
        string? insightType,
        string? recommendation,
        string? rationale)
    {
        var tenantId = await GetTenantIdAsync();
        if (tenantId == Guid.Empty) return BadRequest();

        await _tasks.CreateTaskAsync(
            tenantId, title, "Plan de acción desde Revenue OS", "Revenue", tenantId, null,
            DateTime.UtcNow.AddDays(7), "High", "RecoveryPlan");

        await RecordAbosActionAsync(
            "RecoveryPlan",
            title,
            insightType ?? FlowInsightTypes.RevenueAtRisk,
            recommendation ?? FlowInsightTypes.DefaultRecommendation(FlowInsightTypes.RevenueAtRisk),
            rationale ?? "Plan de recuperación de revenue",
            null);

        SetSuccess("Plan de recuperación creado como tarea.");
        return Redirect(SafeReturn(returnUrl));
    }

    public async Task<IActionResult> OnPostSendEmailAsync(
        Guid customerId,
        string subject,
        string body,
        string? returnUrl,
        string? insightType,
        string? recommendation,
        string? rationale)
    {
        var tenantId = await GetTenantIdAsync();
        if (tenantId == Guid.Empty) return BadRequest();

        var customer = await _customers.GetByIdAsync(customerId);
        if (customer == null)
        {
            SetError("Cliente no encontrado.");
            return Redirect(SafeReturn(returnUrl));
        }

        var to = customer.Email ?? customer.Name;
        var result = await _comms.SendEmailAsync(new CommunicationSendRequest(
            tenantId, "Email", to, subject, body, customerId, null, null, "InsightAction"));

        await RecordAbosActionAsync(
            "Email",
            subject,
            insightType ?? FlowInsightTypes.Risk,
            recommendation ?? FlowInsightTypes.DefaultRecommendation(insightType ?? FlowInsightTypes.Risk, "Email"),
            rationale ?? body,
            customerId);

        SetSuccess(result.Success ? "Email enviado." : "Comunicación registrada.");
        return Redirect(SafeReturn(returnUrl));
    }

    public async Task<IActionResult> OnPostCreateDealAsync(
        Guid customerId,
        string title,
        decimal amount,
        string? returnUrl,
        string? insightType,
        string? recommendation,
        string? rationale)
    {
        var tenantId = await GetTenantIdAsync();
        if (tenantId == Guid.Empty) return BadRequest();

        var handler = _sp.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
        var dealId = await handler.HandleAsync(new CreateDealCommand(tenantId, customerId, title, amount, null));

        await RecordAbosActionAsync(
            "CreateDeal",
            title,
            insightType ?? FlowInsightTypes.Expansion,
            recommendation ?? FlowInsightTypes.DefaultRecommendation(insightType ?? FlowInsightTypes.Expansion),
            rationale ?? $"Oportunidad ${amount:N0}",
            customerId);

        SetSuccess("Oportunidad creada.");
        return Redirect($"/Deals/Details/{dealId}");
    }

    public async Task<IActionResult> OnPostRequestApprovalAsync(
        Guid customerId,
        string? returnUrl,
        string? insightType,
        string? recommendation,
        string? rationale)
    {
        var tenantId = await GetTenantIdAsync();
        if (tenantId == Guid.Empty) return BadRequest();

        var inbox = await _trust.GetInboxAsync(tenantId);
        var pending = inbox.FirstOrDefault(i => i.CustomerId == customerId && i.Status == "pending");
        if (pending != null)
            return Redirect($"/TrustInbox?id={pending.Id}");

        await _tasks.CreateTaskAsync(
            tenantId,
            "Escalar a Trust Studio",
            "Revisar y solicitar aprobación humana para acciones IA sobre este cliente.",
            "Customer", customerId, null,
            DateTime.UtcNow.AddDays(1), "High", "TrustApproval");

        await RecordAbosActionAsync(
            "TrustApproval",
            "Escalar a Trust Studio",
            insightType ?? FlowInsightTypes.Risk,
            recommendation ?? "Solicitar aprobación humana",
            rationale ?? "Escalamiento desde insight ABOS",
            customerId);

        SetSuccess("Escalado a Trust Studio. Revise la cola de aprobaciones.");
        return Redirect("/TrustInbox");
    }

    private async Task RecordAbosActionAsync(
        string actionType,
        string detail,
        string insightType,
        string recommendation,
        string rationale,
        Guid? customerId,
        Guid? relatedAuditId = null)
    {
        var tenantId = await GetTenantIdAsync();
        if (tenantId == Guid.Empty) return;

        try
        {
            await _learning.RecordActionExecutedAsync(
                tenantId,
                GetUserId(),
                actionType,
                detail,
                insightType,
                recommendation,
                rationale,
                customerId,
                relatedAuditId,
                HttpContext.RequestAborted);
        }
        catch
        {
            // Non-blocking: operational action already succeeded.
        }
    }

    private Task<Guid> GetTenantIdAsync()
        => this.GetTenantIdForPageAsync(_sp);

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static string SafeReturn(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith('/'))
            return "/";
        return url;
    }

    private void SetSuccess(string message) => TempData["FlowActionMessage"] = message;
    private void SetError(string message) => TempData["FlowActionError"] = message;
}
