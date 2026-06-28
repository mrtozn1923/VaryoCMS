namespace VaryoCms.Application.DTOs.Auth;

// Result of a successful cross-tenant SystemAdmin login. Role is implicitly SystemAdmin; no tenant.
public class AuthenticatedSystemAdminDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? FullName { get; set; }
}
