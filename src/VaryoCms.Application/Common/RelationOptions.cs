using System.Text.Json;

namespace VaryoCms.Application.Common;

// Parsed options_json for Relation / MultiRelation fields:
// { "target_content_type_id": 3, "display_field_slug": "title", "min_items": 0, "max_items": 10 }
// Multiplicity is derived from the field type (Relation vs MultiRelation), not from options.
// MinItems/MaxItems are optional count constraints (only meaningful for MultiRelation).
public sealed record RelationOptions(int TargetContentTypeId, string? DisplayFieldSlug)
{
    public int? MinItems { get; init; }
    public int? MaxItems { get; init; }

    public static RelationOptions? Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("target_content_type_id", out var targetEl)
                || !targetEl.TryGetInt32(out int target) || target <= 0)
                return null;

            string? display = root.TryGetProperty("display_field_slug", out var d) && d.ValueKind == JsonValueKind.String
                ? d.GetString()
                : null;

            return new RelationOptions(target, display)
            {
                MinItems = ReadNonNegativeInt(root, "min_items"),
                MaxItems = ReadNonNegativeInt(root, "max_items")
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int? ReadNonNegativeInt(JsonElement root, string property)
        => root.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.Number
           && el.TryGetInt32(out int v) && v >= 0
            ? v
            : null;
}
