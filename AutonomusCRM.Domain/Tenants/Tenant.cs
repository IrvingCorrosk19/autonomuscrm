using AutonomusCRM.Domain.Common;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Tenants.Events;
using AutonomusCRM.Domain.ValueObjects;

namespace AutonomusCRM.Domain.Tenants;

/// <summary>
/// Entidad Tenant para multi-tenancy
/// </summary>
public class Tenant : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsKillSwitchEnabled { get; private set; }
    public Dictionary<string, string> Settings { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }

    private Tenant() : base()
    {
        Name = string.Empty;
        Settings = new Dictionary<string, string>();
        IsActive = true;
        IsKillSwitchEnabled = false;
    }

    private Tenant(Guid id, string name, string? description = null) : base(id)
    {
        Name = name;
        Description = description;
        Settings = new Dictionary<string, string>();
        IsActive = true;
        IsKillSwitchEnabled = false;
    }

    public static Tenant Create(string name, string? description = null)
    {
        var tenant = new Tenant(Guid.NewGuid(), name, description);
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.Name));
        return tenant;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del tenant no puede estar vacío", nameof(name));

        Name = name;
        MarkAsUpdated();
        AddDomainEvent(new TenantUpdatedEvent(Id, name));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        MarkAsUpdated();
        AddDomainEvent(new TenantActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        MarkAsUpdated();
        AddDomainEvent(new TenantDeactivatedEvent(Id));
    }

    public void EnableKillSwitch()
    {
        if (IsKillSwitchEnabled)
            return;

        IsKillSwitchEnabled = true;
        IsActive = false;
        MarkAsUpdated();
        AddDomainEvent(new TenantKillSwitchEnabledEvent(Id));
    }

    public void DisableKillSwitch()
    {
        if (!IsKillSwitchEnabled)
            return;

        IsKillSwitchEnabled = false;
        MarkAsUpdated();
        AddDomainEvent(new TenantKillSwitchDisabledEvent(Id));
    }

    public void UpdateSetting(string key, string value)
    {
        Settings[key] = value;
        MarkAsUpdated();
    }
}

