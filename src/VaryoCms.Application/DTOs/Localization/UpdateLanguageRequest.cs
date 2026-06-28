namespace VaryoCms.Application.DTOs.Localization;

// Code is intentionally absent — immutable (content values reference language_code).
public class UpdateLanguageRequest
{
    public string Name { get; set; } = null!;
    public string? FlagIcon { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
