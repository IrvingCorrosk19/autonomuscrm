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
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly EnterpriseAuthOptions _enterpriseAuth;

    public LoginModel(
        IRequestHandler<LoginCommand, LoginResult> loginHandler,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITokenService tokenService,
        IConfiguration configuration,
        ICurrentTenantAccessor tenantAccessor,
        IOptions<EnterpriseAuthOptions> enterpriseAuth)
    {
        _loginHandler = loginHandler;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tokenService = tokenService;
        _configuration = configuration;
        _tenantAccessor = tenantAccessor;
        _enterpriseAuth = enterpriseAuth.Value;
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
            return RedirectToPage("/Index");

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        ShowDemoAccounts = _configuration.GetValue("Seed:Enabled", false)
            && !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
        ShowTenantField = !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

        DemoEmail = _configuration["Seed:AdminEmail"] ?? DemoEmail;
        DemoPassword = _configuration["Seed:AdminPassword"] ?? DemoPassword;

        _tenantAccessor.BypassTenantFilter = true;
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        _tenantAccessor.BypassTenantFilter = false;
        var first = tenants.FirstOrDefault();
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
                ErrorMessage = "MFA requerido. Use la API /api/auth/verify-mfa con el token temporal.";
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
                ErrorMessage = "Usuario no encontrado.";
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

            return RedirectToPage("/Index");
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Credenciales inválidas. Use el email y contraseña del cuadro demo; el Tenant ID se rellena solo.";
            await LoadTenantHintAsync(cancellationToken);
            return Page();
        }
        catch (Exception)
        {
            ErrorMessage = "No se pudo iniciar sesión. Verifique los datos e intente de nuevo.";
            await LoadTenantHintAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadTenantHintAsync(CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        var first = tenants.FirstOrDefault();
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
