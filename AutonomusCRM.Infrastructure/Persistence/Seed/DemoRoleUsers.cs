namespace AutonomusCRM.Infrastructure.Persistence.Seed;

/// <summary>
/// Usuarios de demostración local: un usuario por rol del sistema.
/// </summary>
public static class DemoRoleUsers
{
    public static readonly IReadOnlyList<DemoRoleUser> All =
    [
        new("Admin", "admin@autonomuscrm.local", "Admin", "Sistema"),
        new("Manager", "manager@autonomuscrm.local", "María", "Gerente"),
        new("Sales", "sales@autonomuscrm.local", "Ana", "Ventas"),
        new("Support", "support@autonomuscrm.local", "Carlos", "Soporte"),
        new("Viewer", "viewer@autonomuscrm.local", "Laura", "Consulta")
    ];

    public static string PasswordFor(string role) => $"{role}123!";

    public record DemoRoleUser(string Role, string Email, string FirstName, string LastName);
}
