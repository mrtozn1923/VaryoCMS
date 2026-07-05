using VaryoCms.Application.Common;
using VaryoCms.Application.Common.FieldOptions;
using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.DTOs.Media;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;
using VaryoCms.Web.ViewModels;

namespace VaryoCms.Web.Support;

// Composes the dynamic item form (content type header + field definitions + current values)
// from the application services, keeping the controller thin.
public class ContentItemFormFactory
{
    private readonly IContentTypeService _types;
    private readonly IContentFieldService _fields;
    private readonly IContentItemService _items;
    private readonly IMediaService _media;
    private readonly ILanguageService _languages;

    public ContentItemFormFactory(
        IContentTypeService types, IContentFieldService fields, IContentItemService items,
        IMediaService media, ILanguageService languages)
    {
        _types = types;
        _fields = fields;
        _items = items;
        _media = media;
        _languages = languages;
    }

    private static bool IsMedia(VaryoCms.Domain.Enums.FieldType t) =>
        t is FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File or FieldType.Gallery;

    private static string? MediaTypeFilter(FieldType t) => t switch
    {
        FieldType.Image or FieldType.Gallery => "image",
        FieldType.Video => "video",
        FieldType.Audio => "audio",
        _ => null   // File = any
    };

    // Single media fields store one media id ("12"); Gallery stores a JSON array ("[12,15]").
    private static List<int> ParseMediaIds(FieldType type, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        if (type != FieldType.Gallery)
            return int.TryParse(raw, out var id) && id > 0 ? new List<int> { id } : new();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return new();
            return doc.RootElement.EnumerateArray()
                .Where(e => e.TryGetInt32(out _)).Select(e => e.GetInt32()).Where(i => i > 0).ToList();
        }
        catch (System.Text.Json.JsonException) { return new(); }
    }

    public async Task<ContentItemFormViewModel?> BuildAsync(
        int contentTypeId, int? itemId, string primaryLanguageCode, CancellationToken ct)
    {
        var type = await _types.GetByIdAsync(contentTypeId, ct);
        if (!type.IsSuccess) return null;

        var fields = await _fields.GetByContentTypeAsync(contentTypeId, ct);
        var fieldById = fields.Value!.ToDictionary(f => f.Id);

        var activeLanguages = await _languages.GetActiveAsync(ct);
        var languageCodes = activeLanguages.Count > 0
            ? activeLanguages.Select(l => l.Code).ToList()
            : new List<string> { primaryLanguageCode };
        if (!languageCodes.Contains(primaryLanguageCode)) languageCodes.Insert(0, primaryLanguageCode);

        var vm = new ContentItemFormViewModel
        {
            ContentTypeId = contentTypeId,
            ContentTypeName = type.Value!.Name,
            PrimaryLanguageCode = primaryLanguageCode,
            AvailableLanguages = activeLanguages
        };

        // Shared (is_localized=false) field values, same regardless of language. Localized values
        // keyed by language — every active language is loaded up front (not just the primary one)
        // so the client-side tabs (content-item-tabs.js) never need to reload the page to switch.
        Dictionary<int, string?> sharedValues = new();
        Dictionary<string, Dictionary<int, string?>> localizedValues = new();
        Dictionary<int, List<RelatedItemDto>> relations = new();

        if (itemId is int id)
        {
            var editByLang = new Dictionary<string, ContentItemEditDto>();
            foreach (var lang in languageCodes)
            {
                var edit = await _items.GetForEditAsync(id, lang, ct);
                if (!edit.IsSuccess) return null;
                // Guard: the item must belong to the content type this form was opened for (route
                // contentTypeId, already permission-checked). Prevents reading another type's item
                // values by crafting /admin/content-types/{A}/items/{B_item_id}/edit.
                if (edit.Value!.ContentTypeId != contentTypeId) return null;
                editByLang[lang] = edit.Value;
            }

            var primaryEdit = editByLang[primaryLanguageCode];
            vm.ItemId = primaryEdit.Id;
            vm.Status = primaryEdit.Status;
            vm.Slug = primaryEdit.Slug;
            relations = primaryEdit.Relations;

            // GetForEditAsync returns rows for language_code IN (@Lang, 'all') mixed together —
            // split them back out by the owning field's IsLocalized flag.
            foreach (var (fieldId, raw) in primaryEdit.Values)
                if (fieldById.TryGetValue(fieldId, out var f) && !f.IsLocalized)
                    sharedValues[fieldId] = raw;

            foreach (var lang in languageCodes)
            {
                vm.Titles[lang] = editByLang[lang].Title;
                vm.LanguageActive[lang] = editByLang[lang].IsLanguageActive;

                var perLang = new Dictionary<int, string?>();
                foreach (var (fieldId, raw) in editByLang[lang].Values)
                    if (fieldById.TryGetValue(fieldId, out var f) && f.IsLocalized)
                        perLang[fieldId] = raw;
                localizedValues[lang] = perLang;
            }
        }

        vm.Values = sharedValues;

        // Pre-resolve media for media fields in one batch (shared + every language's localized ids).
        var sharedMediaIdsByField = new Dictionary<int, List<int>>();
        var localizedMediaIdsByField = new Dictionary<int, Dictionary<string, List<int>>>();
        foreach (var f in fields.Value!)
        {
            if (!IsMedia(f.FieldType)) continue;
            if (f.IsLocalized)
            {
                var perLang = new Dictionary<string, List<int>>();
                foreach (var lang in languageCodes)
                {
                    var raw = localizedValues.TryGetValue(lang, out var lv) && lv.TryGetValue(f.Id, out var rv) ? rv : null;
                    var ids = ParseMediaIds(f.FieldType, raw);
                    if (ids.Count > 0) perLang[lang] = ids;
                }
                if (perLang.Count > 0) localizedMediaIdsByField[f.Id] = perLang;
            }
            else
            {
                var ids = ParseMediaIds(f.FieldType, sharedValues.TryGetValue(f.Id, out var rv) ? rv : null);
                if (ids.Count > 0) sharedMediaIdsByField[f.Id] = ids;
            }
        }
        var allMediaIds = sharedMediaIdsByField.Values.SelectMany(x => x)
            .Concat(localizedMediaIdsByField.Values.SelectMany(d => d.Values.SelectMany(x => x)))
            .Distinct().ToList();
        var mediaById = (await _media.GetByIdsAsync(allMediaIds, ct)).ToDictionary(m => m.Id);

        vm.Fields = fields.Value!.Select(f =>
        {
            var model = new FieldRenderModel
            {
                Id = f.Id,
                Name = f.Name,
                Slug = f.Slug,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                IsLocalized = f.IsLocalized,
                OptionsJson = f.OptionsJson,
                LanguageCode = primaryLanguageCode
            };

            if (f.IsLocalized)
                model.ValuesByLang = languageCodes.ToDictionary(
                    lang => lang,
                    lang => localizedValues.TryGetValue(lang, out var lv) && lv.TryGetValue(f.Id, out var v) ? v : null);
            else
                model.Value = sharedValues.TryGetValue(f.Id, out var v) ? v : null;

            if (f.FieldType is FieldType.Relation or FieldType.MultiRelation)
            {
                var options = RelationOptions.Parse(f.OptionsJson);
                model.RelationTargetTypeId = options?.TargetContentTypeId;
                model.RelationDisplayField = options?.DisplayFieldSlug;
                model.RelationMultiple = f.FieldType == FieldType.MultiRelation;
                model.RelationMinItems = options?.MinItems;
                model.RelationMaxItems = options?.MaxItems;
                model.SelectedRelations = relations.TryGetValue(f.Id, out var sel) ? sel : new();
            }

            if (IsMedia(f.FieldType))
            {
                model.MediaMultiple = f.FieldType == FieldType.Gallery;
                model.MediaTypeFilter = MediaTypeFilter(f.FieldType);
                if (f.IsLocalized)
                {
                    model.SelectedMediaByLang = languageCodes.ToDictionary(
                        lang => lang,
                        lang => localizedMediaIdsByField.TryGetValue(f.Id, out var perLang) && perLang.TryGetValue(lang, out var ids)
                            ? ids.Where(mediaById.ContainsKey).Select(mid => mediaById[mid]).ToList()
                            : new List<MediaAssetDto>());
                }
                else if (sharedMediaIdsByField.TryGetValue(f.Id, out var sids))
                {
                    model.SelectedMedia = sids.Where(mediaById.ContainsKey).Select(mid => mediaById[mid]).ToList();
                }
                var mo = MediaOptions.Parse(f.OptionsJson);
                model.MediaMaxSizeMb = mo.MaxSizeMb;
                model.MediaAllowedFormats = mo.AllowedFormats;
            }

            if (f.FieldType is FieldType.Text)
            {
                var to = TextOptions.Parse(f.OptionsJson);
                model.TextMaxLength = to.MaxLength;
                model.Placeholder = to.Placeholder;
            }
            else if (f.FieldType is FieldType.RichText or FieldType.Markdown)
            {
                var to = TextOptions.Parse(f.OptionsJson);
                model.Placeholder = to.Placeholder;
            }
            else if (f.FieldType is FieldType.Number or FieldType.Decimal)
            {
                var no = NumberOptions.Parse(f.OptionsJson);
                model.NumberMin = no.Min;
                model.NumberMax = no.Max;
                model.NumberStep = no.Decimals.HasValue
                    ? (no.Decimals.Value == 0 ? "1" : Math.Pow(10, -no.Decimals.Value).ToString("0.####################"))
                    : "any";
            }
            else if (f.FieldType is FieldType.Rating)
            {
                model.RatingMax = RatingOptions.Parse(f.OptionsJson).Max;
            }
            else if (f.FieldType is FieldType.Select or FieldType.MultiSelect)
            {
                model.SelectChoices = SelectOptions.Parse(f.OptionsJson).Choices;
            }
            else if (f.FieldType is FieldType.CodeSnippet)
            {
                model.CodeLanguage = CodeSnippetOptions.Parse(f.OptionsJson).Language;
            }

            return model;
        }).ToList();

        return vm;
    }
}
