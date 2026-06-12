using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class CustomerHealthEngine : ICustomerHealthEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IWorkflowTaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerHealthEngine(
        ICustomerRepository customerRepository,
        IDealRepository dealRepository,
        IWorkflowTaskRepository taskRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _dealRepository = dealRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerHealthDto> CalculateHealthAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {customerId} not found");
        if (customer.TenantId != tenantId)
            throw new InvalidOperationException("Tenant mismatch");

        var wonSum = await _dealRepository.GetWonAmountForCustomerAsync(tenantId, customerId, cancellationToken);
        var tasks = await _taskRepository.GetHealthTaskAggregateForCustomerAsync(tenantId, customerId, cancellationToken);
        return BuildDto(customer.Id, customer.Name, customer.LastContactAt, customer.LifetimeValue, customer.RiskScore, wonSum, tasks);
    }

    public async Task<IReadOnlyList<CustomerHealthDto>> CalculateAllAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetHealthEligibleProjectionsAsync(tenantId, cancellationToken);
        var wonByCustomer = await _dealRepository.GetWonAmountByCustomerAsync(tenantId, cancellationToken);
        var taskAggregates = await _taskRepository.GetHealthTaskAggregatesByCustomerAsync(tenantId, cancellationToken);

        return customers
            .Select(c =>
            {
                wonByCustomer.TryGetValue(c.Id, out var wonSum);
                taskAggregates.TryGetValue(c.Id, out var tasks);
                tasks ??= new CustomerTaskHealthAggregate(c.Id, 0, 0, 0, 0);
                return BuildDto(c.Id, c.Name, c.LastContactAt, c.LifetimeValue, c.RiskScore, wonSum, tasks);
            })
            .OrderBy(h => h.HealthScore)
            .ToList();
    }

    public async Task<double?> GetAverageHealthScoreAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var health = await CalculateAllAsync(tenantId, cancellationToken);
        return health.Count > 0 ? health.Average(h => h.HealthScore) : null;
    }

    public async Task PersistHealthAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var dto = await CalculateHealthAsync(tenantId, customerId, cancellationToken);
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
            return;

        customer.UpdateMetadata("HealthScore", dto.HealthScore);
        customer.UpdateMetadata("HealthClassification", dto.Classification);
        customer.UpdateMetadata("AdoptionScore", dto.AdoptionScore);
        customer.UpdateMetadata("EngagementScore", dto.EngagementScore);
        customer.UpdateMetadata("SupportScore", dto.SupportScore);
        customer.UpdateMetadata("RevenueScore", dto.RevenueScore);
        customer.UpdateMetadata("HealthCalculatedAt", DateTime.UtcNow);

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static CustomerHealthDto BuildDto(
        Guid customerId,
        string name,
        DateTime? lastContactAt,
        decimal? lifetimeValue,
        int? riskScore,
        decimal wonAmountSum,
        CustomerTaskHealthAggregate tasks)
    {
        var adoption = CustomerSuccessCore.ScoreAdoption(tasks.OnboardingTotal, tasks.OnboardingCompleted);
        var engagement = CustomerSuccessCore.ScoreEngagement(lastContactAt);
        var support = CustomerSuccessCore.ScoreSupport(tasks.OpenTaskCount, tasks.OverdueOpenCount);
        var revenue = CustomerSuccessCore.ScoreRevenue(lifetimeValue, wonAmountSum);
        var riskComponent = CustomerSuccessCore.ScoreRiskComponent(riskScore);
        var health = CustomerSuccessCore.CompositeHealth(adoption, engagement, support, revenue, riskComponent);

        return new CustomerHealthDto(
            customerId,
            name,
            health,
            adoption,
            engagement,
            support,
            revenue,
            riskComponent,
            CustomerSuccessCore.ClassifyHealth(health));
    }
}
