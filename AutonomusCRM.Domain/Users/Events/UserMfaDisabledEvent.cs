using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserMfaDisabledEvent : DomainEventBase
{
    public override string EventType => "User.MfaDisabled";

    public UserMfaDisabledEvent(Guid userId, Guid tenantId) : base(tenantId)
    {
    }
}

