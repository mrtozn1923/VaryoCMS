using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.DTOs.ContentField;

public class CreateContentFieldRequest
{
    public int ContentTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsLocalized { get; set; } = true;
    public string? OptionsJson { get; set; }
}
