namespace AutonomusCRM.Application.Common.Tenancy;

/// <summary>
/// Identificadores estables para tenants de QA / demo (no secretos).
/// </summary>
public static class TenantIds
{
    /// <summary>Tenant demo principal (AutonomusCRM Demo).</summary>
    public static readonly Guid QaTenantA = Guid.Parse("d7a30c86-7bb7-4303-9c1b-a0518fd78c67");

    /// <summary>Segundo tenant aislado para pruebas SaaS (QA-B).</summary>
    public static readonly Guid QaTenantB = Guid.Parse("a8f41d97-8cc8-5414-0a2c-b1629fe89d78");

}
