using VaryoCms.Application.Common;
using VaryoCms.Application.Common.FieldOptions;
using VaryoCms.Application.DTOs.ContentItem;
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
        int contentTypeId, int? itemId, string languageCode, CancellationToken ct)
    {
        var type = await _types.GetByIdAsync(contentTypeId, ct);
        if (!type.IsSuccess) return null;

        var fields = await _fields.GetByContentTypeAsync(contentTypeId, ct);

        var vm = new ContentItemFormViewModel
        {
            ContentTypeId = contentTypeId,
            ContentTypeName = type.Value!.Name,
            LanguageCode = languageCode,
            AvailableLanguages = await _languages.GetActiveAsync(ct)
        };

        Dictionary<int, string?> values = new();
        Dictionary<int, List<RelatedItemDto>> relations = new();
        if (itemId is int id)
        {
            var edit = await _items.GetForEditAsync(id, languageCode, ct);
            if (!edit.IsSuccess) return null;
            vm.ItemId = edit.Value!.Id;
            vm.Status = edit.Value.Status;
            vm.Slug = edit.Value.Slug;
            vm.Title = edit.Value.Title;
            vm.IsLanguageActive = edit.Value.IsLanguageActive;
            values = edit.Value.Values;
            relations = edit.Value.Relations;
        }

        vm.Values = values;

        // Pre-resolve media for media fields in one batch (the field-mapping below is synchronous).
        var mediaIdsByField = new Dictionary<int, List<int>>();
        foreach (var f in fields.Value!)
            if (IsMedia(f.FieldType))
            {
                var ids = ParseMediaIds(f.FieldType, values.TryGetValue(f.Id, out var rv) ? rv : null);
                if (ids.Count > 0) mediaIdsByField[f.Id] = ids;
            }
        var mediaById = (await _media.GetByIdsAsync(
            mediaIdsByField.Values.SelectMany(x => x).Distinct().ToList(), ct)).ToDictionary(m => m.Id);

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
                Value = values.TryGetValue(f.Id, out var v) ? v : null,
                LanguageCode = languageCode
            };

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
                if (mediaIdsByField.TryGetValue(f.Id, out var ids))
                    model.SelectedMedia = ids.Where(mediaById.ContainsKey).Select(id => mediaById[id]).ToList();
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
