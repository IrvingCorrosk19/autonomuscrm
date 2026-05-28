using System.Collections.Frozen;
using System.Text.Json;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Domain.Tenants.Events;
using AutonomusCRM.Domain.Users.Events;

namespace AutonomusCRM.Infrastructure.Persistence.EventStore;

internal static class DomainEventTypeRegistry
{
    internal static IReadOnlyDictionary<string, Type> GetRegisteredTypes() => Map;

    private static readonly FrozenDictionary<string, Type> Map = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["Lead.Created"] = typeof(LeadCreatedEvent),
        ["Lead.Updated"] = typeof(LeadUpdatedEvent),
        ["Lead.Qualified"] = typeof(LeadQualifiedEvent),
        ["Lead.Assigned"] = typeof(LeadAssignedEvent),
        ["Lead.ScoreUpdated"] = typeof(LeadScoreUpdatedEvent),
        ["Lead.StatusChanged"] = typeof(LeadStatusChangedEvent),
        ["Lead.ConvertedToCustomer"] = typeof(LeadConvertedToCustomerEvent),
        ["Customer.Created"] = typeof(CustomerCreatedEvent),
        ["Customer.Updated"] = typeof(CustomerUpdatedEvent),
        ["Customer.StatusChanged"] = typeof(CustomerStatusChangedEvent),
        ["Customer.LifetimeValueUpdated"] = typeof(CustomerLifetimeValueUpdatedEvent),
        ["Customer.RiskScoreUpdated"] = typeof(CustomerRiskScoreUpdatedEvent),
        ["Customer.PurchaseRecorded"] = typeof(CustomerPurchaseRecordedEvent),
        ["Deal.Created"] = typeof(DealCreatedEvent),
        ["Deal.Updated"] = typeof(DealUpdatedEvent),
        ["Deal.StageChanged"] = typeof(DealStageChangedEvent),
        ["Deal.ProbabilityUpdated"] = typeof(DealProbabilityUpdatedEvent),
        ["Deal.AmountUpdated"] = typeof(DealAmountUpdatedEvent),
        ["Deal.Closed"] = typeof(DealClosedEvent),
        ["Deal.Lost"] = typeof(DealLostEvent),
        ["Deal.Assigned"] = typeof(DealAssignedEvent),
        ["User.Created"] = typeof(UserCreatedEvent),
        ["User.Updated"] = typeof(UserUpdatedEvent),
        ["User.Activated"] = typeof(UserActivatedEvent),
        ["User.Deactivated"] = typeof(UserDeactivatedEvent),
        ["User.LoggedIn"] = typeof(UserLoggedInEvent),
        ["User.MfaEnabled"] = typeof(UserMfaEnabledEvent),
        ["User.MfaDisabled"] = typeof(UserMfaDisabledEvent),
        ["User.PasswordChanged"] = typeof(UserPasswordChangedEvent),
        ["User.RoleAdded"] = typeof(UserRoleAddedEvent),
        ["User.RoleRemoved"] = typeof(UserRoleRemovedEvent),
        ["Tenant.Created"] = typeof(TenantCreatedEvent),
        ["Tenant.Updated"] = typeof(TenantUpdatedEvent),
        ["Tenant.Activated"] = typeof(TenantActivatedEvent),
        ["Tenant.Deactivated"] = typeof(TenantDeactivatedEvent),
        ["Tenant.KillSwitchEnabled"] = typeof(TenantKillSwitchEnabledEvent),
        ["Tenant.KillSwitchDisabled"] = typeof(TenantKillSwitchDisabledEvent),
    }.ToFrozenDictionary();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryDeserialize(string eventType, string eventData, out IDomainEvent domainEvent)
    {
        domainEvent = null!;
        if (!Map.TryGetValue(eventType, out var clrType))
            return false;

        try
        {
            var deserialized = JsonSerializer.Deserialize(eventData, clrType, JsonOptions);
            if (deserialized is IDomainEvent evt)
            {
                domainEvent = evt;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
