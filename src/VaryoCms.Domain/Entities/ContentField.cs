using VaryoCms.Domain.Enums;

namespace VaryoCms.Domain.Entities;

public class ContentField
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ContentTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsLocalized { get; set; }
    public int SortOrder { get; set; }
    public string? OptionsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
