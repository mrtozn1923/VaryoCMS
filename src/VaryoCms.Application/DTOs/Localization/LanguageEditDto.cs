namespace VaryoCms.Application.DTOs.Localization;

public class LanguageEditDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;   // read-only in the UI (immutable — referenced by content values)
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? FlagIcon { get; set; }
}
