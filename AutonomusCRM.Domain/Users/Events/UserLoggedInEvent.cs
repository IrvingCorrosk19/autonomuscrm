using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserLoggedInEvent : DomainEventBase
{
    public override string EventType => "User.LoggedIn";

    public UserLoggedInEvent(Guid userId, Guid tenantId) : base(tenantId)
    {
    }
}

