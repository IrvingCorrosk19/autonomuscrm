using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserRoleAddedEvent : DomainEventBase
{
    public override string EventType => "User.RoleAdded";
    public string Role { get; }

    public UserRoleAddedEvent(Guid userId, Guid tenantId, string role) : base(tenantId)
    {
        Role = role;
    }
}

