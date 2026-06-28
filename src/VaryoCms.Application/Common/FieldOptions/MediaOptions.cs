using System.Text.Json;

namespace VaryoCms.Application.Common.FieldOptions;

// { "max_size_mb": 10, "allowed_formats": ["jpg","png","webp"] }
// Used for Image, Video, Audio, File, Gallery field types.
public sealed record MediaOptions(int? MaxSizeMb, IReadOnlyList<string> AllowedFormats)
{
    public static MediaOptions Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new(null, []);
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var root = doc.RootElement;

            int? maxSizeMb = null;
            if (root.TryGetProperty("max_size_mb", out var ms)
                && ms.ValueKind == JsonValueKind.Number
                && ms.TryGetInt32(out int v) && v > 0)
                maxSizeMb = v;

            List<string> formats = [];
            if (root.TryGetProperty("allowed_formats", out var arr)
                && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String)
                    {
                        string? s = el.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            formats.Add(s.ToLowerInvariant().TrimStart('.'));
                    }
                }
            }

            return new(maxSizeMb, formats);
        }
        catch (JsonException) { return new(null, []); }
    }

    public bool HasFormatRestriction => AllowedFormats.Count > 0;

    public bool IsFormatAllowed(string extension)
    {
        if (!HasFormatRestriction) return true;
        string ext = extension.ToLowerInvariant().TrimStart('.');
        return AllowedFormats.Any(f => f == ext);
    }

    public long? MaxSizeBytes => MaxSizeMb.HasValue ? (long)MaxSizeMb.Value * 1024 * 1024 : null;
}
