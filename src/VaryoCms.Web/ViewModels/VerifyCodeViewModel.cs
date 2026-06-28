using System.ComponentModel.DataAnnotations;

namespace VaryoCms.Web.ViewModels;

public class VerifyCodeViewModel
{
    // Set by GET from TempData; rendered as hidden fields; posted back with the form.
    public string Email { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public bool RememberMe { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "6 haneli bir sayı girin.")]
    public string Code { get; set; } = "";
}
