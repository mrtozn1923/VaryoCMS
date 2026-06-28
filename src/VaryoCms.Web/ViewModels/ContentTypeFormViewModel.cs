using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using VaryoCms.Application.DTOs.ContentType;

namespace VaryoCms.Web.ViewModels;

public class ContentTypeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Validation.Required"), StringLength(200)]
    [Display(Name = "Common.Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.Required"), StringLength(200)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Validation.Slug")]
    [Display(Name = "Common.Slug")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Field.Description")]
    public string? Description { get; set; }

    [StringLength(100)]
    [Display(Name = "Field.Icon")]
    public string? Icon { get; set; }

    [Display(Name = "Field.Published")]
    public bool IsPublished { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Field.SortOrder")]
    public int SortOrder { get; set; }

    public int? ParentId { get; set; }

    // Populated by the controller — not bound from the form (just a display list).
    public List<SelectListItem> ParentOptions { get; set; } = new();

    public CreateContentTypeRequest ToCreateRequest() => new()
    {
        Name = Name,
        Slug = Slug,
        Description = Description,
        Icon = Icon,
        IsPublished = IsPublished,
        SortOrder = SortOrder,
        ParentId = ParentId
    };

    public UpdateContentTypeRequest ToUpdateRequest() => new()
    {
        Name = Name,
        Slug = Slug,
        Description = Description,
        Icon = Icon,
        IsPublished = IsPublished,
        SortOrder = SortOrder,
        ParentId = ParentId
    };

    public static ContentTypeFormViewModel FromDto(ContentTypeDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Slug = d.Slug,
        Description = d.Description,
        Icon = d.Icon,
        IsPublished = d.IsPublished,
        SortOrder = d.SortOrder,
        ParentId = d.ParentId
    };
}
