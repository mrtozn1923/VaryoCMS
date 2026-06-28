using System.ComponentModel.DataAnnotations;
using VaryoCms.Application.DTOs.User;
using VaryoCms.Domain.Enums;

namespace VaryoCms.Web.ViewModels;

public class UserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    [StringLength(256)]
    [Display(Name = "Common.Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Field.FullName")]
    public string? FullName { get; set; }

    [Display(Name = "Common.Role")]
    public UserRole Role { get; set; } = UserRole.Editor;

    [Display(Name = "Common.Active")]
    public bool IsActive { get; set; } = true;

    // Create: the new password (required). Edit: optional reset (blank = keep existing).
    [DataType(DataType.Password)]
    [Display(Name = "Common.Password")]
    public string? Password { get; set; }

    public CreateUserRequest ToCreateRequest() => new()
    {
        Email = Email.Trim(),
        FullName = string.IsNullOrWhiteSpace(FullName) ? null : FullName.Trim(),
        Role = Role,
        IsActive = IsActive,
        Password = Password ?? string.Empty
    };

    public UpdateUserRequest ToUpdateRequest() => new()
    {
        Email = Email.Trim(),
        FullName = string.IsNullOrWhiteSpace(FullName) ? null : FullName.Trim(),
        Role = Role,
        IsActive = IsActive,
        NewPassword = string.IsNullOrWhiteSpace(Password) ? null : Password
    };

    public static UserFormViewModel FromDto(UserEditDto d) => new()
    {
        Id = d.Id,
        Email = d.Email,
        FullName = d.FullName,
        Role = d.Role,
        IsActive = d.IsActive
    };
}
