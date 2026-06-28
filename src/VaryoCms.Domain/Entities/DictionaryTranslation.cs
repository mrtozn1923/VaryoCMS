namespace VaryoCms.Domain.Entities;

// Translation value. No tenant_id (resolved via EntryId) / timestamps / IsDeleted (per schema).
public class DictionaryTranslation
{
    public int Id { get; set; }
    public int EntryId { get; set; }
    public string LanguageCode { get; set; } = null!;
    public string Value { get; set; } = null!;
}
