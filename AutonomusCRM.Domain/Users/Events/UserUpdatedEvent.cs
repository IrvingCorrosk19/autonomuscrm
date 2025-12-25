using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserUpdatedEvent : DomainEventBase
{
    public override string EventType => "User.Updated";
    public Guid UserId { get; }

    public UserUpdatedEvent(Guid userId, Guid tenantId) : base(tenantId)
    {
        UserId = userId;
    }
}

