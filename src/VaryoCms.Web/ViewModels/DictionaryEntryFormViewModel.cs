using System.ComponentModel.DataAnnotations;
using VaryoCms.Application.DTOs.Dictionary;
using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Web.ViewModels;

public class DictionaryEntryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(300)]
    [Display(Name = "Dictionary.Key")]
    public string KeyName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Dictionary.Category")]
    public string? Category { get; set; }

    // Bound from form fields named Translations[tr], Translations[en], ...  (languageCode -> value)
    public Dictionary<string, string?> Translations { get; set; } = new();

    // Active languages for rendering the per-language inputs (re-populated by the controller on each render).
    public IReadOnlyList<LanguageDto> Languages { get; set; } = Array.Empty<LanguageDto>();

    public SaveDictionaryEntryRequest ToRequest() => new()
    {
        KeyName = KeyName?.Trim() ?? string.Empty,
        Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
        Translations = Translations ?? new()
    };
}
