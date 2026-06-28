namespace VaryoCms.Application.DTOs.Localization;

public class LanguageListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? FlagIcon { get; set; }
}
