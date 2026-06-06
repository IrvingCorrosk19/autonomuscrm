using System.Text.Json;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using AutonomusCRM.Application.Auth;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.EnterpriseAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/enterprise")]
public class EnterpriseAuthController : ControllerBase
{
    private readonly EnterpriseAuthOptions _options;
    private readonly IScimUserService _scim;
    private readonly IScimGroupService _groups;
    private readonly ISamlMetadataService _saml;
    private readonly ISamlAuthService _samlAuth;
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly ITokenService _tokens;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EnterpriseAuthController(
        IOptions<EnterpriseAuthOptions> options,
        IScimUserService scim,
        IScimGroupService groups,
        ISamlMetadataService saml,
        ISamlAuthService samlAuth,
        IUserRepository users,
        ITenantRepository tenants,
        ITokenService tokens,
        ICurrentTenantAccessor tenantAccessor,
        IStringLocalizer<SharedResource> localizer)
    {
        _options = options.Value;
        _scim = scim;
        _groups = groups;
        _saml = saml;
        _samlAuth = samlAuth;
        _users = users;
        _tenants = tenants;
        _tokens = tokens;
        _tenantAccessor = tenantAccessor;
        _localizer = localizer;
    }

    [HttpGet("saml/metadata")]
    [AllowAnonymous]
    public IActionResult SamlMetadata()
    {
        var acs = $"{Request.Scheme}://{Request.Host}/api/enterprise/saml/acs";
        return Content(_saml.GetServiceProviderMetadataXml(acs), "application/xml");
    }

    /// <summary>Assertion Consumer Service — compatible con IdP que POSTean SAMLResponse (Okta, Azure AD, Keycloak).</summary>
    [HttpPost("saml/acs")]
    [AllowAnonymous]
    public async Task<IActionResult> SamlAcs([FromForm] string SAMLResponse, CancellationToken cancellationToken)
    {
        if (!_samlAuth.IsAcsConfigured)
            return BadRequest(ApiLocalization.Error(_localizer, "Api_Error_SamlNotConfigured"));

        var parsed = _samlAuth.ParseAssertion(SAMLResponse);
        if (!parsed.Success || string.IsNullOrWhiteSpace(parsed.Email))
            return BadRequest(ApiLocalization.Error(_localizer, parsed.Error ?? "Api_Error_InvalidSamlAssertion"));

        var user = await ResolveUserByEmailAsync(parsed.Email, parsed.TenantId, cancellationToken);
        if (user is null)
            return Unauthorized(ApiLocalization.Error(_localizer, "Api_Error_SamlUserNotProvisioned"));

        var principal = _tokens.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        return Redirect("/");
    }

