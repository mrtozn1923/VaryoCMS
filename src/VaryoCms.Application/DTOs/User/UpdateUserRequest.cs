using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.DTOs.User;

public class UpdateUserRequest
{
    public string Email { get; set; } = null!;
    public string? FullName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    // Optional: when non-empty the password is reset to this value (BCrypt-hashed).
    public string? NewPassword { get; set; }
}
