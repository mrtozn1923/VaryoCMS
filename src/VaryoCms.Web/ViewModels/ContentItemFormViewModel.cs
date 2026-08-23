using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Web.ViewModels;

public class ContentItemFormViewModel
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    public int ItemId { get; set; }          // 0 = create

    // The language the form was opened/created in — always included in the save loop even
    // if left blank, and the only Title input wired to the auto-slug generator (site.js).
    public string PrimaryLanguageCode { get; set; } = "tr";

    // Shared across all languages — single columns on content_items, no language dimension.
    public string Status { get; set; } = "draft";
    public string? Slug { get; set; }

    // Shared field values (content_field.is_localized = false → stored once as language_code='all').
    // Posted as Values[fieldId] = rawValue
    public Dictionary<int, string?> Values { get; set; } = new();

    // Relation/MultiRelation — content_field_relations has no language_code column, always shared.
    // Posted as Relations[fieldId] = "1,5,9" (comma-separated target item ids)
    public Dictionary<int, string?> Relations { get; set; } = new();

    // Shared DateRange fields post two date inputs; combined into Values[fieldId] = "start|end".
    public Dictionary<int, string?> RangeStart { get; set; } = new();
    public Dictionary<int, string?> RangeEnd { get; set; } = new();

    // Per-language — one entry per active language, all present in the DOM at once (client-side
    // tabs, see content-item-tabs.js) so switching tabs never discards unsaved input.
    // Posted as Titles[lang] = rawTitle (content_item_titles.title)
    public Dictionary<string, string?> Titles { get; set; } = new();
    // Posted as LanguageActive[lang] = true|false (content_item_titles.is_active)
    public Dictionary<string, bool> LanguageActive { get; set; } = new();
    // Posted as LocalizedValues[lang][fieldId] = rawValue (content_field.is_localized = true)
    public Dictionary<string, Dictionary<int, string?>> LocalizedValues { get; set; } = new();
    // Per-language DateRange inputs; combined into LocalizedValues[lang][fieldId] = "start|end".
    public Dictionary<string, Dictionary<int, string?>> LocalizedRangeStart { get; set; } = new();
    public Dictionary<string, Dictionary<int, string?>> LocalizedRangeEnd { get; set; } = new();

    // Render-only (populated on GET; not posted back)
    public List<FieldRenderModel> Fields { get; set; } = new();

    // Render-only: the tenant's active languages, drives the language tabs at the top of the form.
    public IReadOnlyList<LanguageDto> AvailableLanguages { get; set; } = new List<LanguageDto>();

    // A language is worth saving if the user actually put something into it — the primary
    // language is always saved regardless. Without this guard, looping every active language on
    // every save would run FirstMissingRequired against languages the editor never opened this
    // session, blocking the whole save on a required localized field nobody has translated yet.
    public bool HasContent(string lang)
    {
        if (string.Equals(lang, PrimaryLanguageCode, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrWhiteSpace(Titles.GetValueOrDefault(lang))) return true;
        if (LanguageActive.GetValueOrDefault(lang)) return true;
        // "false" is skipped: every Boolean field posts a hidden "false" fallback (see
        // _FieldInput.cshtml), which would otherwise mark every language as "has content" and drag
        // untranslated languages into the save loop.
        if (LocalizedValues.TryGetValue(lang, out var vals) &&
            vals.Values.Any(v => !string.IsNullOrWhiteSpace(v) && v != "false"))
            return true;
        bool HasRange(Dictionary<string, Dictionary<int, string?>> ranges) =>
            ranges.TryGetValue(lang, out var r) && r.Values.Any(v => !string.IsNullOrWhiteSpace(v));
        return HasRange(LocalizedRangeStart) || HasRange(LocalizedRangeEnd);
    }

    public SaveContentItemRequest ToSaveRequest(string lang)
    {
        // Shared values, plus this language's localized values merged in — BuildValues() (Application
        // layer) re-stamps each field's language_code from field.IsLocalized, so passing the union is
        // correct: shared entries land on 'all' every pass, localized entries land on `lang`.
        var values = new Dictionary<int, string?>(Values);
        MergeRange(values, RangeStart, RangeEnd);

        if (LocalizedValues.TryGetValue(lang, out var localizedVals))
            foreach (var (fieldId, raw) in localizedVals)
                values[fieldId] = raw;

        var localizedRangeStart = LocalizedRangeStart.GetValueOrDefault(lang) ?? new();
        var localizedRangeEnd = LocalizedRangeEnd.GetValueOrDefault(lang) ?? new();
        MergeRange(values, localizedRangeStart, localizedRangeEnd);

        return new SaveContentItemRequest
        {
            ContentTypeId = ContentTypeId,
            LanguageCode = lang,
            Status = Status,
            Slug = string.IsNullOrWhiteSpace(Slug) ? null : Slug,
            Title = string.IsNullOrWhiteSpace(Titles.GetValueOrDefault(lang)) ? null : Titles[lang]!.Trim(),
            IsLanguageActive = LanguageActive.GetValueOrDefault(lang),
            Values = values,
            Relations = Relations.ToDictionary(
                kv => kv.Key,
                kv => (kv.Value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList())
        };
    }

    private static void MergeRange(
        Dictionary<int, string?> values, Dictionary<int, string?> start, Dictionary<int, string?> end)
    {
        foreach (var key in start.Keys.Union(end.Keys))
        {
            start.TryGetValue(key, out var s);
            end.TryGetValue(key, out var e);
            values[key] = string.IsNullOrEmpty(s) && string.IsNullOrEmpty(e) ? null : $"{s}|{e}";
        }
    }
}
