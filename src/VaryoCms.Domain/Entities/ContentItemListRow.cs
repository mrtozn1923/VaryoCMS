namespace VaryoCms.Domain.Entities;

// Read-model for the content item list grid (populated by a single JOIN query in ContentItemRepository).
public class ContentItemListRow
{
    public int Id { get; set; }
    public string? Slug { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Title { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
}
