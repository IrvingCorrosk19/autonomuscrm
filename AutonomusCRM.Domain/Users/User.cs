using AutonomusCRM.Domain.Common;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Users.Events;

namespace AutonomusCRM.Domain.Users;

/// <summary>
/// Entidad User para autenticación y autorización
/// </summary>
public class User : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? MfaSecret { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public List<string> Roles { get; private set; }
    public Dictionary<string, string> Claims { get; private set; }

    private User() : base()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        Roles = new List<string>();
        Claims = new Dictionary<string, string>();
        IsActive = true;
    }

    private User(Guid id, Guid tenantId, string email, string passwordHash) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        Roles = new List<string>();
        Claims = new Dictionary<string, string>();
        IsActive = true;
    }

    public static User Create(Guid tenantId, string email, string passwordHash, string? firstName = null, string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email no puede estar vacío", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña no puede estar vacío", nameof(passwordHash));

        var user = new User(Guid.NewGuid(), tenantId, email, passwordHash)
        {
            FirstName = firstName,
            LastName = lastName
        };

        user.AddDomainEvent(new UserCreatedEvent(user.Id, tenantId, email));
        return user;
    }

    public void EnableMfa(string mfaSecret)
    {
        if (string.IsNullOrWhiteSpace(mfaSecret))
            throw new ArgumentException("El secreto MFA no puede estar vacío", nameof(mfaSecret));

        MfaEnabled = true;
        MfaSecret = mfaSecret;
        MarkAsUpdated();
        AddDomainEvent(new UserMfaEnabledEvent(Id, TenantId));
    }

    public void DisableMfa()
    {
        if (!MfaEnabled)
            return;

        MfaEnabled = false;
        MfaSecret = null;
        MarkAsUpdated();
        AddDomainEvent(new UserMfaDisabledEvent(Id, TenantId));
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        MarkAsUpdated();
        AddDomainEvent(new UserLoggedInEvent(Id, TenantId));
    }

    public void AddRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("El rol no puede estar vacío", nameof(role));

        if (!Roles.Contains(role))
        {
            Roles.Add(role);
            MarkAsUpdated();
            AddDomainEvent(new UserRoleAddedEvent(Id, TenantId, role));
        }
    }

    public void RemoveRole(string role)
    {
        if (Roles.Remove(role))
        {
            MarkAsUpdated();
            AddDomainEvent(new UserRoleRemovedEvent(Id, TenantId, role));
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        MarkAsUpdated();
        AddDomainEvent(new UserDeactivatedEvent(Id, TenantId));
    }
}

