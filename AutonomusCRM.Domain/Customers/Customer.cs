using AutonomusCRM.Domain.Common;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Customers.Events;

namespace AutonomusCRM.Domain.Customers;

/// <summary>
/// Entidad Customer - Cliente del CRM
/// </summary>
public class Customer : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Company { get; private set; }
    public CustomerStatus Status { get; private set; }
    public decimal? LifetimeValue { get; private set; }
    public int? RiskScore { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public DateTime? LastContactAt { get; private set; }
    public DateTime? LastPurchaseAt { get; private set; }

    private Customer() : base()
    {
        Name = string.Empty;
        Status = CustomerStatus.Prospect;
        Metadata = new Dictionary<string, object>();
    }

    private Customer(Guid id, Guid tenantId, string name) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Status = CustomerStatus.Prospect;
        Metadata = new Dictionary<string, object>();
    }

    public static Customer Create(Guid tenantId, string name, string? email = null, string? phone = null, string? company = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del cliente no puede estar vacío", nameof(name));

        var customer = new Customer(Guid.NewGuid(), tenantId, name)
        {
            Email = email,
            Phone = phone,
            Company = company
        };

        customer.AddDomainEvent(new CustomerCreatedEvent(customer.Id, tenantId, customer.Name));
        return customer;
    }

    public void UpdateContactInfo(string? email, string? phone, string? company)
    {
        Email = email;
        Phone = phone;
        Company = company;
        MarkAsUpdated();
        AddDomainEvent(new CustomerUpdatedEvent(Id, TenantId));
    }

    public void ChangeStatus(CustomerStatus newStatus)
    {
        if (Status == newStatus)
            return;

        var oldStatus = Status;
        Status = newStatus;
        MarkAsUpdated();
        AddDomainEvent(new CustomerStatusChangedEvent(Id, TenantId, oldStatus, newStatus));
    }

    public void UpdateLifetimeValue(decimal value)
    {
        LifetimeValue = value;
        MarkAsUpdated();
        AddDomainEvent(new CustomerLifetimeValueUpdatedEvent(Id, TenantId, value));
    }

    public void UpdateRiskScore(int score)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException("El score de riesgo debe estar entre 0 y 100", nameof(score));

        RiskScore = score;
        MarkAsUpdated();
        AddDomainEvent(new CustomerRiskScoreUpdatedEvent(Id, TenantId, score));
    }

    public void RecordContact(DateTime contactDate)
    {
        LastContactAt = contactDate;
        MarkAsUpdated();
    }

    public void RecordPurchase(DateTime purchaseDate)
    {
        LastPurchaseAt = purchaseDate;
        MarkAsUpdated();
        AddDomainEvent(new CustomerPurchaseRecordedEvent(Id, TenantId, purchaseDate));
    }

    public void UpdateMetadata(string key, object value)
    {
        Metadata[key] = value;
        MarkAsUpdated();
    }
}

public enum CustomerStatus
{
    Prospect = 0,
    Lead = 1,
    Qualified = 2,
    Customer = 3,
    VIP = 4,
    Churned = 5,
    Inactive = 6
}

