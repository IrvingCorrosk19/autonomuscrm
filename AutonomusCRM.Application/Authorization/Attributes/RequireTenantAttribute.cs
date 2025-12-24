using AutonomusCRM.Application.Authorization.Policies;
using Microsoft.AspNetCore.Authorization;

namespace AutonomusCRM.Application.Authorization.Attributes;

/// <summary>
/// Atributo para requerir que el usuario pertenezca al mismo tenant del recurso
/// </summary>
public class RequireTenantAttribute : AuthorizeAttribute
{
    public RequireTenantAttribute()
    {
        Policy = AuthorizationPolicies.RequireSameTenant;
    }
}

