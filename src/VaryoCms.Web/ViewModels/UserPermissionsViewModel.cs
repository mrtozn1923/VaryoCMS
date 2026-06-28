using VaryoCms.Application.DTOs.User;

namespace VaryoCms.Web.ViewModels;

public class UserPermissionsViewModel
{
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public List<ContentTypePermissionDto> Permissions { get; set; } = new();
}
