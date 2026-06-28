namespace VaryoCms.Domain.Entities;

// EAV value row. No IsDeleted (per schema) — values are replaced/removed with their item.
public class ContentFieldValue
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ContentItemId { get; set; }
    public int ContentFieldId { get; set; }
    public string LanguageCode { get; set; } = "tr";   // 'tr', 'en', 'de' ... or 'all' for non-localized
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public bool? ValueBool { get; set; }
    public DateTime? ValueDate { get; set; }
    public DateTime? ValueDateEnd { get; set; }
    public int? ValueMediaId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
