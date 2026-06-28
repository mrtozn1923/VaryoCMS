using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.DTOs.ContentField;

public class ContentFieldDto
{
    public int Id { get; set; }
    public int ContentTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsLocalized { get; set; }
    public int SortOrder { get; set; }
    public string? OptionsJson { get; set; }
}
