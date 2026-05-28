using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class RenewalEngine : IRenewalEngine
{
    private static readonly int[] Windows = { 90, 60, 30 };

    private readonly ICustomerContractRepository _contractRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOperationalTaskService _taskService;
    private readonly ICustomerPlaybookService _playbooks;

    public RenewalEngine(
        ICustomerContractRepository contractRepository,
        ICustomerRepository customerRepository,
        IOperationalTaskService taskService,
        ICustomerPlaybookService playbooks)
    {
        _contractRepository = contractRepository;
        _customerRepository = customerRepository;
        _taskService = taskService;
        _playbooks = playbooks;
    }

    public async Task<IReadOnlyList<RenewalAlertDto>> GetUpcomingRenewalsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var contracts = (await _contractRepository.GetByTenantAsync(tenantId, cancellationToken))
            .Where(c => c.Status == CustomerSuccessConstants.ContractActive)
            .ToList();
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);
        var now = DateTime.UtcNow;
        var alerts = new List<RenewalAlertDto>();

        foreach (var contract in contracts)
        {
            var days = contract.DaysUntilRenewal(now);
            if (days > 90 || days < 0)
                continue;

            var window = days <= 30 ? "30d" : days <= 60 ? "60d" : "90d";
            customers.TryGetValue(contract.CustomerId, out var name);
            alerts.Add(new RenewalAlertDto(
                contract.Id,
                contract.CustomerId,
                name ?? "Cliente",
                contract.RenewalDate,
                days,
                contract.AnnualValue,
                window));
        }

        return alerts.OrderBy(a => a.DaysUntilRenewal).ToList();
    }

    public async Task<RenewalForecastDto> GetRenewalForecastAsync(
        Guid tenantId, int horizonDays = 90, CancellationToken cancellationToken = default)
    {
        var contracts = (await _contractRepository.GetRenewingWithinDaysAsync(tenantId, horizonDays, cancellationToken)).ToList();
        var arr = contracts.Sum(c => c.AnnualValue);
        var atRisk = contracts.Where(c => c.DaysUntilRenewal(DateTime.UtcNow) <= 30).Sum(c => c.AnnualValue);
        return new RenewalForecastDto(horizonDays, arr, contracts.Count, atRisk);
    }

    public async Task<int> EnforceRenewalWindowsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var contracts = (await _contractRepository.GetByTenantAsync(tenantId, cancellationToken))
            .Where(c => c.Status == CustomerSuccessConstants.ContractActive)
            .ToList();
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);
        var created = 0;
        var now = DateTime.UtcNow;

        foreach (var contract in contracts)
        {
            var days = contract.DaysUntilRenewal(now);
            string? taskType = days switch
            {
                <= 30 and >= 0 => CustomerSuccessConstants.TaskRenewal30,
                <= 60 and > 30 => CustomerSuccessConstants.TaskRenewal60,
                <= 90 and > 60 => CustomerSuccessConstants.TaskRenewal90,
                _ => null
            };

            if (taskType == null)
                continue;

            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Customer", contract.CustomerId, taskType, cancellationToken))
                continue;

            customers.TryGetValue(contract.CustomerId, out var name);
            await _taskService.CreateTaskAsync(
                tenantId,
                $"Renovación {taskType.Replace("Renewal_", "")} — {name}",
                $"Contrato vence {contract.RenewalDate:yyyy-MM-dd}. ARR {contract.AnnualValue:N2}.",
                "Customer",
                contract.CustomerId,
                null,
                contract.RenewalDate.AddDays(-7),
                days <= 30 ? "Urgent" : "High",
                taskType,
                cancellationToken);
            created++;

            if (days <= 90 && days > 60)
                await _playbooks.ExecutePlaybookAsync(tenantId, contract.CustomerId, CustomerSuccessConstants.PlaybookRenewal, cancellationToken: cancellationToken);
        }

        return created;
    }
}
