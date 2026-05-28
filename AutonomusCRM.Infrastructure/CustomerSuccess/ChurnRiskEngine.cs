using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class ChurnRiskEngine : IChurnRiskEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly ICustomerPlaybookService _playbooks;
    private readonly IOperationalTaskService _taskService;
    private readonly IWorkflowTaskRepository _taskRepository;
    private readonly ICustomerAnalyticsSnapshotRepository _snapshotRepository;

    public ChurnRiskEngine(
        ICustomerRepository customerRepository,
        ICustomerHealthEngine healthEngine,
        ICustomerPlaybookService playbooks,
        IOperationalTaskService taskService,
        IWorkflowTaskRepository taskRepository,
        ICustomerAnalyticsSnapshotRepository snapshotRepository)
    {
        _customerRepository = customerRepository;
        _healthEngine = healthEngine;
        _playbooks = playbooks;
        _taskService = taskService;
        _taskRepository = taskRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<IReadOnlyList<ChurnRiskSignalDto>> DetectSignalsAsync(
        Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var customers = customerId.HasValue
            ? new[] { await _customerRepository.GetByIdAsync(customerId.Value, cancellationToken) }
                .Where(c => c != null && c.TenantId == tenantId).Cast<Customer>().ToList()
            : (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
                .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP).ToList();

        var healthList = await _healthEngine.CalculateAllAsync(tenantId, cancellationToken);
        var healthById = healthList.ToDictionary(h => h.CustomerId);
        var openTasks = (await _taskRepository.GetOpenByTenantAsync(tenantId, cancellationToken)).ToList();
        var signals = new List<ChurnRiskSignalDto>();

        foreach (var customer in customers)
        {
            if (!healthById.TryGetValue(customer.Id, out var health))
                continue;

            if (health.Classification == CustomerSuccessConstants.HealthCritical)
            {
                signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "LowHealth", "High",
                    $"Health score {health.HealthScore} — clasificación Critical"));
            }

            if (!customer.LastContactAt.HasValue || (DateTime.UtcNow - customer.LastContactAt.Value).TotalDays > 60)
            {
                signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "Inactivity", "High",
                    "Sin contacto en más de 60 días"));
            }

            if (health.AdoptionScore < 40)
            {
                signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "IncompleteOnboarding", "Medium",
                    "Onboarding incompleto o tareas de adopción pendientes"));
            }

            var overdue = openTasks.Count(t => t.RelatedEntityId == customer.Id && t.IsOverdue);
            if (overdue > 0)
            {
                signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "OverdueTasks", "Medium",
                    $"{overdue} tarea(s) vencida(s)"));
            }

            if (health.EngagementScore < 30)
            {
                signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "LowUsage", "Medium",
                    "Engagement bajo — uso o contacto insuficiente"));
            }

            var history = (await _snapshotRepository.GetByCustomerAsync(tenantId, customer.Id, 14, cancellationToken))
                .OrderBy(s => s.SnapshotDate).ToList();
            if (history.Count >= 2)
            {
                var healthDrop = history.Last().HealthScore - history.First().HealthScore;
                var engagementDrop = history.Last().EngagementScore - history.First().EngagementScore;
                if (healthDrop <= -15)
                {
                    signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "HealthTrendDown", "High",
                        $"Health cayó {healthDrop} pts en {history.Count} snapshots"));
                }
                if (engagementDrop <= -15)
                {
                    signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "EngagementTrendDown", "Medium",
                        $"Engagement cayó {engagementDrop} pts (histórico)"));
                }
            }

            var supportOpen = openTasks.Count(t =>
                t.RelatedEntityType == "Customer" && t.RelatedEntityId == customer.Id
                && t.TaskType != null && t.TaskType.StartsWith("Support_", StringComparison.Ordinal));
            if (supportOpen > 0)
            {
                signals.Add(new ChurnRiskSignalDto(customer.Id, customer.Name, "OpenSupport", "Low",
                    $"{supportOpen} ticket(s) de soporte abiertos (proxy tareas)"));
            }
        }

        return signals;
    }

    public async Task<int> EnforceAlertsAndPlaybooksAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var signals = await DetectSignalsAsync(tenantId, cancellationToken: cancellationToken);
        var acted = 0;
        var highRiskCustomers = signals
            .Where(s => s.Severity == "High")
            .Select(s => s.CustomerId)
            .Distinct();

        foreach (var customerId in highRiskCustomers)
        {
            if (!await _taskService.ExistsOpenTaskAsync(tenantId, "Customer", customerId, CustomerSuccessConstants.TaskChurnAlert, cancellationToken))
            {
                await _taskService.CreateTaskAsync(
                    tenantId,
                    "Alerta churn — intervención CS",
                    "Cliente con señales de churn alto. Ejecutar playbook Rescue.",
                    "Customer",
                    customerId,
                    null,
                    DateTime.UtcNow.AddDays(2),
                    "Urgent",
                    CustomerSuccessConstants.TaskChurnAlert,
                    cancellationToken);
                acted++;
            }

            await _playbooks.ExecutePlaybookAsync(tenantId, customerId, CustomerSuccessConstants.PlaybookRescue, cancellationToken: cancellationToken);
        }

        return acted;
    }
}
