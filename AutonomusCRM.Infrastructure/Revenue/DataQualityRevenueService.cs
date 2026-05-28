using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Infrastructure.Revenue;

public class DataQualityRevenueService : IDataQualityRevenueService
{
    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOperationalTaskService _taskService;

    public DataQualityRevenueService(
        ILeadRepository leadRepository,
        IDealRepository dealRepository,
        ICustomerRepository customerRepository,
        IOperationalTaskService taskService)
    {
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
        _customerRepository = customerRepository;
        _taskService = taskService;
    }

    public async Task<int> ScanAndCreateTasksAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var created = 0;
        var deals = await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var deal in deals.Where(d => d.Status == DealStatus.Open))
        {
            if (!deal.AssignedToUserId.HasValue)
                created += await CreateDqTask(tenantId, "Deal", deal.Id, "DQ_NoOwner", $"Asignar owner a deal: {deal.Title}", cancellationToken);
            if (!deal.ExpectedCloseDate.HasValue)
                created += await CreateDqTask(tenantId, "Deal", deal.Id, "DQ_NoCloseDate", $"Definir fecha cierre: {deal.Title}", cancellationToken);
        }

        var leads = await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var lead in leads.Where(l => l.Status == LeadStatus.New && (DateTime.UtcNow - l.CreatedAt).TotalDays > 7))
            created += await CreateDqTask(tenantId, "Lead", lead.Id, "DQ_AbandonedLead", $"Lead abandonado: {lead.Name}", cancellationToken);

        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var dupEmails = customers.Where(c => !string.IsNullOrWhiteSpace(c.Email))
            .GroupBy(c => c.Email!.Trim().ToLowerInvariant()).Where(g => g.Count() > 1);
        foreach (var group in dupEmails)
        {
            foreach (var c in group)
                created += await CreateDqTask(tenantId, "Customer", c.Id, "DQ_DuplicateEmail", $"Duplicado email: {c.Email}", cancellationToken);
        }

        var orphanDeals = deals.Where(d => !customers.Any(c => c.Id == d.CustomerId));
        foreach (var d in orphanDeals)
            created += await CreateDqTask(tenantId, "Deal", d.Id, "DQ_OrphanDeal", $"Deal huérfano: {d.Title}", cancellationToken);

        return created;
    }

    private async Task<int> CreateDqTask(
        Guid tenantId, string entityType, Guid entityId, string taskType, string title, CancellationToken ct)
    {
        if (await _taskService.ExistsOpenTaskAsync(tenantId, entityType, entityId, taskType, ct))
            return 0;

        await _taskService.CreateTaskAsync(tenantId, title, "Data Quality Revenue", entityType, entityId, null,
            DateTime.UtcNow.AddDays(3), "Normal", taskType, ct);
        return 1;
    }
}
