using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Users.Events;

public class UserRoleRemovedEvent : DomainEventBase
{
    public override string EventType => "User.RoleRemoved";
    public string Role { get; }

    public UserRoleRemovedEvent(Guid userId, Guid tenantId, string role) : base(tenantId)
    {
        Role = role;
    }
}

