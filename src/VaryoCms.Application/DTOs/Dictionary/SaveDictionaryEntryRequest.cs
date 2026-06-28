namespace VaryoCms.Application.DTOs.Dictionary;

// Form payload for create/update. Translations maps languageCode -> value.
// A null/empty value removes that language's translation.
public class SaveDictionaryEntryRequest
{
    public string KeyName { get; set; } = null!;
    public string? Category { get; set; }
    public Dictionary<string, string?> Translations { get; set; } = new();
}
