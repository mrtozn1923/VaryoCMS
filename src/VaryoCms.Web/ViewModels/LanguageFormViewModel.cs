using System.ComponentModel.DataAnnotations;
using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Web.ViewModels;

public class LanguageFormViewModel
{
    public int Id { get; set; }
    public bool IsEdit => Id > 0;

    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(5)]
    [Display(Name = "Common.Code")]
    public string Code { get; set; } = string.Empty;   // read-only on edit (immutable)

    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(100)]
    [Display(Name = "Common.Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Field.FlagIcon")]
    public string? FlagIcon { get; set; }

    [Display(Name = "Field.IsDefaultLanguage")]
    public bool IsDefault { get; set; }

    [Display(Name = "Common.Active")]
    public bool IsActive { get; set; } = true;

    public CreateLanguageRequest ToCreateRequest() => new()
    {
        Code = Code.Trim(),
        Name = Name.Trim(),
        FlagIcon = string.IsNullOrWhiteSpace(FlagIcon) ? null : FlagIcon.Trim(),
        IsDefault = IsDefault,
        IsActive = IsActive
    };

    public UpdateLanguageRequest ToUpdateRequest() => new()
    {
        Name = Name.Trim(),
        FlagIcon = string.IsNullOrWhiteSpace(FlagIcon) ? null : FlagIcon.Trim(),
        IsDefault = IsDefault,
        IsActive = IsActive
    };

    public static LanguageFormViewModel FromDto(LanguageEditDto d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Name = d.Name,
        FlagIcon = d.FlagIcon,
        IsDefault = d.IsDefault,
        IsActive = d.IsActive
    };
}
