using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.CustomerSuccess;

public interface ICustomerHealthEngine
{
    Task<CustomerHealthDto> CalculateHealthAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerHealthDto>> CalculateAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task PersistHealthAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}

public interface IChurnRiskEngine
{
    Task<IReadOnlyList<ChurnRiskSignalDto>> DetectSignalsAsync(Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default);
    Task<int> EnforceAlertsAndPlaybooksAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IRenewalEngine
{
    Task<IReadOnlyList<RenewalAlertDto>> GetUpcomingRenewalsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<RenewalForecastDto> GetRenewalForecastAsync(Guid tenantId, int horizonDays = 90, CancellationToken cancellationToken = default);
    Task<int> EnforceRenewalWindowsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICustomerPlaybookService
{
    Task<PlaybookExecutionDto> ExecutePlaybookAsync(
        Guid tenantId,
        Guid customerId,
        string playbookType,
        Guid? assignedToUserId = null,
        CancellationToken cancellationToken = default);
}

public interface IEmailAutomationEngine
{
    Task<CommunicationSendResultDto> SendTemplatedAsync(
        Guid tenantId,
        string eventType,
        string templateKey,
        string recipient,
        Guid? customerId = null,
        Guid? leadId = null,
        IReadOnlyDictionary<string, string>? variables = null,
        CancellationToken cancellationToken = default);
}

public interface IWhatsAppAutomationEngine
{
    Task<CommunicationSendResultDto> SendTemplatedAsync(
        Guid tenantId,
        string eventType,
        string templateKey,
        string recipientPhone,
        Guid? customerId = null,
        IReadOnlyDictionary<string, string>? variables = null,
        CancellationToken cancellationToken = default);
}

public interface ICustomerJourneyEngine
{
    Task<IReadOnlyList<JourneyStageMetricDto>> GetJourneyMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IExpansionRevenueEngine
{
    Task<IReadOnlyList<ExpansionOpportunityDto>> DetectOpportunitiesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> CreateExpansionTasksAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICustomerSuccessIntelligenceService
{
    Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunHealthIntelligenceAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunChurnIntelligenceAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunRenewalIntelligenceAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerIntelligenceActionDto>> RunExpansionIntelligenceAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
}

public interface IRetentionAutomationEngine
{
    Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task RunPeriodicRetentionScanAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICustomerKpiService
{
    Task<CustomerKpiSnapshotDto> GetSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IExecutiveCustomerDashboardService
{
    Task<ExecutiveCustomerDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICustomerContractRepository : Common.Interfaces.IRepository<CustomerContract>
{
    Task<IEnumerable<CustomerContract>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerContract>> GetActiveByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerContract>> GetRenewingWithinDaysAsync(Guid tenantId, int days, CancellationToken cancellationToken = default);
}

public interface ICustomerCommunicationRepository : Common.Interfaces.IRepository<CustomerCommunicationLog>
{
    Task<IEnumerable<CustomerCommunicationLog>> GetByTenantAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default);
}
