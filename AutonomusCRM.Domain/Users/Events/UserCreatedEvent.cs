using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserCreatedEvent : DomainEventBase
{
    public override string EventType => "User.Created";
    public Guid UserId { get; }
    public string Email { get; }

    public UserCreatedEvent(Guid userId, Guid tenantId, string email) : base(tenantId)
    {
        UserId = userId;
        Email = email;
    }
}

