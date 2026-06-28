namespace VaryoCms.Application.DTOs.ContentItem;

// A selectable/related target item: its id and a human-readable display value.
public class RelatedItemDto
{
    public int Id { get; set; }
    public string DisplayValue { get; set; } = null!;
}
