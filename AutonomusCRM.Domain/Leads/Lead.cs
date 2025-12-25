using AutonomusCRM.Domain.Common;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads.Events;

namespace AutonomusCRM.Domain.Leads;

/// <summary>
/// Entidad Lead - Prospecto potencial
/// </summary>
public class Lead : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Company { get; private set; }
    public LeadStatus Status { get; private set; }
    public LeadSource Source { get; private set; }
    public int? Score { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? QualifiedAt { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private Lead() : base()
    {
        Name = string.Empty;
        Status = LeadStatus.New;
        Source = LeadSource.Unknown;
        Metadata = new Dictionary<string, object>();
    }

    private Lead(Guid id, Guid tenantId, string name, LeadSource source) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Source = source;
        Status = LeadStatus.New;
        Metadata = new Dictionary<string, object>();
    }

    public static Lead Create(Guid tenantId, string name, LeadSource source, string? email = null, string? phone = null, string? company = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del lead no puede estar vacío", nameof(name));

        var lead = new Lead(Guid.NewGuid(), tenantId, name, source)
        {
            Email = email,
            Phone = phone,
            Company = company
        };

        lead.AddDomainEvent(new LeadCreatedEvent(lead.Id, tenantId, lead.Name, source));
        return lead;
    }

    public void UpdateScore(int score)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException("El score del lead debe estar entre 0 y 100", nameof(score));

        Score = score;
        MarkAsUpdated();
        AddDomainEvent(new LeadScoreUpdatedEvent(Id, TenantId, score));
    }

    public void AssignToUser(Guid userId)
    {
        AssignedToUserId = userId;
        MarkAsUpdated();
        AddDomainEvent(new LeadAssignedEvent(Id, TenantId, userId));
    }

    public void Qualify()
    {
        if (Status == LeadStatus.Qualified)
            return;

        Status = LeadStatus.Qualified;
        QualifiedAt = DateTime.UtcNow;
        MarkAsUpdated();
        AddDomainEvent(new LeadQualifiedEvent(Id, TenantId));
    }

    public void ChangeStatus(LeadStatus newStatus)
    {
        if (Status == newStatus)
            return;

        var oldStatus = Status;
        Status = newStatus;
        MarkAsUpdated();
        AddDomainEvent(new LeadStatusChangedEvent(Id, TenantId, oldStatus, newStatus));
    }

    public void ConvertToCustomer(Guid customerId)
    {
        Status = LeadStatus.Converted;
        MarkAsUpdated();
        AddDomainEvent(new LeadConvertedToCustomerEvent(Id, TenantId, customerId));
    }

    public void UpdateInfo(string name, string? email, string? phone, string? company, LeadSource source)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del lead no puede estar vacío", nameof(name));

        Name = name;
        Email = email;
        Phone = phone;
        Company = company;
        Source = source;
        MarkAsUpdated();
        AddDomainEvent(new LeadUpdatedEvent(Id, TenantId));
    }
}

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Converted = 3,
    Lost = 4,
    Unqualified = 5
}

public enum LeadSource
{
    Unknown = 0,
    Website = 1,
    Referral = 2,
    SocialMedia = 3,
    EmailCampaign = 4,
    ColdCall = 5,
    Partner = 6,
    Event = 7,
    Other = 99
}

