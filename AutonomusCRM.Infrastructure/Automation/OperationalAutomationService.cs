using AutonomusCRM.Application.Automation;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Leads.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Automation;

public class OperationalAutomationService : IOperationalAutomationService
{
    private readonly ILeadRepository _leadRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IOperationalTaskService _taskService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OperationalAutomationService> _logger;

    public OperationalAutomationService(
        ILeadRepository leadRepository,
        ICustomerRepository customerRepository,
        IDealRepository dealRepository,
        IOperationalTaskService taskService,
        IServiceScopeFactory scopeFactory,
        IUnitOfWork unitOfWork,
        ILogger<OperationalAutomationService> logger)
    {
        _leadRepository = leadRepository;
        _customerRepository = customerRepository;
        _dealRepository = dealRepository;
        _taskService = taskService;
        _scopeFactory = scopeFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId == null)
            return;

        switch (domainEvent)
        {
            case LeadQualifiedEvent lqe:
                await OnLeadQualifiedAsync(lqe.TenantId.Value, lqe.LeadId, cancellationToken);
                break;
            case DealClosedEvent dce:
                await OnDealClosedAsync(dce.TenantId!.Value, dce.DealId, cancellationToken);
                break;
        }
    }

    private async Task OnLeadQualifiedAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead == null || lead.TenantId != tenantId)
            return;

        var customers = await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var customer = !string.IsNullOrWhiteSpace(lead.Email)
            ? customers.FirstOrDefault(c => string.Equals(c.Email, lead.Email, StringComparison.OrdinalIgnoreCase))
            : null;

        var customerId = customer?.Id ?? await CreateCustomerAsync(
            tenantId, lead.Name, lead.Email, lead.Phone, lead.Company, cancellationToken);

        var existingDeal = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .FirstOrDefault(d => d.Metadata.TryGetValue("LeadId", out var lid) && lid.ToString() == leadId.ToString());

        if (existingDeal == null)
        {
            var dealId = await CreateDealAsync(
                tenantId,
                customerId,
                $"Oportunidad: {lead.Name}",
                1m,
                "Borrador automático al calificar lead",
                cancellationToken);

            var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
            if (deal != null)
            {
                deal.UpdateMetadata("LeadId", leadId.ToString());
                deal.UpdateMetadata("IsDraft", true);
                if (lead.AssignedToUserId.HasValue)
                    deal.AssignToUser(lead.AssignedToUserId.Value);
                await _dealRepository.UpdateAsync(deal, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        if (!await _taskService.ExistsOpenTaskAsync(tenantId, "Lead", leadId, OperationalConstants.TaskTypeFollowUp, cancellationToken))
        {
            await _taskService.CreateTaskAsync(
                tenantId,
                $"Seguimiento lead calificado: {lead.Name}",
                "Contactar al lead en las próximas 24 horas.",
                "Lead",
                leadId,
                lead.AssignedToUserId,
                DateTime.UtcNow.AddDays(1),
                "High",
                OperationalConstants.TaskTypeFollowUp,
                cancellationToken);
        }

        _logger.LogInformation("Lead {LeadId} qualified: deal draft + follow-up task ensured", leadId);
    }

    private async Task OnDealClosedAsync(Guid tenantId, Guid dealId, CancellationToken cancellationToken)
    {
        var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
        if (deal == null || deal.TenantId != tenantId || deal.Stage != DealStage.ClosedWon)
            return;

        var schedules = new[]
        {
            (Days: 0, Title: "Onboarding CS — Día 1", Desc: "Bienvenida y kick-off con el cliente."),
            (Days: 7, Title: "Onboarding CS — Día 7", Desc: "Revisión de adopción inicial."),
            (Days: 30, Title: "Onboarding CS — Día 30", Desc: "Revisión de salud de cuenta y renovación.")
        };

        foreach (var item in schedules)
        {
            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Deal", dealId, $"Onboarding_D{item.Days}", cancellationToken))
                continue;

            await _taskService.CreateTaskAsync(
                tenantId,
                item.Title,
                item.Desc,
                "Deal",
                dealId,
                deal.AssignedToUserId,
                DateTime.UtcNow.AddDays(item.Days),
                item.Days == 0 ? "Urgent" : "Normal",
                $"Onboarding_D{item.Days}",
                cancellationToken);
        }

        _logger.LogInformation("Deal {DealId} ClosedWon: CS onboarding tasks created", dealId);
    }

    private async Task<Guid> CreateCustomerAsync(
        Guid tenantId, string name, string? email, string? phone, string? company,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<CreateCustomerCommand, Guid>>();
        return await handler.HandleAsync(
            new CreateCustomerCommand(tenantId, name, email, phone, company), cancellationToken);
    }

    private async Task<Guid> CreateDealAsync(
        Guid tenantId, Guid customerId, string title, decimal amount, string? description,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
        return await handler.HandleAsync(
            new CreateDealCommand(tenantId, customerId, title, amount, description), cancellationToken);
    }
}
