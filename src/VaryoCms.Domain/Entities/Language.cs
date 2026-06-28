namespace VaryoCms.Domain.Entities;

// Per-tenant language. No timestamps / IsDeleted (per schema).
public class Language
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Code { get; set; } = null!;   // 'tr', 'en', 'de'
    public string Name { get; set; } = null!;   // 'Türkçe', 'English'
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? FlagIcon { get; set; }
}
