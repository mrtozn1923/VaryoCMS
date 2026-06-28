using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VaryoCms.Web.ViewModels;

public class LoginViewModel
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

    // Display-only: the tenant resolved for this request (shown on the login page).
    // [BindNever] prevents over-posting — it's always set server-side from ITenantContext.
    [BindNever]
    public string? TenantSlug { get; set; }
}
