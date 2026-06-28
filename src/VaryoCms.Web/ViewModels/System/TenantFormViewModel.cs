using System.ComponentModel.DataAnnotations;
using VaryoCms.Application.DTOs.System;

namespace VaryoCms.Web.ViewModels.System;

// Single view model for create + edit. Create-only fields (slug, language, first admin) are validated
// server-side by FluentValidation; the slug is read-only on edit (immutable).
public class TenantFormViewModel
{
    public int Id { get; set; }
    public bool IsEdit => Id > 0;

    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(200)]
    [Display(Name = "Common.Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Common.Slug")]
    public string Slug { get; set; } = string.Empty;

    [Display(Name = "Common.Active")]
    public bool IsActive { get; set; } = true;

    // --- Create-only ---
    [Display(Name = "Field.DefaultLanguageCode")]
    public string DefaultLanguageCode { get; set; } = "tr";

    [Display(Name = "Field.DefaultLanguageName")]
    public string DefaultLanguageName { get; set; } = "Türkçe";

    [Display(Name = "Field.FirstAdminEmail")]
    public string? FirstAdminEmail { get; set; }

    [Display(Name = "Field.FirstAdminFullName")]
    public string? FirstAdminFullName { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Field.FirstAdminPassword")]
    public string? FirstAdminPassword { get; set; }

    public CreateTenantRequest ToCreateRequest() => new()
    {
        Name = Name.Trim(),
        Slug = Slug.Trim(),
        DefaultLanguageCode = (DefaultLanguageCode ?? string.Empty).Trim(),
        DefaultLanguageName = (DefaultLanguageName ?? string.Empty).Trim(),
        FirstAdminEmail = (FirstAdminEmail ?? string.Empty).Trim(),
        FirstAdminFullName = string.IsNullOrWhiteSpace(FirstAdminFullName) ? null : FirstAdminFullName.Trim(),
        FirstAdminPassword = FirstAdminPassword ?? string.Empty
    };

    public UpdateTenantRequest ToUpdateRequest() => new()
    {
        Name = Name.Trim(),
        IsActive = IsActive
    };

    public static TenantFormViewModel FromDto(TenantEditDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Slug = d.Slug,
        IsActive = d.IsActive
    };
}
