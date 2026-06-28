using System.Text.Json;

namespace VaryoCms.Application.Common.FieldOptions;

// { "min": 0, "max": 999999, "decimals": 2 }
// Used for both Number and Decimal field types.
public sealed record NumberOptions(decimal? Min, decimal? Max, int? Decimals)
{
    public static NumberOptions Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new(null, null, null);
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var root = doc.RootElement;

            decimal? min = TryGetDecimal(root, "min");
            decimal? max = TryGetDecimal(root, "max");

            int? decimals = null;
            if (root.TryGetProperty("decimals", out var d)
                && d.ValueKind == JsonValueKind.Number
                && d.TryGetInt32(out int dv) && dv >= 0)
                decimals = dv;

            return new(min, max, decimals);
        }
        catch (JsonException) { return new(null, null, null); }
    }

    private static decimal? TryGetDecimal(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var el) || el.ValueKind != JsonValueKind.Number)
            return null;
        return el.TryGetDecimal(out decimal v) ? v : null;
    }
}
