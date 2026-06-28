namespace VaryoCms.Application.DTOs.System;

// One resource key with its value per culture (culture code -> value).
public class TranslationKeyDto
{
    public string Key { get; set; } = null!;
    public IReadOnlyDictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
}
