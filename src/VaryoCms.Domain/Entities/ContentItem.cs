namespace VaryoCms.Domain.Entities;

public class ContentItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ContentTypeId { get; set; }
    public string? Slug { get; set; }
    public string Status { get; set; } = "draft";   // draft | published | archived
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
