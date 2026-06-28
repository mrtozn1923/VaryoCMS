using VaryoCms.Application.DTOs.ContentField;

namespace VaryoCms.Web.ViewModels;

public class FieldBuilderViewModel
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    public IReadOnlyList<ContentFieldDto> Fields { get; set; } = Array.Empty<ContentFieldDto>();
    public ContentFieldFormViewModel NewField { get; set; } = new();
}
