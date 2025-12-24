using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserMfaEnabledEvent : DomainEventBase
{
    public override string EventType => "User.MfaEnabled";

    public UserMfaEnabledEvent(Guid userId, Guid tenantId) : base(tenantId)
    {
    }
}

