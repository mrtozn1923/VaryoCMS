namespace VaryoCms.Application.DTOs.User;

// Form payload for saving the permission matrix. Rows with all flags false are not persisted.
public class SaveUserPermissionsRequest
{
    public List<ContentTypePermissionDto> Permissions { get; set; } = new();
}
