namespace VaryoCms.Application.DTOs.Api;

// One row of the API management list: a content type and its API exposure status.
public class ApiConfigListItemDto
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = null!;
    public string ContentTypeSlug { get; set; } = null!;
    public bool IsConfigured { get; set; }   // an api_configurations row exists
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }       // true = no auth; false = credential required
    public bool AllowWrite { get; set; }     // true if any of create/update/delete is enabled
}
