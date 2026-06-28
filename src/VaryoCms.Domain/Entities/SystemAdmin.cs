namespace VaryoCms.Domain.Entities;

// Cross-tenant platform owner. Not tied to a tenant (no tenant_id); everyone in system_admins is a SystemAdmin.
public class SystemAdmin
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
