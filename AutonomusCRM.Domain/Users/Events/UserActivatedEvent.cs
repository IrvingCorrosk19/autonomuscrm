using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserActivatedEvent : DomainEventBase
{
    public override string EventType => "User.Activated";
    public Guid UserId { get; }

    public UserActivatedEvent(Guid userId, Guid tenantId) : base(tenantId)
    {
        UserId = userId;
    }
}

