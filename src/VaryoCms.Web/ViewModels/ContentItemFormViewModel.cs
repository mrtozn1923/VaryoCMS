using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Web.ViewModels;

public class ContentItemFormViewModel
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    public int ItemId { get; set; }          // 0 = create
    public string LanguageCode { get; set; } = "tr";
    public string Status { get; set; } = "draft";
    public string? Slug { get; set; }
    public string? Title { get; set; }
    public bool IsLanguageActive { get; set; }

    // Posted as Values[fieldId] = rawValue
    public Dictionary<int, string?> Values { get; set; } = new();

    // Posted as Relations[fieldId] = "1,5,9" (comma-separated target item ids)
    public Dictionary<int, string?> Relations { get; set; } = new();

    // DateRange fields post two date inputs; combined into Values[fieldId] = "start|end".
    public Dictionary<int, string?> RangeStart { get; set; } = new();
    public Dictionary<int, string?> RangeEnd { get; set; } = new();

    // Render-only (populated on GET; not posted back)
    public List<FieldRenderModel> Fields { get; set; } = new();

    // Render-only: the tenant's active languages, drives the language tabs at the top of the form.
    public IReadOnlyList<LanguageDto> AvailableLanguages { get; set; } = new List<LanguageDto>();

    public SaveContentItemRequest ToSaveRequest()
    {
        // Merge DateRange start/end inputs into the combined "start|end" value per field.
        var values = new Dictionary<int, string?>(Values);
        foreach (var key in RangeStart.Keys.Union(RangeEnd.Keys))
        {
            RangeStart.TryGetValue(key, out var s);
            RangeEnd.TryGetValue(key, out var e);
            values[key] = string.IsNullOrEmpty(s) && string.IsNullOrEmpty(e) ? null : $"{s}|{e}";
        }

        return new SaveContentItemRequest
        {
            ContentTypeId = ContentTypeId,
            LanguageCode = LanguageCode,
            Status = Status,
            Slug = string.IsNullOrWhiteSpace(Slug) ? null : Slug,
            Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
            IsLanguageActive = IsLanguageActive,
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
}
