using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.DTOs.Auth;

// Minimal identity returned after a successful credential check (no password hash).
public class AuthenticatedUserDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Email { get; set; } = null!;
    public string? FullName { get; set; }
    public UserRole Role { get; set; }
}
