using System.ComponentModel.DataAnnotations;

namespace VaryoCms.Web.ViewModels.System;

public class SystemLoginViewModel
{
    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    [Display(Name = "Common.Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.Required")]
    [DataType(DataType.Password)]
    [Display(Name = "Common.Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Account.RememberMe")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
