namespace VaryoCms.Application.DTOs.User;

// Full permission matrix for a user: every content type, with the user's current flags.
public class UserPermissionsDto
{
    public int UserId { get; set; }
    public string UserEmail { get; set; } = null!;
    public IReadOnlyList<ContentTypePermissionDto> Permissions { get; set; } = Array.Empty<ContentTypePermissionDto>();
}
