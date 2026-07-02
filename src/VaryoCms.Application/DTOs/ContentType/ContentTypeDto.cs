namespace VaryoCms.Application.DTOs.ContentType;

public class ContentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public int? ParentId { get; set; }
    // True = form gönderim kutusu tipi (admin liste görünümü okundu/okunmadı UI'sını gösterir).
    public bool NotifyOnCreate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
