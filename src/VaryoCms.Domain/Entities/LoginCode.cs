namespace VaryoCms.Domain.Entities;

public class LoginCode
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string TenantType { get; set; } = "";  // "tenant" | "system"
    public int? TenantId { get; set; }
    public string Code { get; set; } = "";
    public int Attempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
