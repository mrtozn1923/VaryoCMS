namespace VaryoCms.Application.DTOs.ContentType;

public class CreateContentTypeRequest
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public int? ParentId { get; set; }
}
