using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;

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

        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var tasks = (await _taskRepository.GetByTenantAsync(tenantId, cancellationToken: cancellationToken)).ToList();
        return BuildDto(customer, deals, tasks);
    }

    public async Task<IReadOnlyList<CustomerHealthDto>> CalculateAllAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP or CustomerStatus.Qualified)
            .ToList();
        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var tasks = (await _taskRepository.GetByTenantAsync(tenantId, cancellationToken: cancellationToken)).ToList();
        return customers.Select(c => BuildDto(c, deals, tasks)).OrderBy(h => h.HealthScore).ToList();
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
        Customer customer,
        List<Deal> deals,
        List<AutonomusCRM.Application.Automation.Workflows.WorkflowTask> tasks)
    {
        var customerDeals = deals.Where(d => d.CustomerId == customer.Id).ToList();
        var won = customerDeals.Where(d => d.Stage == DealStage.ClosedWon).ToList();
        var relatedTasks = CustomerSuccessCore.TasksForCustomer(customer.Id, tasks, deals).ToList();
        var open = relatedTasks.Where(t => t.Status == "Open").ToList();

        var adoption = CustomerSuccessCore.ScoreAdoption(relatedTasks);
        var engagement = CustomerSuccessCore.ScoreEngagement(customer);
        var support = CustomerSuccessCore.ScoreSupport(open);
        var revenue = CustomerSuccessCore.ScoreRevenue(customer, won);
        var riskComponent = CustomerSuccessCore.ScoreRiskComponent(customer);
        var health = CustomerSuccessCore.CompositeHealth(adoption, engagement, support, revenue, riskComponent);

        return new CustomerHealthDto(
            customer.Id,
            customer.Name,
            health,
            adoption,
            engagement,
            support,
            revenue,
            riskComponent,
            CustomerSuccessCore.ClassifyHealth(health));
    }
}
