using System.Globalization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.Common;

// Maps a raw form string to the correct value_* column on a ContentFieldValue (and back),
// based on the field's type.
// v1 supports scalar types. Deferred: Relation, MultiRelation, Gallery (separate tables),
// and the end side of DateRange — these fall back to value_text / value_date for now.
public static class FieldValueMapper
{
    public static void Apply(ContentFieldValue target, FieldType type, string? raw)
    {
        target.ValueText = null;
        target.ValueNumber = null;
        target.ValueBool = null;
        target.ValueDate = null;
        target.ValueDateEnd = null;
        target.ValueMediaId = null;

        switch (type)
        {
            case FieldType.Number or FieldType.Decimal or FieldType.Rating:
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    target.ValueNumber = num;
                break;

            case FieldType.Boolean:
                target.ValueBool = ParseBool(raw);
                break;

            case FieldType.Date or FieldType.DateTime or FieldType.Time:
                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    target.ValueDate = dt;
                break;

            case FieldType.DateRange:
                // raw is "start|end" (either side may be empty).
                var parts = (raw ?? string.Empty).Split('|');
                if (parts.Length > 0 && DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                    target.ValueDate = start;
                if (parts.Length > 1 && DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                    target.ValueDateEnd = end;
                break;

            case FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File:
                if (int.TryParse(raw, out var mediaId))
                    target.ValueMediaId = mediaId;
                break;

            default:
                // Text-like: Text, RichText, Markdown, Email, URL, Phone, Color, JSON,
                // CodeSnippet, Select, MultiSelect, Tags, Slug, Password, GeoLocation.
                target.ValueText = string.IsNullOrEmpty(raw) ? null : raw;
                break;
        }
    }

    public static string? ToRaw(ContentFieldValue value, FieldType type) => type switch
    {
        FieldType.Number or FieldType.Decimal or FieldType.Rating
            => value.ValueNumber?.ToString(CultureInfo.InvariantCulture),
        FieldType.Boolean
            => value.ValueBool?.ToString().ToLowerInvariant(),
        FieldType.Date or FieldType.DateTime or FieldType.Time
            => value.ValueDate?.ToString("o", CultureInfo.InvariantCulture),
        FieldType.DateRange => FormatRange(value),
        FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File
            => value.ValueMediaId?.ToString(),
        _ => value.ValueText
    };

    private static bool ParseBool(string? raw)
        => raw is "true" or "on" or "1" or "True";

    // DateRange round-trips as "start|end" (yyyy-MM-dd, either side may be empty) for the form's two date inputs.
    private static string? FormatRange(ContentFieldValue value)
    {
        if (value.ValueDate is null && value.ValueDateEnd is null) return null;
        string start = value.ValueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        string end = value.ValueDateEnd?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        return $"{start}|{end}";
    }
}
