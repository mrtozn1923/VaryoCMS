namespace VaryoCms.Application.DTOs.ContentItem;

// Form payload for create/update. Values maps content_field.id -> raw string value
// (the service converts each to the right value_* column based on the field's type).
public class SaveContentItemRequest
{
    public int ContentTypeId { get; set; }
    public string LanguageCode { get; set; } = "tr";
    public string Status { get; set; } = "draft";
    public string? Slug { get; set; }

    // Language-specific title for this language (stored in content_item_titles).
    public string? Title { get; set; }

    // Whether this language is activated (content published in this language).
    public bool IsLanguageActive { get; set; }

    public Dictionary<int, string?> Values { get; set; } = new();

    // Relation/MultiRelation fields: content_field.id -> ordered target content_item ids.
    public Dictionary<int, List<int>> Relations { get; set; } = new();
}
