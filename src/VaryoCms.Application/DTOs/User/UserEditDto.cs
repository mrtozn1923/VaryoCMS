using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.DTOs.User;

// Never carries the password hash to the Web layer.
public class UserEditDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? FullName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
