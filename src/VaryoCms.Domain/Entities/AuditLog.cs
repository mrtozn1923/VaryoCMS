namespace VaryoCms.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public int? ContentTypeId { get; set; }
    public string? EntityName { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
