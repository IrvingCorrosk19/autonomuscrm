using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;
using AutonomusCRM.Application.Auth;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using AutonomusCRM.Application.EnterpriseAuth;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace AutonomusCRM.API.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IRequestHandler<LoginCommand, LoginResult> _loginHandler;
    private readonly IRequestHandler<VerifyMfaCommand, LoginResult> _verifyMfaHandler;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly EnterpriseAuthOptions _enterpriseAuth;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LoginModel(
        IRequestHandler<LoginCommand, LoginResult> loginHandler,
        IRequestHandler<VerifyMfaCommand, LoginResult> verifyMfaHandler,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITokenService tokenService,
        IConfiguration configuration,
        ICurrentTenantAccessor tenantAccessor,
        IOptions<EnterpriseAuthOptions> enterpriseAuth,
        IStringLocalizer<SharedResource> localizer)
    {
        _loginHandler = loginHandler;
        _verifyMfaHandler = verifyMfaHandler;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tokenService = tokenService;
        _configuration = configuration;
        _tenantAccessor = tenantAccessor;
        _enterpriseAuth = enterpriseAuth.Value;
        _localizer = localizer;
    }

    public bool SsoEnabled =>
        _enterpriseAuth.Enabled
        && !string.IsNullOrWhiteSpace(_enterpriseAuth.OidcAuthority)
        && !string.IsNullOrWhiteSpace(_enterpriseAuth.OidcClientId);

    public IActionResult OnGetExternalLogin(string? returnUrl = null)
    {
        if (!SsoEnabled)
            return RedirectToPage();

        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [BindProperty]
    public Guid TenantId { get; set; }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string? MfaCode { get; set; }

    [BindProperty]
    public string? MfaTempToken { get; set; }

    public bool ShowMfaStep { get; set; }

    public string? ErrorMessage { get; set; }
    public string? DemoTenantName { get; set; }
    public string DemoEmail { get; set; } = "admin@autonomuscrm.local";
    public string DemoPassword { get; set; } = "Admin123!";
    public IReadOnlyList<DemoLoginAccount> DemoAccounts { get; set; } = Array.Empty<DemoLoginAccount>();
    public bool ShowDemoAccounts { get; private set; }
    public bool ShowTenantField { get; private set; }

    public record DemoLoginAccount(string Role, string Email, string Password);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(RoleHomeRedirect.GetHomePath(User));

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        ShowDemoAccounts = _configuration.GetValue("Seed:Enabled", false)
            && !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
        ShowTenantField = !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

        DemoEmail = _configuration["Seed:AdminEmail"] ?? DemoEmail;
        DemoPassword = _configuration["Seed:AdminPassword"] ?? DemoPassword;

        _tenantAccessor.BypassTenantFilter = true;
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        _tenantAccessor.BypassTenantFilter = false;
        var gmg = tenants.FirstOrDefault(t => t.Name == GlobalManufacturingDemoSeeder.TenantName);
        var ceoDemo = tenants.FirstOrDefault(t => t.Name == CeoDemoSeeder.TenantName);
        var first = gmg ?? ceoDemo ?? tenants.FirstOrDefault();
        if (first is not null)
        {
            TenantId = first.Id;
            DemoTenantName = first.Name;
        }

        LoadDemoAccounts();
        return Page();
    }

    [EnableRateLimiting("login")]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        ShowDemoAccounts = _configuration.GetValue("Seed:Enabled", false)
            && !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
        ShowTenantField = !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

        DemoEmail = _configuration["Seed:AdminEmail"] ?? DemoEmail;
        DemoPassword = _configuration["Seed:AdminPassword"] ?? DemoPassword;
        _tenantAccessor.TenantId = TenantId;

        try
        {
            var result = await _loginHandler.HandleAsync(
                new LoginCommand(Email, Password, TenantId),
                cancellationToken);

            if (result.RequiresMfa)
            {
                ShowMfaStep = true;
                MfaTempToken = result.AccessToken;
                await LoadTenantHintAsync(cancellationToken);
                return Page();
            }

            var resolvedTenantId = TenantId;
            if (resolvedTenantId == Guid.Empty)
            {
                var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
                foreach (var tenant in tenants)
                {
                    var u = await _userRepository.GetByEmailAsync(tenant.Id, Email, cancellationToken);
                    if (u is not null)
                    {
                        resolvedTenantId = tenant.Id;
                        break;
                    }
                }
            }

            var user = await _userRepository.GetByEmailAsync(resolvedTenantId, Email, cancellationToken);
            if (user is null)
            {
                ErrorMessage = _localizer["Account_UserNotFound"];
                await LoadTenantHintAsync(cancellationToken);
                return Page();
            }

            var principal = _tokenService.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = result.ExpiresAt
                });

            Response.Cookies.Append("access_token", result.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = result.ExpiresAt
            });

            return Redirect(RoleHomeRedirect.GetHomePath(principal));
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = _localizer["Account_InvalidCredentials"];
            await LoadTenantHintAsync(cancellationToken);
            return Page();
        }
        catch (Exception)
        {
            ErrorMessage = _localizer["Account_LoginFailed"];
            await LoadTenantHintAsync(cancellationToken);
            return Page();
        }
    }

    [EnableRateLimiting("login")]
    public async Task<IActionResult> OnPostVerifyMfaAsync(CancellationToken cancellationToken)
    {
        ShowMfaStep = true;
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        ShowTenantField = !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(MfaTempToken) || string.IsNullOrWhiteSpace(MfaCode))
        {
            ErrorMessage = _localizer["Account_EnterMfaCode"].Value;
            await LoadTenantHintAsync(cancellationToken);
            return Page();
        }

        try
        {
            var result = await _verifyMfaHandler.HandleAsync(
                new VerifyMfaCommand(MfaTempToken, MfaCode.Trim()),
                cancellationToken);

            _tenantAccessor.TenantId = TenantId;
            var principal = _tokenService.CreatePrincipal(
                await ResolveUserAfterMfaAsync(result.AccessToken, cancellationToken)
                    ?? throw new UnauthorizedAccessException(),
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = result.ExpiresAt });

            Response.Cookies.Append("access_token", result.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = result.ExpiresAt
            });

            return Redirect(RoleHomeRedirect.GetHomePath(principal));
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = _localizer["Account_InvalidMfaExpired"].Value;
            await LoadTenantHintAsync(cancellationToken);
            return Page();
        }
    }

    private async Task<Domain.Users.User?> ResolveUserAfterMfaAsync(string accessToken, CancellationToken cancellationToken)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var tid = jwt.Claims.FirstOrDefault(c => c.Type == "TenantId")?.Value;
        if (sub == null || !Guid.TryParse(sub, out var userId))
            return null;
        if (tid != null && Guid.TryParse(tid, out var tenantId))
            _tenantAccessor.TenantId = tenantId;
        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    private async Task LoadTenantHintAsync(CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        var gmg = tenants.FirstOrDefault(t => t.Name == GlobalManufacturingDemoSeeder.TenantName);
        var ceoDemo = tenants.FirstOrDefault(t => t.Name == CeoDemoSeeder.TenantName);
        var first = gmg ?? ceoDemo ?? tenants.FirstOrDefault();
        if (first is not null)
        {
            TenantId = first.Id;
            DemoTenantName = first.Name;
        }

        LoadDemoAccounts();
    }

    private void LoadDemoAccounts()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        if (!_configuration.GetValue("Seed:Enabled", false)
            || string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            DemoAccounts = Array.Empty<DemoLoginAccount>();
            return;
        }

        DemoAccounts = DemoRoleUsers.All
            .Select(u => new DemoLoginAccount(u.Role, u.Email, DemoRoleUsers.PasswordFor(u.Role)))
            .ToList();
    }
}
