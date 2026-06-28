namespace VaryoCms.Application.DTOs.Api;

// One field row in the API configuration's visibility table.
public class ApiFieldVisibilityDto
{
    public int ContentFieldId { get; set; }
    // Display-only (populated when editing); not required when posting the matrix back.
    public string? FieldName { get; set; }
    public string? FieldSlug { get; set; }
    public bool IsVisible { get; set; }
    public string? ResponseKeyAlias { get; set; }
}
