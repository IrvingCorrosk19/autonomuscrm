using AutonomusCRM.Application.Auth;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IRequestHandler<LoginCommand, LoginResult> _loginHandler;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public LoginModel(
        IRequestHandler<LoginCommand, LoginResult> loginHandler,
        IUserRepository userRepository,
        ITokenService tokenService)
    {
        _loginHandler = loginHandler;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    [BindProperty]
    public Guid TenantId { get; set; }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _loginHandler.HandleAsync(
                new LoginCommand(Email, Password, TenantId),
                cancellationToken);

            if (result.RequiresMfa)
            {
                ErrorMessage = "MFA requerido. Use la API /api/auth/verify-mfa con el token temporal.";
                return Page();
            }

            var user = await _userRepository.GetByEmailAsync(TenantId, Email, cancellationToken);
            if (user is null)
            {
                ErrorMessage = "Usuario no encontrado.";
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
            ErrorMessage = "Credenciales inválidas.";
            return Page();
        }
        catch (Exception)
        {
            ErrorMessage = "No se pudo iniciar sesión. Verifique tenant, email y contraseña.";
            return Page();
        }
    }
}
