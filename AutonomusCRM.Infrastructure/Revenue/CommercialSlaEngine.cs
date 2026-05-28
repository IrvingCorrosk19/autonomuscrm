using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Infrastructure.Revenue;

public class CommercialSlaEngine : ICommercialSlaEngine
{
    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IWorkflowTaskRepository _taskRepository;
    private readonly IOperationalTaskService _taskService;

    public CommercialSlaEngine(
        ILeadRepository leadRepository,
        IDealRepository dealRepository,
        IWorkflowTaskRepository taskRepository,
        IOperationalTaskService taskService)
    {
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
        _taskRepository = taskRepository;
        _taskService = taskService;
    }

    public async Task EnforceLeadCreatedSlaAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead == null || lead.TenantId != tenantId)
            return;

        if (await _taskRepository.ExistsOpenTaskAsync(tenantId, "Lead", leadId, "SLA_LeadContact24h", cancellationToken))
            return;

        await _taskService.CreateTaskAsync(
            tenantId,
            $"SLA: Contactar lead en 24h — {lead.Name}",
            "Primer contacto obligatorio dentro de 24 horas.",
            "Lead",
            leadId,
            lead.AssignedToUserId,
            lead.CreatedAt.AddHours(24),
            "High",
            "SLA_LeadContact24h",
            cancellationToken);
    }

    public async Task<IReadOnlyList<SlaBreachDto>> DetectBreachesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var breaches = new List<SlaBreachDto>();
        var now = DateTime.UtcNow;
        var openTasks = (await _taskRepository.GetByTenantAsync(tenantId, "Open", cancellationToken: cancellationToken))
            .Where(t => t.IsOverdue).ToList();

        foreach (var task in openTasks.Where(t => t.TaskType != null && t.TaskType.StartsWith("SLA_", StringComparison.Ordinal)))
        {
            breaches.Add(new SlaBreachDto(
                task.TaskType!,
                task.RelatedEntityType ?? "Unknown",
                task.RelatedEntityId ?? Guid.Empty,
                task.Title,
                task.DueDate ?? now,
                (int)(now - (task.DueDate ?? now)).TotalHours,
                "Completar o reasignar de inmediato"));
        }

        var leads = await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var lead in leads.Where(l => l.Status == LeadStatus.New && (now - l.CreatedAt).TotalHours > 24))
        {
            breaches.Add(new SlaBreachDto(
                "SLA_LeadContact24h",
                "Lead",
                lead.Id,
                lead.Name,
                lead.CreatedAt.AddHours(24),
                (int)(now - lead.CreatedAt.AddHours(24)).TotalHours,
                "Contactar lead urgente"));
        }

        var deals = await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var deal in deals.Where(d => d.Metadata.TryGetValue("AtRisk", out var v) && v?.ToString() == "true" && d.Status == DealStatus.Open))
        {
            var hasRescue = openTasks.Any(t => t.RelatedEntityId == deal.Id && t.TaskType == OperationalConstants.TaskTypeAtRisk);
            if (!hasRescue)
            {
                breaches.Add(new SlaBreachDto(
                    "SLA_DealAtRisk",
                    "Deal",
                    deal.Id,
                    deal.Title,
                    now.AddHours(-24),
                    24,
                    "Ejecutar rescate de deal en riesgo"));
            }
        }

        return breaches;
    }
}
