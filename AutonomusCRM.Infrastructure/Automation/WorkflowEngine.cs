using System.Text.Json;
using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Infrastructure.Persistence.EventStore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Automation;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWorkflowTaskRepository _workflowTaskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        IWorkflowRepository workflowRepository,
        ILeadRepository leadRepository,
        IDealRepository dealRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IWorkflowTaskRepository workflowTaskRepository,
        IUnitOfWork unitOfWork,
        ILogger<WorkflowEngine> logger)
    {
        _workflowRepository = workflowRepository;
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _workflowTaskRepository = workflowTaskRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteWorkflowsAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId == null)
            return;

        var activeWorkflows = await _workflowRepository.GetActiveByTenantAsync(domainEvent.TenantId.Value, cancellationToken);

        foreach (var workflow in activeWorkflows)
        {
            var shouldExecute = workflow.Triggers.Any(t =>
                t.Type == "DomainEvent" &&
                string.Equals(t.EventType, domainEvent.EventType, StringComparison.Ordinal));

            if (!shouldExecute)
                continue;

            if (!await EvaluateConditionsAsync(workflow, domainEvent, cancellationToken))
                continue;

            await ExecuteActionsAsync(workflow, domainEvent, cancellationToken);

            workflow.RecordExecution();
            await _workflowRepository.UpdateAsync(workflow, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Workflow {WorkflowId} executed for event {EventType}",
                workflow.Id,
                domainEvent.EventType);
        }
    }

    public Task<bool> EvaluateConditionsAsync(Workflow workflow, IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!workflow.Conditions.Any())
            return Task.FromResult(true);

        foreach (var condition in workflow.Conditions)
        {
            if (condition.Type == "EventTypeEquals" &&
                !string.Equals(condition.Expression, domainEvent.EventType, StringComparison.Ordinal))
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task ExecuteActionsAsync(Workflow workflow, IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var tenantId = domainEvent.TenantId!.Value;
        var aggregate = ResolveAggregate(domainEvent);

        foreach (var action in workflow.Actions)
        {
            _logger.LogInformation(
                "Workflow {WorkflowId} action {ActionType} target {Target}",
                workflow.Id,
                action.Type,
                action.Target);

            switch (action.Type)
            {
                case "Assign":
                    await ExecuteAssignAsync(tenantId, aggregate, action, cancellationToken);
                    break;
                case "UpdateStatus":
                    await ExecuteUpdateStatusAsync(tenantId, aggregate, action, cancellationToken);
                    break;
                case "CreateTask":
                    await ExecuteCreateTaskAsync(tenantId, workflow.Id, aggregate, action, cancellationToken);
                    break;
                case "Communicate":
                case "ActivateAgent":
                    _logger.LogInformation("Action {Type} logged for workflow {WorkflowId}", action.Type, workflow.Id);
                    break;
            }
        }
    }

    private async Task ExecuteAssignAsync(
        Guid tenantId,
        (string Type, Guid Id) aggregate,
        WorkflowAction action,
        CancellationToken cancellationToken)
    {
        if (!action.Parameters.TryGetValue("userId", out var userObj) ||
            !Guid.TryParse(userObj?.ToString(), out var userId))
            return;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.TenantId != tenantId)
            return;

        switch (aggregate.Type)
        {
            case "Lead":
                var lead = await _leadRepository.GetByIdAsync(aggregate.Id, cancellationToken);
                if (lead is null || lead.TenantId != tenantId) return;
                lead.AssignToUser(userId);
                await _leadRepository.UpdateAsync(lead, cancellationToken);
                break;
            case "Deal":
                var deal = await _dealRepository.GetByIdAsync(aggregate.Id, cancellationToken);
                if (deal is null || deal.TenantId != tenantId) return;
                deal.AssignToUser(userId);
                await _dealRepository.UpdateAsync(deal, cancellationToken);
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ExecuteUpdateStatusAsync(
        Guid tenantId,
        (string Type, Guid Id) aggregate,
        WorkflowAction action,
        CancellationToken cancellationToken)
    {
        var statusValue = action.Parameters.TryGetValue("status", out var s) ? s?.ToString() : null;
        if (string.IsNullOrEmpty(statusValue))
            return;

        switch (aggregate.Type)
        {
            case "Lead":
                var lead = await _leadRepository.GetByIdAsync(aggregate.Id, cancellationToken);
                if (lead is null || lead.TenantId != tenantId) return;
                if (Enum.TryParse<LeadStatus>(statusValue, true, out var leadStatus))
                {
                    lead.ChangeStatus(leadStatus);
                    await _leadRepository.UpdateAsync(lead, cancellationToken);
                }
                else if (string.Equals(statusValue, "Qualified", StringComparison.OrdinalIgnoreCase))
                {
                    lead.Qualify();
                    await _leadRepository.UpdateAsync(lead, cancellationToken);
                }
                break;
            case "Customer":
                var customer = await _customerRepository.GetByIdAsync(aggregate.Id, cancellationToken);
                if (customer is null || customer.TenantId != tenantId) return;
                if (Enum.TryParse<CustomerStatus>(statusValue, true, out var custStatus))
                {
                    customer.ChangeStatus(custStatus);
                    await _customerRepository.UpdateAsync(customer, cancellationToken);
                }
                break;
            case "Deal":
                var deal = await _dealRepository.GetByIdAsync(aggregate.Id, cancellationToken);
                if (deal is null || deal.TenantId != tenantId) return;
                if (Enum.TryParse<DealStage>(statusValue, true, out var stage))
                {
                    deal.UpdateStage(stage);
                    await _dealRepository.UpdateAsync(deal, cancellationToken);
                }
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ExecuteCreateTaskAsync(
        Guid tenantId,
        Guid workflowId,
        (string Type, Guid Id) aggregate,
        WorkflowAction action,
        CancellationToken cancellationToken)
    {
        var title = action.Parameters.TryGetValue("title", out var t)
            ? t?.ToString()
            : $"Tarea workflow: {action.Target}";

        var assigned = action.Parameters.TryGetValue("userId", out var u) && Guid.TryParse(u?.ToString(), out var uid)
            ? uid
            : (Guid?)null;

        DateTime? dueDate = null;
        if (action.Parameters.TryGetValue("dueDate", out var dueObj) &&
            DateTime.TryParse(dueObj?.ToString(), out var parsedDue))
            dueDate = parsedDue;

        var priority = action.Parameters.TryGetValue("priority", out var p) ? p?.ToString() ?? "Normal" : "Normal";
        var taskType = action.Parameters.TryGetValue("taskType", out var tt) ? tt?.ToString() : null;

        var task = WorkflowTask.Create(
            tenantId,
            workflowId,
            title ?? "Tarea automática",
            action.Parameters.TryGetValue("description", out var d) ? d?.ToString() : null,
            aggregate.Id,
            aggregate.Type,
            assigned,
            dueDate,
            priority,
            taskType);

        await _workflowTaskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static (string Type, Guid Id) ResolveAggregate(IDomainEvent domainEvent)
    {
        if (domainEvent is LeadCreatedEvent lce)
            return ("Lead", lce.LeadId);
        if (domainEvent is PersistedDomainEvent persisted)
        {
            try
            {
                using var doc = JsonDocument.Parse(persisted.EventData);
                if (doc.RootElement.TryGetProperty("leadId", out var leadId) && leadId.TryGetGuid(out var lid))
                    return ("Lead", lid);
                if (doc.RootElement.TryGetProperty("dealId", out var dealId) && dealId.TryGetGuid(out var did))
                    return ("Deal", did);
                if (doc.RootElement.TryGetProperty("customerId", out var custId) && custId.TryGetGuid(out var cid))
                    return ("Customer", cid);
            }
            catch { /* ignore */ }
        }

        return ("Unknown", Guid.Empty);
    }
}
