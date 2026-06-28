using VaryoCms.Application.DTOs.Localization;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.ViewComponents;

// Top-bar admin-UI language switcher. Lists the active UI cultures (ui_cultures, global) so it works for
// tenant admins AND SystemAdmins. Selecting one sets the cms_lang cookie (drives CultureInfo).
public class LanguageSwitcherViewComponent : ViewComponent
{
    private readonly IUiTranslationStore _uiCultures;
    private readonly ILanguageContext _language;

    public LanguageSwitcherViewComponent(IUiTranslationStore uiCultures, ILanguageContext language)
    {
        _uiCultures = uiCultures;
        _language = language;
    }

    public IViewComponentResult Invoke()
    {
        var request = HttpContext.Request;
        return View(new LanguageSwitcherViewModel
        {
            Languages = _uiCultures.ActiveCultureItems
                .Select(c => new LanguageDto { Code = c.Code, Name = c.Name })
                .ToList(),
            CurrentCode = _language.CurrentCode,
            ReturnUrl = request.Path + request.QueryString
        });
    }
}
