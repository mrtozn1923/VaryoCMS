namespace VaryoCms.Domain.Entities;

// Junction: per-user permissions on a content type. No timestamps / IsDeleted (per schema).
public class UserContentTypePermission
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public int ContentTypeId { get; set; }
    public bool CanRead { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}
