using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using AutonomusCRM.Application.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/provisioning")]
public class ProvisioningController : ControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ProvisioningController(IStringLocalizer<SharedResource> localizer) => _localizer = localizer;

    /// <summary>Onboard a new tenant with admin user (platform key or admin JWT).</summary>
    [HttpPost("tenants")]
    [AllowAnonymous]
    public async Task<ActionResult<ProvisionTenantResponse>> ProvisionTenant(
        [FromBody] ProvisionTenantRequest request,
        CancellationToken cancellationToken)
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var platformKey = config["Provisioning:ApiKey"];
        if (!string.IsNullOrWhiteSpace(platformKey))
        {
            if (!Request.Headers.TryGetValue("X-Platform-Key", out var key) || key != platformKey)
                return Unauthorized(ApiLocalization.Error(_localizer, "Api_Error_InvalidPlatformKey"));
        }
        else if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Unauthorized(ApiLocalization.Error(_localizer, "Api_Error_ProvisioningAuth"));
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.AdminEmail)
            || string.IsNullOrWhiteSpace(request.AdminPassword))
            return BadRequest(ApiLocalization.Error(_localizer, "Api_Error_ProvisioningRequired"));

        var svc = HttpContext.RequestServices.GetRequiredService<ITenantProvisioningService>();
        var tenantId = await svc.ProvisionTenantAsync(
            request.Name.Trim(),
            request.Description,
            request.AdminEmail.Trim(),
            request.AdminPassword,
            cancellationToken);

        return CreatedAtAction(nameof(ProvisionTenant), new ProvisionTenantResponse(tenantId, request.Name));
    }

    public sealed record ProvisionTenantRequest(string Name, string AdminEmail, string AdminPassword, string? Description);
    public sealed record ProvisionTenantResponse(Guid TenantId, string Name);
}
