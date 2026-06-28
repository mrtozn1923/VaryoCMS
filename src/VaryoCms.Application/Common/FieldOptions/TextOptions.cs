using System.Text.Json;

namespace VaryoCms.Application.Common.FieldOptions;

// { "max_length": 500, "placeholder": "Enter title..." }
public sealed record TextOptions(int? MaxLength, string? Placeholder)
{
    public static TextOptions Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new(null, null);
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var root = doc.RootElement;

            int? maxLength = null;
            if (root.TryGetProperty("max_length", out var ml)
                && ml.ValueKind == JsonValueKind.Number
                && ml.TryGetInt32(out int v) && v > 0)
                maxLength = v;

            string? placeholder = root.TryGetProperty("placeholder", out var ph)
                && ph.ValueKind == JsonValueKind.String
                ? ph.GetString()
                : null;

            return new(maxLength, placeholder);
        }
        catch (JsonException) { return new(null, null); }
    }
}
