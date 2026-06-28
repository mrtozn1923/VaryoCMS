using System.ComponentModel.DataAnnotations;

namespace VaryoCms.Web.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Validation.Required")]
    [DataType(DataType.Password)]
    [Display(Name = "Field.CurrentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Validation.PasswordLength")]
    [DataType(DataType.Password)]
    [Display(Name = "Field.NewPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.Required")]
    [DataType(DataType.Password)]
    [Display(Name = "Field.ConfirmPassword")]
    [Compare(nameof(NewPassword), ErrorMessage = "Validation.PasswordsMatch")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
