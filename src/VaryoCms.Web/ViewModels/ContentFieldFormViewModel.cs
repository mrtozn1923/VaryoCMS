using System.ComponentModel.DataAnnotations;
using VaryoCms.Application.DTOs.ContentField;
using VaryoCms.Domain.Enums;

namespace VaryoCms.Web.ViewModels;

public class ContentFieldFormViewModel
{
    public int Id { get; set; }
    public int ContentTypeId { get; set; }

    [Required(ErrorMessage = "Validation.Required"), StringLength(200)]
    [Display(Name = "Common.Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.Required"), StringLength(200)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Validation.Slug")]
    [Display(Name = "Common.Slug")]
    public string Slug { get; set; } = string.Empty;

    [Display(Name = "Field.FieldType")]
    public FieldType FieldType { get; set; }

    [Display(Name = "Field.Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Field.Localized")]
    public bool IsLocalized { get; set; } = true;

    [Display(Name = "Field.Options")]
    public string? OptionsJson { get; set; }

    public CreateContentFieldRequest ToCreateRequest() => new()
    {
        ContentTypeId = ContentTypeId,
        Name = Name,
        Slug = Slug,
        FieldType = FieldType,
        IsRequired = IsRequired,
        IsLocalized = IsLocalized,
        OptionsJson = OptionsJson
    };

    public UpdateContentFieldRequest ToUpdateRequest() => new()
    {
        Name = Name,
        Slug = Slug,
        FieldType = FieldType,
        IsRequired = IsRequired,
        IsLocalized = IsLocalized,
        OptionsJson = OptionsJson
    };

    public static ContentFieldFormViewModel FromDto(ContentFieldDto d) => new()
    {
        Id = d.Id,
        ContentTypeId = d.ContentTypeId,
        Name = d.Name,
        Slug = d.Slug,
        FieldType = d.FieldType,
        IsRequired = d.IsRequired,
        IsLocalized = d.IsLocalized,
        OptionsJson = d.OptionsJson
    };
}
