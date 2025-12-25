using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserPasswordChangedEvent : DomainEventBase
{
    public override string EventType => "User.PasswordChanged";
    public Guid UserId { get; }

    public UserPasswordChangedEvent(Guid userId, Guid tenantId) : base(tenantId)
    {
        UserId = userId;
    }
}

