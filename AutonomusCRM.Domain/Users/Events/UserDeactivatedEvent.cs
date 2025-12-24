using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserDeactivatedEvent : DomainEventBase
{
    public override string EventType => "User.Deactivated";

    public UserDeactivatedEvent(Guid userId, Guid tenantId) : base(tenantId)
    {
    }
}

