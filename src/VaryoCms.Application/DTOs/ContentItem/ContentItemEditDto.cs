namespace VaryoCms.Application.DTOs.ContentItem;

// Item header + current field values (fieldId -> raw string) for the active language.
public class ContentItemEditDto
{
    public int Id { get; set; }
    public int ContentTypeId { get; set; }
    public string? Slug { get; set; }
    public string Status { get; set; } = "draft";
    public string LanguageCode { get; set; } = "tr";
    public string? Title { get; set; }
    public bool IsLanguageActive { get; set; }
    public Dictionary<int, string?> Values { get; set; } = new();

    // Relation/MultiRelation fields: content_field.id -> selected target items (with display values).
    public Dictionary<int, List<RelatedItemDto>> Relations { get; set; } = new();
}
