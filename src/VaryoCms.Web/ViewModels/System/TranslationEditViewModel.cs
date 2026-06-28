using VaryoCms.Application.DTOs.System;

namespace VaryoCms.Web.ViewModels.System;

public class TranslationEditViewModel
{
    public string Key { get; set; } = null!;
    public IReadOnlyList<UiCultureDto> Cultures { get; set; } = new List<UiCultureDto>();

    // culture code -> value (bound on POST as Values[code])
    public Dictionary<string, string> Values { get; set; } = new();
}
