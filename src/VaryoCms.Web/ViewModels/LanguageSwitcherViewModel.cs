using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Web.ViewModels;

public class LanguageSwitcherViewModel
{
    public IReadOnlyList<LanguageDto> Languages { get; set; } = new List<LanguageDto>();
    public string CurrentCode { get; set; } = "tr";
    public string ReturnUrl { get; set; } = "/";
}
