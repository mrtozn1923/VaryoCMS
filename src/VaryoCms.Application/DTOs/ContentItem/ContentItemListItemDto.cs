namespace VaryoCms.Application.DTOs.ContentItem;

public class ContentItemListItemDto
{
    public int Id { get; set; }
    public string? Slug { get; set; }
    public string Status { get; set; } = null!;
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
    // (Code, IsActive) pairs — active=published in that language, inactive=draft/unreleased.
    public IReadOnlyList<(string Code, bool IsActive)> FilledLanguages { get; set; } = Array.Empty<(string, bool)>();
}
