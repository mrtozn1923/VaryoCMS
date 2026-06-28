using VaryoCms.Application.Common;
using VaryoCms.Application.Common.FieldOptions;
using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class ContentItemService : IContentItemService
{
    private readonly IContentItemRepository _items;
    private readonly IContentFieldValueRepository _values;
    private readonly IContentFieldRepository _fields;
    private readonly IContentRelationRepository _relations;
    private readonly IMediaRepository _media;
    private readonly IContentItemTitleRepository _titles;
    private readonly ICurrentUserContext _currentUser;
    private readonly IValidator<SaveContentItemRequest> _validator;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public ContentItemService(
        IContentItemRepository items,
        IContentFieldValueRepository values,
        IContentFieldRepository fields,
        IContentRelationRepository relations,
        IMediaRepository media,
        IContentItemTitleRepository titles,
        ICurrentUserContext currentUser,
        IValidator<SaveContentItemRequest> validator,
        IStringLocalizer<SharedResource> t,
        IAuditLogger audit)
    {
        _items = items;
        _values = values;
        _fields = fields;
        _relations = relations;
        _media = media;
        _titles = titles;
        _currentUser = currentUser;
        _validator = validator;
        _t = t;
        _audit = audit;
    }

    private static bool IsRelation(FieldType type) => type is FieldType.Relation or FieldType.MultiRelation;

    public async Task<Result<PagedResult<ContentItemListItemDto>>> GetListAsync(
        int contentTypeId, string languageCode, int page, int pageSize,
        string? searchQuery = null, string? statusFilter = null, string? languageFilter = null,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (rows, total) = await _items.GetPagedListAsync(
            contentTypeId, languageCode, page, pageSize, searchQuery, statusFilter, languageFilter, ct);

        var ids = rows.Select(r => r.Id).ToList();
        var filledLangs = await _titles.GetFilledLanguagesAsync(ids, ct);

        IReadOnlyList<ContentItemListItemDto> dtos = rows.Select(r => new ContentItemListItemDto
        {
            Id = r.Id,
            Slug = r.Slug,
            Status = r.Status,
            Title = r.Title,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CreatedByName = r.CreatedByName,
            UpdatedByName = r.UpdatedByName,
            FilledLanguages = filledLangs.TryGetValue(r.Id, out var langs)
                ? langs
                : Array.Empty<(string Code, bool IsActive)>()
        }).ToList();

        return Result<PagedResult<ContentItemListItemDto>>.Success(
            new PagedResult<ContentItemListItemDto>(dtos, page, pageSize, total));
    }

    public async Task<Result<ContentItemEditDto>> GetForEditAsync(int itemId, string languageCode, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return Result<ContentItemEditDto>.Failure(_t["Err.ContentItemNotFound"]);

        var fields = await _fields.GetByContentTypeAsync(item.ContentTypeId, ct);
        var typeByFieldId = fields.ToDictionary(f => f.Id, f => f.FieldType);

        var values = await _values.GetByItemAsync(itemId, languageCode, ct);
        var raw = new Dictionary<int, string?>();
        foreach (var v in values)
            if (typeByFieldId.TryGetValue(v.ContentFieldId, out var type) && !IsRelation(type))
                raw[v.ContentFieldId] = FieldValueMapper.ToRaw(v, type);

        var relations = new Dictionary<int, List<RelatedItemDto>>();
        foreach (var field in fields.Where(f => IsRelation(f.FieldType)))
        {
            var options = RelationOptions.Parse(field.OptionsJson);
            if (options is null) continue;

            var targetIds = await _relations.GetTargetIdsAsync(itemId, field.Id, ct);
            if (targetIds.Count == 0) { relations[field.Id] = new(); continue; }

            var displays = await _relations.GetDisplayValuesAsync(
                options.TargetContentTypeId, options.DisplayFieldSlug, targetIds, languageCode, ct);
            relations[field.Id] = targetIds
                .Select(id => new RelatedItemDto { Id = id, DisplayValue = displays.TryGetValue(id, out var d) ? d : $"#{id}" })
                .ToList();
        }

        var titleData = await _titles.GetTitleAsync(itemId, languageCode, ct);

        return Result<ContentItemEditDto>.Success(new ContentItemEditDto
        {
            Id = item.Id,
            ContentTypeId = item.ContentTypeId,
            Slug = item.Slug,
            Status = item.Status,
            LanguageCode = languageCode,
            Title = titleData?.Title,
            IsLanguageActive = titleData?.IsActive ?? false,
            Values = raw,
            Relations = relations
        });
    }

    public async Task<Result<int>> CreateAsync(SaveContentItemRequest request, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result<int>.Failure(FirstError(validation));

        var fields = await _fields.GetByContentTypeAsync(request.ContentTypeId, ct);
        var missing = FirstMissingRequired(fields, request);
        if (missing is not null) return Result<int>.Failure(_t["Err.FieldRequired", missing]);

        var countError = FirstRelationCountError(fields, request);
        if (countError is not null) return Result<int>.Failure(countError);

        var optError = await FirstFieldOptionErrorAsync(fields, request, ct);
        if (optError is not null) return Result<int>.Failure(optError);

        string? slug = await BuildUniqueSlugAsync(request.ContentTypeId, request.Slug, request.Title, null, ct);

        int itemId = await _items.CreateAsync(new ContentItem
        {
            ContentTypeId = request.ContentTypeId,
            Slug = slug,
            Status = request.Status,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        }, ct);

        await _titles.SaveTitleAsync(itemId, request.LanguageCode, request.Title, request.IsLanguageActive, ct);
        await _values.SaveValuesAsync(itemId, BuildValues(fields, request), ct);
        await SaveRelationsAsync(itemId, fields, request, ct);
        await _audit.LogAsync(AuditActions.ContentItemCreated, "ContentItem", itemId,
            request.ContentTypeId, request.Title, ct: ct);
        return Result<int>.Success(itemId);
    }

    public async Task<Result> UpdateAsync(int itemId, SaveContentItemRequest request, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result.Failure(FirstError(validation));

        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return Result.Failure(_t["Err.ContentItemNotFound"]);

        var fields = await _fields.GetByContentTypeAsync(item.ContentTypeId, ct);
        var missing = FirstMissingRequired(fields, request);
        if (missing is not null) return Result.Failure(_t["Err.FieldRequired", missing]);

        var countError = FirstRelationCountError(fields, request);
        if (countError is not null) return Result.Failure(countError);

        var optError = await FirstFieldOptionErrorAsync(fields, request, ct);
        if (optError is not null) return Result.Failure(optError);

        item.Slug = await BuildUniqueSlugAsync(item.ContentTypeId, request.Slug, request.Title, itemId, ct);
        item.Status = request.Status;
        item.UpdatedBy = _currentUser.UserId;
        await _items.UpdateAsync(item, ct);

        await _titles.SaveTitleAsync(itemId, request.LanguageCode, request.Title, request.IsLanguageActive, ct);
        await _values.SaveValuesAsync(itemId, BuildValues(fields, request), ct);
        await SaveRelationsAsync(itemId, fields, request, ct);
        await _audit.LogAsync(AuditActions.ContentItemUpdated, "ContentItem", itemId,
            item.ContentTypeId, request.Title, ct: ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<RelatedItemDto>>> SearchRelatedAsync(
        int targetContentTypeId, string? displayFieldSlug, string? query, string languageCode, CancellationToken ct = default)
    {
        var matches = await _relations.SearchTargetsAsync(targetContentTypeId, displayFieldSlug, query, languageCode, limit: 20, ct);
        IReadOnlyList<RelatedItemDto> dtos = matches
            .Select(m => new RelatedItemDto { Id = m.Id, DisplayValue = m.Display })
            .ToList();
        return Result<IReadOnlyList<RelatedItemDto>>.Success(dtos);
    }

    public async Task<Result> DeleteAsync(int itemId, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        bool deleted = await _items.SoftDeleteAsync(itemId, ct);
        if (!deleted) return Result.Failure(_t["Err.ContentItemNotFound"]);
        await _audit.LogAsync(AuditActions.ContentItemDeleted, "ContentItem", itemId,
            item?.ContentTypeId, item?.Slug, ct: ct);
        return Result.Success();
    }

    // Generates a unique slug: uses the provided slug if non-empty, otherwise derives from title.
    // Appends -2, -3, ... until the slug is not taken (tenant+content_type scoped).
    private async Task<string?> BuildUniqueSlugAsync(
        int contentTypeId, string? requestedSlug, string? title, int? excludeItemId, CancellationToken ct)
    {
        string? baseSlug = !string.IsNullOrWhiteSpace(requestedSlug)
            ? requestedSlug
            : (!string.IsNullOrWhiteSpace(title) ? Slugifier.ToSlug(title) : null);

        if (string.IsNullOrEmpty(baseSlug)) return null;

        string candidate = baseSlug;
        int suffix = 2;
        while (await _items.SlugExistsAsync(contentTypeId, candidate, excludeItemId, ct))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return candidate;
    }

    // Relation/MultiRelation fields are stored in content_field_relations, not content_field_values.
    private async Task SaveRelationsAsync(
        int itemId, IReadOnlyList<ContentField> fields, SaveContentItemRequest request, CancellationToken ct)
    {
        foreach (var field in fields.Where(f => IsRelation(f.FieldType)))
        {
            var targets = request.Relations.TryGetValue(field.Id, out var ids) ? ids : new List<int>();
            // Relation = single target: keep only the first.
            if (field.FieldType == FieldType.Relation && targets.Count > 1)
                targets = targets.Take(1).ToList();
            await _relations.ReplaceAsync(itemId, field.Id, targets, ct);
        }
    }

    private static List<ContentFieldValue> BuildValues(
        IReadOnlyList<ContentField> fields, SaveContentItemRequest request)
    {
        var result = new List<ContentFieldValue>();
        foreach (var field in fields)
        {
            if (IsRelation(field.FieldType)) continue;   // stored separately
            if (!request.Values.TryGetValue(field.Id, out var rawValue)) continue;

            var value = new ContentFieldValue
            {
                ContentFieldId = field.Id,
                LanguageCode = field.IsLocalized ? request.LanguageCode : "all"
            };
            FieldValueMapper.Apply(value, field.FieldType, rawValue);
            result.Add(value);
        }
        return result;
    }

    private static string? FirstMissingRequired(
        IReadOnlyList<ContentField> fields, SaveContentItemRequest request)
    {
        foreach (var field in fields.Where(f => f.IsRequired))
        {
            if (IsRelation(field.FieldType))
            {
                bool hasTarget = request.Relations.TryGetValue(field.Id, out var ids) && ids.Count > 0;
                if (!hasTarget) return field.Name;
                continue;
            }

            request.Values.TryGetValue(field.Id, out var raw);
            if (string.IsNullOrWhiteSpace(raw)) return field.Name;
        }
        return null;
    }

    // Enforces min_items/max_items from a MultiRelation field's options_json.
    // min_items is checked when the field has input or is required; max_items is always checked.
    private string? FirstRelationCountError(
        IReadOnlyList<ContentField> fields, SaveContentItemRequest request)
    {
        foreach (var field in fields.Where(f => f.FieldType == FieldType.MultiRelation))
        {
            var options = RelationOptions.Parse(field.OptionsJson);
            if (options is null) continue;

            int count = request.Relations.TryGetValue(field.Id, out var ids) ? ids.Count : 0;

            if ((count > 0 || field.IsRequired) && options.MinItems is int min && count < min)
                return _t["Err.RelationMin", field.Name, min].Value;

            if (options.MaxItems is int max && count > max)
                return _t["Err.RelationMax", field.Name, max].Value;
        }
        return null;
    }

    private async Task<string?> FirstFieldOptionErrorAsync(
        IReadOnlyList<ContentField> fields, SaveContentItemRequest request, CancellationToken ct)
    {
        foreach (var field in fields)
        {
            if (IsRelation(field.FieldType)) continue;
            request.Values.TryGetValue(field.Id, out var raw);
            if (string.IsNullOrWhiteSpace(raw)) continue;

            switch (field.FieldType)
            {
                case FieldType.Text:
                {
                    var opts = TextOptions.Parse(field.OptionsJson);
                    if (opts.MaxLength is int ml && raw.Length > ml)
                        return _t["Err.Field.TooLong", field.Name, ml].Value;
                    break;
                }
                case FieldType.Number:
                case FieldType.Decimal:
                {
                    var opts = NumberOptions.Parse(field.OptionsJson);
                    if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var num))
                    {
                        if (opts.Min is decimal mn && num < mn)
                            return _t["Err.Field.OutOfRange", field.Name, mn, opts.Max?.ToString() ?? "∞"].Value;
                        if (opts.Max is decimal mx && num > mx)
                            return _t["Err.Field.OutOfRange", field.Name, opts.Min?.ToString() ?? "-∞", mx].Value;
                        if (opts.Decimals is int dc)
                        {
                            int actualDecimals = BitConverter.GetBytes(decimal.GetBits(num)[3])[2];
                            if (actualDecimals > dc)
                                return _t["Err.Field.TooManyDecimals", field.Name, dc].Value;
                        }
                    }
                    break;
                }
                case FieldType.Rating:
                {
                    var opts = RatingOptions.Parse(field.OptionsJson);
                    if (decimal.TryParse(raw, out var rating) && (rating < 1 || rating > opts.Max))
                        return _t["Err.Field.RatingRange", field.Name, opts.Max].Value;
                    break;
                }
                case FieldType.Select:
                {
                    var opts = SelectOptions.Parse(field.OptionsJson);
                    if (opts.Choices.Count > 0 && !opts.Choices.Contains(raw))
                        return _t["Err.Field.InvalidChoice", field.Name].Value;
                    break;
                }
                case FieldType.MultiSelect:
                {
                    var opts = SelectOptions.Parse(field.OptionsJson);
                    if (opts.Choices.Count > 0)
                    {
                        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim());
                        if (parts.Any(p => !opts.Choices.Contains(p)))
                            return _t["Err.Field.InvalidChoice", field.Name].Value;
                    }
                    break;
                }
                case FieldType.Image:
                case FieldType.Video:
                case FieldType.Audio:
                case FieldType.File:
                case FieldType.Gallery:
                {
                    var opts = MediaOptions.Parse(field.OptionsJson);
                    if (!opts.MaxSizeMb.HasValue && !opts.HasFormatRestriction) break;

                    var ids = ParseMediaIdsForValidation(field.FieldType, raw);
                    if (ids.Count == 0) break;

                    var assets = await _media.GetByIdsAsync(ids, ct);
                    foreach (var asset in assets)
                    {
                        if (opts.MaxSizeBytes is long maxBytes && asset.FileSizeBytes > maxBytes)
                            return _t["Err.Field.MediaTooLarge", field.Name, opts.MaxSizeMb!.Value].Value;

                        if (opts.HasFormatRestriction)
                        {
                            var ext = System.IO.Path.GetExtension(asset.OriginalName);
                            if (!opts.IsFormatAllowed(ext))
                                return _t["Err.Field.MediaFormat", field.Name,
                                    string.Join(", ", opts.AllowedFormats)].Value;
                        }
                    }
                    break;
                }
            }
        }
        return null;
    }

    private static List<int> ParseMediaIdsForValidation(FieldType type, string raw)
    {
        if (type != FieldType.Gallery)
            return int.TryParse(raw, out var id) && id > 0 ? new List<int> { id } : new();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return new();
            return doc.RootElement.EnumerateArray()
                .Where(e => e.TryGetInt32(out _)).Select(e => e.GetInt32()).Where(i => i > 0).ToList();
        }
        catch { return new(); }
    }

    private static string FirstError(FluentValidation.Results.ValidationResult result)
        => result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
}
