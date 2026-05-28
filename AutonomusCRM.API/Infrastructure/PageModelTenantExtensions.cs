using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Tenants.Commands;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Infrastructure;

/// <summary>
/// Resuelve el tenant del usuario autenticado (claim TenantId) con fallback al primer tenant en BD.
/// </summary>
public static class PageModelTenantExtensions
{
    public static async Task<Guid> GetTenantIdForPageAsync(
        this PageModel page,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        if (page.User.Identity?.IsAuthenticated == true)
        {
            var claimValue = page.User.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(claimValue, out var fromClaim) && fromClaim != Guid.Empty)
                return fromClaim;
        }

        var tenantRepository = serviceProvider.GetRequiredService<ITenantRepository>();
        var tenants = await tenantRepository.GetAllAsync(cancellationToken);
        var firstTenant = tenants.FirstOrDefault();
        if (firstTenant != null)
            return firstTenant.Id;

        var createHandler = serviceProvider.GetRequiredService<IRequestHandler<CreateTenantCommand, Guid>>();
        return await createHandler.HandleAsync(
            new CreateTenantCommand("Default Tenant", "Tenant por defecto"),
            cancellationToken);
    }
}
