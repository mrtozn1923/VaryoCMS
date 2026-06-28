using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.DTOs.User;

public class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string? FullName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public string Password { get; set; } = null!;
}
