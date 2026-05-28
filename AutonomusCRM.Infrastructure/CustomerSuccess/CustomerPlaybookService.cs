using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class CustomerPlaybookService : ICustomerPlaybookService
{
    private readonly IOperationalTaskService _taskService;

    public CustomerPlaybookService(IOperationalTaskService taskService)
    {
        _taskService = taskService;
    }

    public async Task<PlaybookExecutionDto> ExecutePlaybookAsync(
        Guid tenantId,
        Guid customerId,
        string playbookType,
        Guid? assignedToUserId = null,
        CancellationToken cancellationToken = default)
    {
        var steps = GetPlaybookSteps(playbookType);
        var createdTypes = new List<string>();

        foreach (var step in steps)
        {
            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Customer", customerId, step.TaskType, cancellationToken))
                continue;

            await _taskService.CreateTaskAsync(
                tenantId,
                step.Title,
                step.Description,
                "Customer",
                customerId,
                assignedToUserId,
                DateTime.UtcNow.AddDays(step.DueInDays),
                step.Priority,
                step.TaskType,
                cancellationToken);
            createdTypes.Add(step.TaskType);
        }

        return new PlaybookExecutionDto(playbookType, customerId, createdTypes.Count, createdTypes);
    }

    private static IReadOnlyList<PlaybookStep> GetPlaybookSteps(string playbookType) => playbookType switch
    {
        CustomerSuccessConstants.PlaybookOnboarding =>
        [
            new("PB_Onboard_Kickoff", "Kick-off onboarding", "Reunión inicial y objetivos de éxito.", 1, "High"),
            new("PB_Onboard_Config", "Configuración cuenta", "Validar integraciones y usuarios activos.", 3, "Normal"),
            new("PB_Onboard_Training", "Capacitación", "Sesión de formación al equipo cliente.", 7, "Normal")
        ],
        CustomerSuccessConstants.PlaybookAdoption =>
        [
            new("PB_Adopt_Usage", "Revisión de uso", "Analizar métricas de adopción semana 2.", 14, "Normal"),
            new("PB_Adopt_QBR", "QBR ligero", "Revisión trimestral de valor entregado.", 30, "Normal")
        ],
        CustomerSuccessConstants.PlaybookRescue =>
        [
            new("PB_Rescue_Call", "Llamada rescate", "Contacto ejecutivo en 24h.", 1, "Urgent"),
            new("PB_Rescue_Plan", "Plan de recuperación", "Documentar causas y acciones correctivas.", 3, "Urgent"),
            new(CustomerSuccessConstants.TaskHealthRescue, "Seguimiento rescate", "Verificar mejora de health score.", 7, "High")
        ],
        CustomerSuccessConstants.PlaybookRenewal =>
        [
            new("PB_Renew_Review", "Revisión renovación", "Validar valor y términos de renovación.", 14, "High"),
            new("PB_Renew_Proposal", "Propuesta renovación", "Enviar propuesta comercial.", 21, "High"),
            new("PB_Renew_Close", "Cierre renovación", "Confirmar firma y actualizar contrato.", 28, "Urgent")
        ],
        CustomerSuccessConstants.PlaybookExpansion =>
        [
            new("PB_Expand_Discover", "Descubrimiento expansión", "Identificar upsell/cross-sell.", 7, "Normal"),
            new(CustomerSuccessConstants.TaskExpansion, "Oportunidad expansión", "Crear oportunidad comercial de expansión.", 14, "High")
        ],
        CustomerSuccessConstants.PlaybookReEngagement =>
        [
            new("PB_Reengage_Outreach", "Re-engagement", "Campaña de reactivación multicanal.", 1, "High"),
            new(CustomerSuccessConstants.TaskReEngagement, "Seguimiento re-engagement", "Confirmar respuesta del cliente.", 5, "Normal")
        ],
        _ => []
    };

    private sealed record PlaybookStep(string TaskType, string Title, string Description, int DueInDays, string Priority);
}
