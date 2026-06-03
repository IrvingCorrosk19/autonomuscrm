using AutonomusCRM.Application.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.Integrations;

[AllowAnonymous]
public class OAuthCallbackModel : PageModel
{
    private readonly IIntegrationOAuthService _oauth;

    public OAuthCallbackModel(IIntegrationOAuthService oauth) => _oauth = oauth;

    public async Task<IActionResult> OnGetAsync(string provider, Guid tenantId, string? code, string? error)
    {
        if (!string.IsNullOrEmpty(error))
            return RedirectToPage("/Integrations", new { error });

        if (string.IsNullOrWhiteSpace(code))
            return RedirectToPage("/Integrations", new { error = "Código OAuth ausente" });

        var result = await _oauth.HandleCallbackAsync(tenantId, provider, code);
        return RedirectToPage("/Integrations", result.Success
            ? new { message = $"{provider} conectado vía OAuth." }
            : new { error = result.Error });
    }
}
