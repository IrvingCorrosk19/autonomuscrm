using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public sealed class CustomerSuccessOsService : ICustomerSuccessOsService
{
    private readonly ApplicationDbContext _db;
    private readonly IOperationalTaskService _tasks;
    private readonly ICustomerKpiService _kpis;
    private readonly ICustomerHealthEngine _health;
    private readonly IChurnRiskEngine _churn;
    private readonly IRenewalEngine _renewal;
    private readonly IExpansionRevenueEngine _expansion;
    private readonly ICustomerPlaybookService _playbooks;
    private readonly IWorkflowTaskRepository _taskRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerSuccessOsService(
        ApplicationDbContext db,
        IOperationalTaskService tasks,
        ICustomerKpiService kpis,
        ICustomerHealthEngine health,
        IChurnRiskEngine churn,
        IRenewalEngine renewal,
        IExpansionRevenueEngine expansion,
        ICustomerPlaybookService playbooks,
        IWorkflowTaskRepository taskRepo,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _tasks = tasks;
        _kpis = kpis;
        _health = health;
        _churn = churn;
        _renewal = renewal;
        _expansion = expansion;
        _playbooks = playbooks;
        _taskRepo = taskRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerSuccessHomeDto> GetHomeAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var names = await CustomerNameMapAsync(tenantId, cancellationToken);
        var tickets = await LoadTicketsAsync(tenantId, names, cancellationToken);
        var cases = await LoadCasesAsync(tenantId, names, openOnly: false, take: 30, cancellationToken);

        var openTickets = tickets.Where(t => t.Status == "Open").ToList();
        var closedTickets = tickets.Where(t => t.Status == "Completed").Take(10).ToList();
        var openCases = cases.Where(c => c.Status == "Open").ToList();

        return new CustomerSuccessHomeDto(
            await _kpis.GetSnapshotAsync(tenantId, cancellationToken),
            (await _churn.DetectSignalsAsync(tenantId, cancellationToken: cancellationToken)).Take(15).ToList(),
            (await _renewal.GetUpcomingRenewalsAsync(tenantId, cancellationToken)).Take(20).ToList(),
            openTickets,
            closedTickets,
            (await _expansion.DetectOpportunitiesAsync(tenantId, cancellationToken)).Take(15).ToList(),
            openCases,
            (await _health.CalculateAllAsync(tenantId, cancellationToken)).Take(20).ToList(),
            openTickets.Count,
            openCases.Count);
    }

    public async Task<Customer360CsPanelDto> GetCustomerPanelAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var names = await CustomerNameMapAsync(tenantId, cancellationToken);
        var allTickets = await LoadTicketsAsync(tenantId, names, cancellationToken, customerId);
        var cases = await LoadCasesAsync(tenantId, names, openOnly: false, take: 10, cancellationToken, customerId);

        return new Customer360CsPanelDto(
            allTickets.Where(t => t.Status == "Open").ToList(),
            allTickets.Where(t => t.Status == "Completed").Take(5).ToList(),
            cases.Take(5).ToList());
    }

    public async Task<CsTicketDto> CreateTicketAsync(
        Guid tenantId, Guid customerId, string subject, string? description, string priority,
        Guid? assignedToUserId, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.CreateTaskAsync(
            tenantId, subject, description, "Customer", customerId, assignedToUserId,
            DateTime.UtcNow.AddDays(3), priority, CustomerSuccessOsConstants.Ticket, cancellationToken);
        var name = await GetCustomerNameAsync(tenantId, customerId, cancellationToken);
        return ToTicket(task, name);
    }

    public async Task<CsCaseDto> CreateCaseAsync(
        Guid tenantId, Guid customerId, string caseType, string title, string? description, string priority,
        Guid? assignedToUserId, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCaseType(caseType);
        var task = await _tasks.CreateTaskAsync(
            tenantId, title, description, "Customer", customerId, assignedToUserId,
            DateTime.UtcNow.AddDays(7), priority, normalized, cancellationToken);
        var name = await GetCustomerNameAsync(tenantId, customerId, cancellationToken);
        return ToCase(task, name);
    }

    public async Task<bool> CloseTicketAsync(Guid tenantId, Guid ticketId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepo.GetByIdAsync(ticketId, cancellationToken);
        if (task == null || task.TenantId != tenantId || !CustomerSuccessOsConstants.IsTicket(task.TaskType))
            return false;
        task.Complete();
        await _taskRepo.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<PlaybookExecutionDto> RunPlaybookAsync(
        Guid tenantId, Guid customerId, string playbookType, Guid? assignedToUserId, CancellationToken cancellationToken = default)
    {
        var normalized = playbookType == CustomerSuccessOsConstants.PlaybookAtRisk
            ? CustomerSuccessConstants.PlaybookRescue
            : playbookType;
        return _playbooks.ExecutePlaybookAsync(tenantId, customerId, normalized, assignedToUserId, cancellationToken);
    }

    private async Task<List<CsTicketDto>> LoadTicketsAsync(
        Guid tenantId, Dictionary<Guid, string> names, CancellationToken cancellationToken, Guid? customerId = null)
    {
        var query = _db.WorkflowTasks.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.TaskType == CustomerSuccessOsConstants.Ticket);
        if (customerId.HasValue)
            query = query.Where(t => t.RelatedEntityId == customerId.Value);
        var tasks = await query.OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync(cancellationToken);
        return tasks.Select(t => ToTicket(t, names.GetValueOrDefault(t.RelatedEntityId ?? Guid.Empty, "Cliente"))).ToList();
    }

    private async Task<List<CsCaseDto>> LoadCasesAsync(
        Guid tenantId, Dictionary<Guid, string> names, bool openOnly, int take,
        CancellationToken cancellationToken, Guid? customerId = null)
    {
        var query = _db.WorkflowTasks.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.TaskType != null && t.TaskType.StartsWith("CS_Case_"));
        if (openOnly)
            query = query.Where(t => t.Status == "Open");
        if (customerId.HasValue)
            query = query.Where(t => t.RelatedEntityId == customerId.Value);
        var tasks = await query.OrderByDescending(t => t.CreatedAt).Take(take).ToListAsync(cancellationToken);
        return tasks.Select(t => ToCase(t, names.GetValueOrDefault(t.RelatedEntityId ?? Guid.Empty, "Cliente"))).ToList();
    }

    private async Task<Dictionary<Guid, string>> CustomerNameMapAsync(Guid tenantId, CancellationToken cancellationToken)
        => await _db.Customers.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

    private async Task<string> GetCustomerNameAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        var name = await _db.Customers.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Id == customerId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);
        return name ?? "Cliente";
    }

    private static CsTicketDto ToTicket(WorkflowTask t, string customerName) => new(
        t.Id,
        t.RelatedEntityId ?? Guid.Empty,
        customerName,
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.DueDate,
        t.CreatedAt,
        t.IsOverdue);

    private static CsCaseDto ToCase(WorkflowTask t, string customerName) => new(
        t.Id,
        t.RelatedEntityId ?? Guid.Empty,
        customerName,
        t.TaskType ?? CustomerSuccessOsConstants.CaseAtRisk,
        CustomerSuccessOsConstants.CaseLabel(t.TaskType),
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.CreatedAt);

    private static string NormalizeCaseType(string caseType) => caseType switch
    {
        "Renewal" or CustomerSuccessOsConstants.CaseRenewal => CustomerSuccessOsConstants.CaseRenewal,
        "Recovery" or CustomerSuccessOsConstants.CaseRecovery => CustomerSuccessOsConstants.CaseRecovery,
        "Expansion" or CustomerSuccessOsConstants.CaseExpansion => CustomerSuccessOsConstants.CaseExpansion,
        "AtRisk" or CustomerSuccessOsConstants.CaseAtRisk => CustomerSuccessOsConstants.CaseAtRisk,
        _ => CustomerSuccessOsConstants.CaseAtRisk
    };
}
