namespace VaryoCms.Application.DTOs.Dictionary;

public class DictionaryEntryEditDto
{
    public int Id { get; set; }
    public string KeyName { get; set; } = null!;
    public string? Category { get; set; }
    public Dictionary<string, string> Translations { get; set; } = new();   // languageCode -> value
}
