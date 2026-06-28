using System.Globalization;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.Contexts;

namespace VaryoCms.Web.Middleware;

// Resolves the active language: ?lang query > cms_lang cookie (top-bar switcher) > Accept-Language first tag
// > tenant default. Runs after TenantResolutionMiddleware so the tenant default is available.
// Also sets the .NET UI culture (for DB-backed admin localization) when the code is an active UI culture.
public class LanguageResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public LanguageResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        LanguageContext languageContext,
        ITenantContext tenantContext,
        IUiTranslationStore uiCultures)
    {
        // Content/editing language: ?lang (content tab) > cms_lang cookie > Accept-Language > tenant default.
        string contentCode = ResolveContentCode(context, tenantContext.DefaultLanguageCode);
        languageContext.Set(contentCode);

        // UI culture: cookie > Accept-Language > system default — intentionally ignores ?lang so that
        // switching a content-item language tab does not change the admin UI language.
        string uiCode = ResolveUiCode(context, uiCultures.DefaultCulture);
        uiCode = uiCultures.IsActiveCulture(uiCode) ? uiCode : uiCultures.DefaultCulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(uiCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (CultureNotFoundException) { /* leave thread default */ }

        await _next(context);
    }

    // Includes ?lang so content-item language tabs work.
    private static string ResolveContentCode(HttpContext context, string fallback)
    {
        string? fromQuery = context.Request.Query["lang"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromQuery))
            return Normalize(fromQuery);

        return ResolveUiCode(context, fallback);
    }

    // Cookie > Accept-Language > fallback. Shared by content (when no ?lang) and UI culture resolution.
    private static string ResolveUiCode(HttpContext context, string fallback)
    {
        string? fromCookie = context.Request.Cookies[Controllers.LanguagePreferenceController.CookieName];
        if (!string.IsNullOrWhiteSpace(fromCookie))
            return Normalize(fromCookie);

        string? fromHeader = context.Request.Headers.AcceptLanguage.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromHeader))
        {
            // "tr-TR,tr;q=0.9,en;q=0.8" -> "tr"
            string first = fromHeader.Split(',')[0].Split(';')[0].Split('-')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
                return Normalize(first);
        }

        return fallback;
    }

    private static string Normalize(string code) => code.Trim().ToLowerInvariant();
}
