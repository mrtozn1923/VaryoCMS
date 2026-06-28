using VaryoCms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers;

// Persists the admin-UI language chosen from the top-bar switcher (cookie read by
// LanguageResolutionMiddleware → CultureInfo). Available to any authenticated user.
[Authorize]
[Route("set-language")]
public class LanguagePreferenceController : Controller
{
    public const string CookieName = "cms_lang";

    private readonly IUiTranslationStore _uiCultures;

    public LanguagePreferenceController(IUiTranslationStore uiCultures) => _uiCultures = uiCultures;

    [HttpGet("")]
    public IActionResult Set(string code, string? returnUrl)
    {
        // Only honour a code that is an active UI culture (global ui_cultures).
        if (!string.IsNullOrWhiteSpace(code) && _uiCultures.IsActiveCulture(code))
        {
            Response.Cookies.Append(CookieName, code.Trim().ToLowerInvariant(), new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        }

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
    }
}
