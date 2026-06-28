namespace VaryoCms.Application.DTOs.Dictionary;

public class DictionaryEntryListItemDto
{
    public int Id { get; set; }
    public string KeyName { get; set; } = null!;
    public string? Category { get; set; }
    public int TranslatedCount { get; set; }   // number of languages with a non-empty value
    public DateTime UpdatedAt { get; set; }
}
