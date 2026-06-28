using System.Text.Json;

namespace VaryoCms.Application.Common.FieldOptions;

// { "language": "javascript" }
public sealed record CodeSnippetOptions(string? Language)
{
    public static CodeSnippetOptions Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new((string?)null);
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var root = doc.RootElement;

            string? lang = root.TryGetProperty("language", out var el)
                && el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;

            return new(lang);
        }
        catch (JsonException) { return new((string?)null); }
    }
}
