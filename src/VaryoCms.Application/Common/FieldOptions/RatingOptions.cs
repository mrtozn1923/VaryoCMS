using System.Text.Json;

namespace VaryoCms.Application.Common.FieldOptions;

// { "max": 5 }  or  { "max": 10 }
public sealed record RatingOptions(int Max)
{
    public static RatingOptions Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new(5);
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("max", out var el)
                && el.ValueKind == JsonValueKind.Number
                && el.TryGetInt32(out int v) && v > 0)
                return new(v);

            return new(5);
        }
        catch (JsonException) { return new(5); }
    }
}
