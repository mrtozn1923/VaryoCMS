namespace VaryoCms.Application.DTOs.Localization;

public class LanguageDto
{
    public string Code { get; set; } = null!;     // 'tr', 'en'
    public string Name { get; set; } = null!;     // 'Türkçe', 'English'
    public bool IsDefault { get; set; }
}
