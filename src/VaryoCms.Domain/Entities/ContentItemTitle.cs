namespace VaryoCms.Domain.Entities;

public class ContentItemTitle
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ContentItemId { get; set; }
    public string LanguageCode { get; set; } = "tr";
    public string Title { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