    [HttpGet("saml/logout")]
    [Authorize]
    public async Task<IActionResult> SamlLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/Account/Login");
    }

    [HttpGet("soc2/checklist")]
    public IActionResult Soc2Checklist() => Ok(_saml.GetSoc2TechnicalChecklist());

    [HttpGet("auth/status")]
    [AllowAnonymous]
    public IActionResult Status() => Ok(new
    {
        ssoEnabled = _options.Enabled,
        oidc = !string.IsNullOrWhiteSpace(_options.OidcAuthority),
        saml = !string.IsNullOrWhiteSpace(_options.SamlEntityId),
        scim = !string.IsNullOrWhiteSpace(_options.ScimBearerToken)
    });

    [HttpPost("scim/v2/Users")]
    [AllowAnonymous]
    public async Task<IActionResult> ScimCreateUser([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!ValidateScimAuth()) return Unauthorized();
        var tenantId = GetScimTenantId(body);
        var req = ParseScimUser(body);
        var user = await _scim.CreateUserAsync(tenantId, req, cancellationToken);
        return Created($"/api/enterprise/scim/v2/Users/{user.Id}", ToScimJson(user));
    }

    [HttpGet("scim/v2/Users/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ScimGetUser(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!ValidateScimAuth()) return Unauthorized();
        var user = await _scim.GetUserAsync(tenantId, id, cancellationToken);
        return user == null ? NotFound() : Ok(ToScimJson(user));
    }

    [HttpPut("scim/v2/Users/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ScimReplaceUser(Guid id, [FromQuery] Guid tenantId, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!ValidateScimAuth()) return Unauthorized();
        var user = await _scim.UpdateUserAsync(tenantId, id, ParseScimUser(body), cancellationToken);
        return Ok(ToScimJson(user));
    }

    [HttpPatch("scim/v2/Users/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ScimPatchUser(Guid id, [FromQuery] Guid tenantId, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!ValidateScimAuth()) return Unauthorized();
        if (body.TryGetProperty("active", out var active) && active.ValueKind == JsonValueKind.False)
        {
            await _scim.DeactivateUserAsync(tenantId, id, cancellationToken);
            return NoContent();
        }

        var user = await _scim.UpdateUserAsync(tenantId, id, ParseScimUser(body), cancellationToken);
        return Ok(ToScimJson(user));
    }

    [HttpDelete("scim/v2/Users/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ScimDeleteUser(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!ValidateScimAuth()) return Unauthorized();
        await _scim.DeactivateUserAsync(tenantId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("scim/v2/Groups")]
    [AllowAnonymous]
    public async Task<IActionResult> ScimCreateGroup([FromQuery] Guid tenantId, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!ValidateScimAuth()) return Unauthorized();
        var name = body.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "Group" : "Group";
        var members = new List<string>();
        if (body.TryGetProperty("members", out var mem) && mem.ValueKind == JsonValueKind.Array)
        {
            foreach (var m in mem.EnumerateArray())
                if (m.TryGetProperty("value", out var v)) members.Add(v.GetString() ?? "");
        }

        var group = await _groups.CreateGroupAsync(tenantId, new ScimGroupRequest(name, members), cancellationToken);
        return Created($"/api/enterprise/scim/v2/Groups/{group.Id}", new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" }, id = group.Id, displayName = group.DisplayName, members = group.Members });
    }

    [HttpGet("scim/v2/Groups")]
    [AllowAnonymous]
    public async Task<IActionResult> ScimListGroups([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!ValidateScimAuth()) return Unauthorized();
        var list = await _groups.ListGroupsAsync(tenantId, cancellationToken);
        return Ok(new { Resources = list.Select(g => new { id = g.Id, displayName = g.DisplayName, members = g.Members }) });
    }

    private async Task<Domain.Users.User?> ResolveUserByEmailAsync(
        string email, Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId is Guid tid && tid != Guid.Empty)
        {
            _tenantAccessor.TenantId = tid;
            return await _users.GetByEmailAsync(tid, email, cancellationToken);
        }

        var previousBypass = _tenantAccessor.BypassTenantFilter;
        try
        {
            _tenantAccessor.BypassTenantFilter = true;
            foreach (var tenant in await _tenants.GetAllAsync(cancellationToken))
            {
                _tenantAccessor.TenantId = tenant.Id;
                var user = await _users.GetByEmailAsync(tenant.Id, email, cancellationToken);
                if (user is not null)
                    return user;
            }
        }
        finally
        {
            _tenantAccessor.BypassTenantFilter = previousBypass;
        }

        return null;
    }

    private bool ValidateScimAuth()
    {
        if (string.IsNullOrWhiteSpace(_options.ScimBearerToken)) return false;
        return Request.Headers.TryGetValue("Authorization", out var auth)
            && auth.ToString() == $"Bearer {_options.ScimBearerToken}";
    }

    private static Guid GetScimTenantId(JsonElement body)
    {
        if (body.TryGetProperty("tenantId", out var t) && t.TryGetGuid(out var g)) return g;
        throw new InvalidOperationException("tenantId required in SCIM payload for multi-tenant provisioning");
    }

    private static ScimUserRequest ParseScimUser(JsonElement body)
    {
        var userName = body.TryGetProperty("userName", out var u) ? u.GetString() ?? "" : "";
        var active = !body.TryGetProperty("active", out var a) || a.ValueKind != JsonValueKind.False;
        string? given = null, family = null;
        if (body.TryGetProperty("name", out var name))
        {
            if (name.TryGetProperty("givenName", out var g)) given = g.GetString();
            if (name.TryGetProperty("familyName", out var f)) family = f.GetString();
        }

        var roles = new List<string>();
        if (body.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in rolesEl.EnumerateArray())
                if (r.TryGetProperty("value", out var v)) roles.Add(v.GetString() ?? "Viewer");
        }

        return new ScimUserRequest(userName, active, given, family, roles);
    }

    private static object ToScimJson(ScimUserResponse user) => new
    {
        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
        id = user.Id,
        userName = user.UserName,
        active = user.Active,
        name = new { givenName = user.GivenName, familyName = user.FamilyName },
        roles = user.Roles.Select(r => new { value = r }).ToArray()
    };
}
