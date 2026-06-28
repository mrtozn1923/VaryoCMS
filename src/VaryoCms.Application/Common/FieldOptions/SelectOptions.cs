using System.Text.Json;

namespace VaryoCms.Application.Common.FieldOptions;

// { "choices": ["Option A", "Option B", "Option C"] }
// Used for both Select and MultiSelect field types.
public sealed record SelectOptions(IReadOnlyList<string> Choices)
{
    public static SelectOptions Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new([]);
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("choices", out var arr)
                || arr.ValueKind != JsonValueKind.Array)
                return new([]);

            List<string> choices = [];
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    string? s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) choices.Add(s);
                }
            }
            return new(choices);
        }
        catch (JsonException) { return new([]); }
    }
}
