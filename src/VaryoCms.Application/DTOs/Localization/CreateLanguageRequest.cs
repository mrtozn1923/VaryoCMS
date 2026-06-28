namespace VaryoCms.Application.DTOs.Localization;

public class CreateLanguageRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? FlagIcon { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
