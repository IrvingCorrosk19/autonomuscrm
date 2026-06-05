using AutonomusCRM.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[AllowAnonymous]
[Route("culture")]
public class CultureController : Controller
{
    [HttpGet("set")]
    public IActionResult SetCulture(string culture, string? returnUrl)
    {
        if (!LocalizationExtensions.SupportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
            culture = LocalizationExtensions.DefaultCulture;

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Index");
    }
}
