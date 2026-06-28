using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.DTOs.Media;
using VaryoCms.Domain.Enums;
using VaryoCms.Application.Common.FieldOptions;

namespace VaryoCms.Web.ViewModels;

// One field's definition plus its current raw value, used to render a dynamic input.
public class FieldRenderModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsLocalized { get; set; }
    public string? OptionsJson { get; set; }
    public string? Value { get; set; }

    // The form's active language — relation search results are resolved in this language.
    public string LanguageCode { get; set; } = "tr";

    // Relation/MultiRelation render data.
    public int? RelationTargetTypeId { get; set; }
    public string? RelationDisplayField { get; set; }
    public bool RelationMultiple { get; set; }
    public int? RelationMinItems { get; set; }
    public int? RelationMaxItems { get; set; }
    public List<RelatedItemDto> SelectedRelations { get; set; } = new();

    // Media field render data (Image/Video/Audio/File/Gallery).
    public bool MediaMultiple { get; set; }          // Gallery
    public string? MediaTypeFilter { get; set; }     // image | video | audio | null (any)
    public List<MediaAssetDto> SelectedMedia { get; set; } = new();

    // Options parsed from options_json — set by ContentItemFormFactory.
    public string? Placeholder { get; set; }
    public int? TextMaxLength { get; set; }
    public decimal? NumberMin { get; set; }
    public decimal? NumberMax { get; set; }
    public string NumberStep { get; set; } = "any";
    public int RatingMax { get; set; } = 5;
    public IReadOnlyList<string> SelectChoices { get; set; } = [];
    public string? CodeLanguage { get; set; }
    public int? MediaMaxSizeMb { get; set; }
    public IReadOnlyList<string> MediaAllowedFormats { get; set; } = [];
}
