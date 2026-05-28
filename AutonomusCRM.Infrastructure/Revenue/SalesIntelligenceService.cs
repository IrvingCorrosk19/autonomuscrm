using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Revenue;

public class SalesIntelligenceService : ISalesIntelligenceService
{
    private readonly IDealRepository _dealRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOperationalTaskService _taskService;
    private readonly IUnitOfWork _unitOfWork;

    public SalesIntelligenceService(
        IDealRepository dealRepository,
        ICustomerRepository customerRepository,
        IOperationalTaskService taskService,
        IUnitOfWork unitOfWork)
    {
        _dealRepository = dealRepository;
        _customerRepository = customerRepository;
        _taskService = taskService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SalesIntelligenceActionDto>> AnalyzeAndActAsync(
        Guid tenantId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
        if (deal == null || deal.TenantId != tenantId)
            return Array.Empty<SalesIntelligenceActionDto>();

        var customer = await _customerRepository.GetByIdAsync(deal.CustomerId, cancellationToken);
        var isAtRisk = deal.Metadata.TryGetValue("AtRisk", out var ar) && ar?.ToString() == "true";
        var actions = new List<(string Priority, string Action)>();

        if (isAtRisk)
            actions.Add(("Urgent", "Llamada de rescate: validar objeciones y próximos pasos con fecha."));
        else if (deal.Stage == DealStage.Negotiation)
            actions.Add(("High", "Confirmar términos finales y fecha de firma."));
        else if (deal.Stage == DealStage.Proposal)
            actions.Add(("Normal", "Seguimiento de propuesta en 48h."));
        else if (deal.Stage == DealStage.Qualification)
            actions.Add(("Normal", "Completar calificación BANT y avanzar a propuesta."));

        if (customer?.LifetimeValue > 50000)
            actions.Add(("High", "Cuenta alto valor — priorizar atención ejecutiva."));

        var results = new List<SalesIntelligenceActionDto>();
        foreach (var (priority, action) in actions)
        {
            var taskType = $"Intel_{priority}";
            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Deal", dealId, taskType, cancellationToken))
            {
                results.Add(new SalesIntelligenceActionDto(dealId, priority, action, null));
                continue;
            }

            var task = await _taskService.CreateTaskAsync(
                tenantId,
                $"Intel: {action}",
                $"Recomendación Deal Strategy — {deal.Title}",
                "Deal",
                dealId,
                deal.AssignedToUserId,
                DateTime.UtcNow.AddDays(priority == "Urgent" ? 1 : 3),
                priority,
                taskType,
                cancellationToken);

            results.Add(new SalesIntelligenceActionDto(dealId, priority, action, task.Id));
        }

        deal.UpdateMetadata("NextBestAction", actions.FirstOrDefault().Action ?? "");
        deal.UpdateMetadata("ActionPriority", actions.FirstOrDefault().Priority ?? "Normal");
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return results;
    }
}
