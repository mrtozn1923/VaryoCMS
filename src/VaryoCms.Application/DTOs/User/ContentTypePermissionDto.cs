namespace VaryoCms.Application.DTOs.User;

// One row of the per-user permission matrix (one content type).
public class ContentTypePermissionDto
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = null!;
    public bool CanRead { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}
