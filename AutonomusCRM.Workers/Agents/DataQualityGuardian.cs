using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

/// <summary>
/// Agente autónomo que monitorea y mejora la calidad de datos
/// </summary>
public class DataQualityGuardian
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DataQualityGuardian> _logger;

    public DataQualityGuardian(
        ICustomerRepository customerRepository,
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<DataQualityGuardian> logger)
    {
        _customerRepository = customerRepository;
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ScanDataQuality(Guid tenantId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DataQualityGuardian scanning data quality for Tenant {TenantId}", tenantId);

        var issues = new List<DataQualityIssue>();

        // Escanear customers
        var customers = await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var customer in customers)
        {
            var customerIssues = ValidateCustomer(customer);
            issues.AddRange(customerIssues);
        }

        // Escanear leads
        var leads = await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var lead in leads)
        {
            var leadIssues = ValidateLead(lead);
            issues.AddRange(leadIssues);
        }

        _logger.LogInformation(
            "DataQualityGuardian found {IssueCount} data quality issues for Tenant {TenantId}",
            issues.Count,
            tenantId);

        // TODO: Crear tareas de corrección o aplicar correcciones automáticas
    }

    private List<DataQualityIssue> ValidateCustomer(Customer customer)
    {
        var issues = new List<DataQualityIssue>();

        if (string.IsNullOrWhiteSpace(customer.Email))
            issues.Add(new DataQualityIssue("Customer", customer.Id, "MissingEmail", "Email is required"));

        if (string.IsNullOrWhiteSpace(customer.Phone))
            issues.Add(new DataQualityIssue("Customer", customer.Id, "MissingPhone", "Phone is recommended"));

        if (!IsValidEmail(customer.Email))
            issues.Add(new DataQualityIssue("Customer", customer.Id, "InvalidEmail", "Email format is invalid"));

        return issues;
    }

    private List<DataQualityIssue> ValidateLead(Lead lead)
    {
        var issues = new List<DataQualityIssue>();

        if (string.IsNullOrWhiteSpace(lead.Email) && string.IsNullOrWhiteSpace(lead.Phone))
            issues.Add(new DataQualityIssue("Lead", lead.Id, "MissingContact", "Email or Phone is required"));

        if (!string.IsNullOrWhiteSpace(lead.Email) && !IsValidEmail(lead.Email))
            issues.Add(new DataQualityIssue("Lead", lead.Id, "InvalidEmail", "Email format is invalid"));

        if (!string.IsNullOrWhiteSpace(lead.Phone) && !IsValidPhone(lead.Phone))
            issues.Add(new DataQualityIssue("Lead", lead.Id, "InvalidPhone", "Phone format is invalid"));

        return issues;
    }

    private bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Validación básica: al menos 10 dígitos
        var digits = phone.Where(char.IsDigit).Count();
        return digits >= 10;
    }
}

public record DataQualityIssue(
    string EntityType,
    Guid EntityId,
    string IssueType,
    string Description
);

