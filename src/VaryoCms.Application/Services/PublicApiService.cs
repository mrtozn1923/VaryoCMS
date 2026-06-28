using System.Text.Json;
using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;        // FieldType, RelationOptions
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Application.Services;

public class PublicApiService : IPublicApiService
{
    private const int MaxPageSize = 100;
    private const string PublishedStatus = "published";   // public API only ever exposes published items

    // Maps the camelCase public sort key to the internal SQL column name.
    private static readonly Dictionary<string, string> SortableItemColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "id",          "id" },
            { "createdAt",   "created_at" },
            { "updatedAt",   "updated_at" },
            { "publishedAt", "published_at" },
            { "status",      "status" }
        };

    private readonly ITenantStore _tenantStore;
    private readonly IPublicApiRepository _repo;
    private readonly IPublicApiWriteRepository _writeRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IResponseCache _cache;
    private readonly IAuditLogger _audit;

    public PublicApiService(
        ITenantStore tenantStore, IPublicApiRepository repo, IPublicApiWriteRepository writeRepo,
        IPasswordHasher passwordHasher, IJwtTokenService jwt, IResponseCache cache, IAuditLogger audit)
    {
        _tenantStore = tenantStore;
        _repo = repo;
        _writeRepo = writeRepo;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _cache = cache;
        _audit = audit;
    }

    public async Task<Result<ApiListResponse>> GetListAsync(
        string tenantSlug, string contentTypeSlug, ApiListRequest request, ApiCredentials credentials, CancellationToken ct = default)
    {
        var ctx = await ResolveAsync(tenantSlug, contentTypeSlug, credentials, ct);
        if (ctx.Error is not null) return Result<ApiListResponse>.Failure(ctx.Error);
        var (tenant, config) = (ctx.Tenant!, ctx.Config!);

        string lang = string.IsNullOrWhiteSpace(request.Lang) ? tenant.DefaultLanguageCode : request.Lang.Trim();

        int page = config.AllowPagination ? Math.Max(1, request.Page) : 1;
        int pageSize = config.AllowPagination
            ? Math.Clamp(request.PageSize, 1, MaxPageSize)
            : MaxPageSize;

        // Filtering/sorting are resolved against the visible fields (you can only filter/sort on exposed fields).
        var visibleFields = await _repo.GetVisibleFieldsAsync(config.Id, ct);
        var sort = ResolveSort(request.Sort, config.AllowSorting, visibleFields);
        var filters = ResolveFilters(request.Filters, config.AllowFiltering, visibleFields);

        // Auth has already passed; cache content keyed by the params that affect the response.
        string filterKey = request.Filters is null || request.Filters.Count == 0
            ? string.Empty
            : string.Join("&", request.Filters.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}"));
        string? cacheKey = config.CacheSeconds > 0
            ? $"api:{tenant.Id}:{config.ContentTypeId}:list:{lang}:{page}:{pageSize}:{request.Sort}:{request.Fields}:{filterKey}"
            : null;
        if (cacheKey is not null && _cache.TryGet<ApiListResponse>(cacheKey, out var cachedList) && cachedList is not null)
            return Result<ApiListResponse>.Success(cachedList);

        var query = new ApiItemQuery(
            tenant.Id, config.ContentTypeId, PublishedStatus, lang,
            (page - 1) * pageSize, pageSize,
            sort.ItemColumn, sort.FieldId, sort.FieldColumn, sort.Desc, filters);
        var (items, total) = await _repo.GetItemsAsync(query, ct);

        var data = await BuildItemsAsync(tenant.Id, items, lang, request.Fields, visibleFields, ct);

        var response = new ApiListResponse
        {
            Data = data,
            Pagination = new ApiPaginationDto
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0
            }
        };

        if (cacheKey is not null)
            _cache.Set(cacheKey, response, TimeSpan.FromSeconds(config.CacheSeconds!.Value));

        return Result<ApiListResponse>.Success(response);
    }

    public async Task<Result<ApiItemDto>> GetByIdAsync(
        string tenantSlug, string contentTypeSlug, int id, string? lang, ApiCredentials credentials, CancellationToken ct = default)
    {
        var ctx = await ResolveAsync(tenantSlug, contentTypeSlug, credentials, ct);
        if (ctx.Error is not null) return Result<ApiItemDto>.Failure(ctx.Error);
        var (tenant, config) = (ctx.Tenant!, ctx.Config!);

        string code = string.IsNullOrWhiteSpace(lang) ? tenant.DefaultLanguageCode : lang.Trim();
        string? cacheKey = config.CacheSeconds > 0 ? $"api:{tenant.Id}:{config.ContentTypeId}:id:{id}:{code}" : null;
        if (cacheKey is not null && _cache.TryGet<ApiItemDto>(cacheKey, out var cached) && cached is not null)
            return Result<ApiItemDto>.Success(cached);

        var item = await _repo.GetItemByIdAsync(tenant.Id, config.ContentTypeId, id, code, ct);
        return await BuildSingleAsync(tenant.Id, config, item, code, cacheKey, ct);
    }

    public async Task<Result<ApiItemDto>> GetBySlugAsync(
        string tenantSlug, string contentTypeSlug, string slug, string? lang, ApiCredentials credentials, CancellationToken ct = default)
    {
        var ctx = await ResolveAsync(tenantSlug, contentTypeSlug, credentials, ct);
        if (ctx.Error is not null) return Result<ApiItemDto>.Failure(ctx.Error);
        var (tenant, config) = (ctx.Tenant!, ctx.Config!);

        string code = string.IsNullOrWhiteSpace(lang) ? tenant.DefaultLanguageCode : lang.Trim();
        string? cacheKey = config.CacheSeconds > 0 ? $"api:{tenant.Id}:{config.ContentTypeId}:slug:{slug}:{code}" : null;
        if (cacheKey is not null && _cache.TryGet<ApiItemDto>(cacheKey, out var cached) && cached is not null)
            return Result<ApiItemDto>.Success(cached);

        var item = await _repo.GetItemBySlugAsync(tenant.Id, config.ContentTypeId, slug, code, ct);
        return await BuildSingleAsync(tenant.Id, config, item, code, cacheKey, ct);
    }

    // --- helpers -------------------------------------------------------------

    private async Task<(TenantInfo? Tenant, ApiConfiguration? Config, string? Error)> ResolveAsync(
        string tenantSlug, string contentTypeSlug, ApiCredentials credentials, CancellationToken ct)
    {
        var tenant = await _tenantStore.FindBySlugAsync(tenantSlug, ct);
        if (tenant is null) return (null, null, "NotFound");

        var config = await _repo.GetEnabledConfigAsync(tenant.Id, contentTypeSlug, ct);
        if (config is null) return (null, null, "NotFound");

        var (authorized, identity) = await ResolveCredentialAsync(config, tenant.Id, tenantSlug, contentTypeSlug, credentials, allowPublic: true, ct);
        if (!authorized)
        {
            await _audit.LogAsync(AuditActions.ApiAuthFailed, "ApiCredential", config.ContentTypeId,
                contentTypeId: config.ContentTypeId, entityName: contentTypeSlug,
                tenantIdOverride: tenant.Id, userEmailOverride: identity, ct: ct);
            return (null, null, "Unauthorized");
        }

        if (!config.AllowRead)
        {
            await _audit.LogAsync(AuditActions.ApiVerbForbidden, "ApiCredential", config.ContentTypeId,
                contentTypeId: config.ContentTypeId, entityName: contentTypeSlug,
                tenantIdOverride: tenant.Id, userEmailOverride: identity, ct: ct);
            return (null, null, "Forbidden");
        }

        return (tenant, config, null);
    }

    // Resolves credential and returns (isValid, auditIdentity). allowPublic=true skips auth for public configs.
    private async Task<(bool Valid, string Identity)> ResolveCredentialAsync(
        ApiConfiguration config, int tenantId, string tenantSlug, string contentTypeSlug,
        ApiCredentials credentials, bool allowPublic, CancellationToken ct)
    {
        if (allowPublic && config.IsPublic) return (true, "api:public");

        if (!string.IsNullOrEmpty(credentials.ApiKey) && TryParseApiKey(credentials.ApiKey, out int credId, out string secret))
        {
            string? hash = await _repo.GetApiKeyGrantHashAsync(tenantId, credId, config.ContentTypeId, ct);
            if (hash is not null && _passwordHasher.Verify(secret, hash))
            {
                string name = await _repo.GetCredentialNameAsync(tenantId, credId, ct) ?? credId.ToString();
                return (true, $"api:{name}");
            }
        }

        if (!string.IsNullOrEmpty(credentials.BearerToken)
            && _jwt.ValidateToken(credentials.BearerToken, tenantSlug, contentTypeSlug))
            return (true, $"api:jwt:{contentTypeSlug}");

        return (false, "api:unknown");
    }

    // Parses the vk_{id}_{secret} format. Returns false for any malformed key.
    private static bool TryParseApiKey(string key, out int credentialId, out string secret)
    {
        credentialId = 0;
        secret = string.Empty;
        if (!key.StartsWith("vk_", StringComparison.Ordinal)) return false;
        int second = key.IndexOf('_', 3);
        if (second < 4) return false;
        if (!int.TryParse(key.AsSpan(3, second - 3), out credentialId)) return false;
        secret = key[(second + 1)..];
        return secret.Length > 0;
    }

    private async Task<Result<ApiItemDto>> BuildSingleAsync(
        int tenantId, ApiConfiguration config, ContentItem? item, string code, string? cacheKey, CancellationToken ct)
    {
        if (item is null) return Result<ApiItemDto>.Failure("NotFound");
        var visibleFields = await _repo.GetVisibleFieldsAsync(config.Id, ct);
        var data = await BuildItemsAsync(tenantId, new[] { item }, code, fields: null, visibleFields, ct);
        if (cacheKey is not null)
            _cache.Set(cacheKey, data[0], TimeSpan.FromSeconds(config.CacheSeconds!.Value));
        return Result<ApiItemDto>.Success(data[0]);
    }

    private async Task<IReadOnlyList<ApiItemDto>> BuildItemsAsync(
        int tenantId, IReadOnlyList<ContentItem> items, string lang, string? fields,
        IReadOnlyList<(ContentField Field, string? Alias)> visibleFields, CancellationToken ct)
    {
        if (items.Count == 0) return Array.Empty<ApiItemDto>();

        var itemIds = items.Select(i => i.Id).ToList();
        var values = await _repo.GetValuesAsync(tenantId, itemIds, lang, ct);

        // item id -> (field id -> value), preferring the requested language over the 'all' fallback.
        var valuesByItem = values
            .GroupBy(v => v.ContentItemId)
            .ToDictionary(g => g.Key, g => g
                .GroupBy(v => v.ContentFieldId)
                .ToDictionary(fg => fg.Key, fg =>
                    fg.FirstOrDefault(v => string.Equals(v.LanguageCode.Trim(), lang, StringComparison.OrdinalIgnoreCase))
                    ?? fg.First()));

        // Relation/MultiRelation expansion (only when such fields are visible).
        var (targetsByItemField, displaysByField) = await LoadRelationsAsync(tenantId, itemIds, visibleFields, lang, ct);
        // Media/Gallery expansion: resolve all referenced media ids to web paths in one query.
        var mediaUrls = await LoadMediaUrlsAsync(tenantId, items, valuesByItem, visibleFields, ct);

        HashSet<string>? projection = ParseProjection(fields);

        return items.Select(item =>
        {
            valuesByItem.TryGetValue(item.Id, out var fieldValues);
            var dto = new ApiItemDto { Id = item.Id, Slug = item.Slug };

            foreach (var (field, alias) in visibleFields)
            {
                if (projection is not null && !projection.Contains(ExternalKey(field.Slug, alias))) continue;
                string key = ExternalKey(field.Slug, alias);

                if (IsRelation(field.FieldType))
                {
                    dto.Fields[key] = ExpandRelation(item.Id, field, targetsByItemField, displaysByField);
                    continue;
                }

                if (IsMedia(field.FieldType))
                {
                    var cfv = fieldValues is not null && fieldValues.TryGetValue(field.Id, out var mv) ? mv : null;
                    dto.Fields[key] = ExpandMedia(field.FieldType, cfv, mediaUrls);
                    continue;
                }

                dto.Fields[key] = fieldValues is not null && fieldValues.TryGetValue(field.Id, out var v)
                    ? TypedValue(v, field.FieldType)
                    : null;
            }

            dto.Meta = new ApiItemMetaDto
            {
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Status = item.Status,
                Language = lang
            };
            return dto;
        }).ToList();
    }

    // Resolves the sort to either a built-in item column or an EAV field (by slug, among visible fields).
    private static (string? ItemColumn, int? FieldId, string? FieldColumn, bool Desc) ResolveSort(
        string? sort, bool allowSorting, IReadOnlyList<(ContentField Field, string? Alias)> visibleFields)
    {
        if (!allowSorting || string.IsNullOrWhiteSpace(sort)) return ("created_at", null, null, true);

        var parts = sort.Split(':', 2);
        string key = parts[0].Trim();
        bool desc = parts.Length > 1 && parts[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

        if (SortableItemColumns.TryGetValue(key, out string? col)) return (col, null, null, desc);

        var field = visibleFields.FirstOrDefault(f => string.Equals(ExternalKey(f.Field.Slug, f.Alias), key, StringComparison.OrdinalIgnoreCase));
        if (field.Field is not null)
            return (null, field.Field.Id, FieldColumn(field.Field.FieldType), desc);

        return ("created_at", null, null, true);   // unknown sort key → default
    }

    // Builds equality filters from filter[slug]=value against visible fields. Unknown fields and
    // unparseable values are skipped. 'status' is reserved (the API always returns published items).
    private static IReadOnlyList<ApiItemFilter> ResolveFilters(
        Dictionary<string, string>? filters, bool allowFiltering,
        IReadOnlyList<(ContentField Field, string? Alias)> visibleFields)
    {
        if (!allowFiltering || filters is null || filters.Count == 0) return Array.Empty<ApiItemFilter>();

        var result = new List<ApiItemFilter>();
        foreach (var (slug, raw) in filters)
        {
            if (string.Equals(slug, "status", StringComparison.OrdinalIgnoreCase)) continue;

            var match = visibleFields.FirstOrDefault(f => string.Equals(ExternalKey(f.Field.Slug, f.Alias), slug, StringComparison.OrdinalIgnoreCase));
            if (match.Field is null) continue;

            var (value, ok) = ParseFilterValue(match.Field.FieldType, raw);
            if (ok) result.Add(new ApiItemFilter(match.Field.Id, FieldColumn(match.Field.FieldType), value));
        }
        return result;
    }

    // Returns the camelCase public-facing key for a field.
    // The alias (if set) is also camelCase-normalized so the contract is consistent.
    private static string ExternalKey(string slug, string? alias)
        => Slugifier.ToCamelCase(alias ?? slug);

    private static string FieldColumn(FieldType type) => type switch
    {
        FieldType.Number or FieldType.Decimal or FieldType.Rating => "value_number",
        FieldType.Boolean => "value_bool",
        FieldType.Date or FieldType.DateTime or FieldType.Time or FieldType.DateRange => "value_date",
        FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File => "value_media_id",
        _ => "value_text"
    };

    private static (object? Value, bool Ok) ParseFilterValue(FieldType type, string raw)
    {
        switch (type)
        {
            case FieldType.Number or FieldType.Decimal or FieldType.Rating:
                return decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var num) ? (num, true) : (null, false);
            case FieldType.Boolean:
                if (raw is "true" or "1" or "on") return (true, true);
                if (raw is "false" or "0" or "off") return (false, true);
                return (null, false);
            case FieldType.Date or FieldType.DateTime or FieldType.Time or FieldType.DateRange:
                return DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt) ? (dt, true) : (null, false);
            case FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File:
                return int.TryParse(raw, out var mediaId) ? (mediaId, true) : (null, false);
            default:
                return string.IsNullOrEmpty(raw) ? (null, false) : (raw, true);
        }
    }

    private static HashSet<string>? ParseProjection(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields)) return null;
        var set = fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return set.Count == 0 ? null : set;
    }

    private static bool IsRelation(FieldType t) => t is FieldType.Relation or FieldType.MultiRelation;

    private static bool IsMedia(FieldType t) =>
        t is FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File or FieldType.Gallery;

    private async Task<IReadOnlyDictionary<int, string>> LoadMediaUrlsAsync(
        int tenantId, IReadOnlyList<ContentItem> items,
        Dictionary<int, Dictionary<int, ContentFieldValue>> valuesByItem,
        IReadOnlyList<(ContentField Field, string? Alias)> visibleFields, CancellationToken ct)
    {
        var mediaFields = visibleFields.Where(f => IsMedia(f.Field.FieldType)).ToList();
        if (mediaFields.Count == 0) return new Dictionary<int, string>();

        var ids = new HashSet<int>();
        foreach (var item in items)
        {
            if (!valuesByItem.TryGetValue(item.Id, out var fv)) continue;
            foreach (var (field, _) in mediaFields)
            {
                if (!fv.TryGetValue(field.Id, out var v)) continue;
                if (field.FieldType == FieldType.Gallery)
                    foreach (var gid in ParseGalleryIds(v.ValueText)) ids.Add(gid);
                else if (v.ValueMediaId is int mid) ids.Add(mid);
            }
        }
        return ids.Count == 0 ? new Dictionary<int, string>() : await _repo.GetMediaUrlsAsync(tenantId, ids.ToList(), ct);
    }

    private static object? ExpandMedia(FieldType type, ContentFieldValue? value, IReadOnlyDictionary<int, string> urls)
    {
        object MapOne(int id) => new { id, url = urls.TryGetValue(id, out var u) ? u : null };

        if (type == FieldType.Gallery)
        {
            var ids = value is null ? new List<int>() : ParseGalleryIds(value.ValueText);
            return ids.Select(MapOne).ToList();
        }
        return value?.ValueMediaId is int mid ? MapOne(mid) : null;
    }

    private static List<int> ParseGalleryIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return new();
            return doc.RootElement.EnumerateArray()
                .Where(e => e.TryGetInt32(out _)).Select(e => e.GetInt32()).Where(i => i > 0).ToList();
        }
        catch (System.Text.Json.JsonException) { return new(); }
    }

    private async Task<(Dictionary<(int Item, int Field), List<int>> Targets,
                       Dictionary<int, IReadOnlyDictionary<int, string>> Displays)> LoadRelationsAsync(
        int tenantId, IReadOnlyList<int> itemIds,
        IReadOnlyList<(ContentField Field, string? Alias)> visibleFields, string lang, CancellationToken ct)
    {
        var targets = new Dictionary<(int, int), List<int>>();
        var displays = new Dictionary<int, IReadOnlyDictionary<int, string>>();

        var relationFields = visibleFields.Where(f => IsRelation(f.Field.FieldType)).ToList();
        if (relationFields.Count == 0) return (targets, displays);

        foreach (var r in await _repo.GetRelationsAsync(tenantId, itemIds, ct))
        {
            var key = (r.SourceItemId, r.SourceFieldId);
            if (!targets.TryGetValue(key, out var list)) targets[key] = list = new List<int>();
            list.Add(r.TargetItemId);
        }

        foreach (var (field, _) in relationFields)
        {
            var options = RelationOptions.Parse(field.OptionsJson);
            if (options is null) continue;
            var ids = targets.Where(kv => kv.Key.Item2 == field.Id).SelectMany(kv => kv.Value).Distinct().ToList();
            displays[field.Id] = await _repo.GetDisplayValuesAsync(
                tenantId, options.TargetContentTypeId, options.DisplayFieldSlug, ids, lang, ct);
        }

        return (targets, displays);
    }

    private static object? ExpandRelation(
        int itemId, ContentField field,
        Dictionary<(int Item, int Field), List<int>> targetsByItemField,
        Dictionary<int, IReadOnlyDictionary<int, string>> displaysByField)
    {
        targetsByItemField.TryGetValue((itemId, field.Id), out var targets);
        targets ??= new List<int>();
        displaysByField.TryGetValue(field.Id, out var displays);

        object MapOne(int id) => new
        {
            id,
            displayValue = displays is not null && displays.TryGetValue(id, out var d) ? d : $"#{id}"
        };

        if (field.FieldType == FieldType.Relation)
            return targets.Count == 0 ? null : MapOne(targets[0]);
        return targets.Select(MapOne).ToList();
    }

    private static object? TypedValue(ContentFieldValue v, FieldType type) => type switch
    {
        FieldType.Number or FieldType.Decimal or FieldType.Rating => v.ValueNumber,
        FieldType.Boolean => v.ValueBool,
        FieldType.Date or FieldType.DateTime or FieldType.Time => v.ValueDate,
        FieldType.DateRange => new { start = v.ValueDate, end = v.ValueDateEnd },
        FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File => v.ValueMediaId,
        _ => v.ValueText
    };

    // =========== Write path ===========

    public async Task<Result<ApiItemDto>> CreateAsync(
        string tenantSlug, string contentTypeSlug, ApiWriteRequest body, ApiCredentials credentials, CancellationToken ct = default)
    {
        var ctx = await ResolveForWriteAsync(tenantSlug, contentTypeSlug, credentials, "create", ct);
        if (ctx.Error is not null) return Result<ApiItemDto>.Failure(ctx.Error);
        var (tenant, config, _, credentialIdentity) = (ctx.Tenant!, ctx.Config!, ctx.Error, ctx.CredentialIdentity);

        string lang = string.IsNullOrWhiteSpace(body.Lang) ? tenant.DefaultLanguageCode : body.Lang.Trim();
        string status = ResolveStatus(body.Status);

        var fields = await _writeRepo.GetContentTypeFieldsAsync(tenant.Id, config.ContentTypeId, ct);
        var visibleFieldsList = await _repo.GetVisibleFieldsAsync(config.Id, ct);

        // Build external-key maps (camelCase): request body keys must match these.
        var visibleExternalKeys = visibleFieldsList
            .Select(f => ExternalKey(f.Field.Slug, f.Alias)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fieldByExternalKey = visibleFieldsList
            .ToDictionary(f => ExternalKey(f.Field.Slug, f.Alias), f => f.Field, StringComparer.OrdinalIgnoreCase);

        var rawValues = MapJsonFields(body.Fields, fieldByExternalKey, visibleExternalKeys);
        var relationValues = MapRelationFields(body.Relations, fieldByExternalKey, visibleExternalKeys);

        // Required field validation.
        var validationError = ValidateRequired(fields, rawValues, relationValues, visibleFieldsList);
        if (validationError is not null) return Result<ApiItemDto>.Failure(validationError);

        // Generate slug.
        string? slug = await BuildUniqueSlugAsync(tenant.Id, config.ContentTypeId, body.Slug, body.Title, null, ct);

        int itemId = await _writeRepo.InsertItemAsync(tenant.Id, config.ContentTypeId, slug, status, ct);

        // Save EAV values.
        var valueList = BuildValueList(rawValues, fields, lang);
        await _writeRepo.SaveValuesAsync(tenant.Id, itemId, valueList, ct);

        // Save relations.
        foreach (var (fieldId, targetIds) in relationValues)
            await _writeRepo.ReplaceRelationsAsync(tenant.Id, itemId, fieldId, targetIds, ct);

        // Save title.
        if (!string.IsNullOrWhiteSpace(body.Title))
            await _writeRepo.SaveTitleAsync(tenant.Id, itemId, lang, body.Title, isActive: status == PublishedStatus, ct);

        // Invalidate cache for this content type.
        InvalidateCache(tenant.Id, config.ContentTypeId);

        await _audit.LogAsync(AuditActions.ContentItemCreated, "ContentItem", itemId,
            contentTypeId: config.ContentTypeId, entityName: body.Title ?? slug,
            tenantIdOverride: tenant.Id, userEmailOverride: credentialIdentity, ct: ct);

        // Return the newly created item (without language gate — item may not be activated yet).
        var newItem = await _writeRepo.GetItemByIdAsync(tenant.Id, config.ContentTypeId, itemId, ct);
        if (newItem is null) return Result<ApiItemDto>.Failure("NotFound");
        var visibleFields = await _repo.GetVisibleFieldsAsync(config.Id, ct);
        var dto = await BuildItemsAsync(tenant.Id, new[] { newItem }, lang, fields: null, visibleFields, ct);
        return Result<ApiItemDto>.Success(dto[0]);
    }

    public async Task<Result<ApiItemDto>> UpdateAsync(
        string tenantSlug, string contentTypeSlug, int id, ApiWriteRequest body, ApiCredentials credentials, CancellationToken ct = default)
    {
        var ctx = await ResolveForWriteAsync(tenantSlug, contentTypeSlug, credentials, "update", ct);
        if (ctx.Error is not null) return Result<ApiItemDto>.Failure(ctx.Error);
        var (tenant, config, _, credentialIdentity) = (ctx.Tenant!, ctx.Config!, ctx.Error, ctx.CredentialIdentity);

        var existing = await _writeRepo.GetItemByIdAsync(tenant.Id, config.ContentTypeId, id, ct);
        if (existing is null) return Result<ApiItemDto>.Failure("NotFound");

        string lang = string.IsNullOrWhiteSpace(body.Lang) ? tenant.DefaultLanguageCode : body.Lang.Trim();
        string status = ResolveStatus(body.Status, existing.Status);

        var fields = await _writeRepo.GetContentTypeFieldsAsync(tenant.Id, config.ContentTypeId, ct);
        var visibleFieldsList = await _repo.GetVisibleFieldsAsync(config.Id, ct);

        var visibleExternalKeys = visibleFieldsList
            .Select(f => ExternalKey(f.Field.Slug, f.Alias)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fieldByExternalKey = visibleFieldsList
            .ToDictionary(f => ExternalKey(f.Field.Slug, f.Alias), f => f.Field, StringComparer.OrdinalIgnoreCase);

        var rawValues = MapJsonFields(body.Fields, fieldByExternalKey, visibleExternalKeys);
        var relationValues = MapRelationFields(body.Relations, fieldByExternalKey, visibleExternalKeys);

        string? slug = body.Slug is not null
            ? await BuildUniqueSlugAsync(tenant.Id, config.ContentTypeId, body.Slug, body.Title, id, ct)
            : existing.Slug;

        await _writeRepo.UpdateItemAsync(tenant.Id, id, slug, status, ct);

        var valueList = BuildValueList(rawValues, fields, lang);
        await _writeRepo.SaveValuesAsync(tenant.Id, id, valueList, ct);

        foreach (var (fieldId, targetIds) in relationValues)
            await _writeRepo.ReplaceRelationsAsync(tenant.Id, id, fieldId, targetIds, ct);

        if (body.Title is not null)
            await _writeRepo.SaveTitleAsync(tenant.Id, id, lang, body.Title, isActive: status == PublishedStatus, ct);

        InvalidateCache(tenant.Id, config.ContentTypeId);

        await _audit.LogAsync(AuditActions.ContentItemUpdated, "ContentItem", id,
            contentTypeId: config.ContentTypeId, entityName: body.Title ?? slug,
            tenantIdOverride: tenant.Id, userEmailOverride: credentialIdentity, ct: ct);

        var updated = await _writeRepo.GetItemByIdAsync(tenant.Id, config.ContentTypeId, id, ct);
        if (updated is null) return Result<ApiItemDto>.Failure("NotFound");
        var visibleFields = await _repo.GetVisibleFieldsAsync(config.Id, ct);
        var dto = await BuildItemsAsync(tenant.Id, new[] { updated }, lang, fields: null, visibleFields, ct);
        return Result<ApiItemDto>.Success(dto[0]);
    }

    public async Task<Result> DeleteAsync(
        string tenantSlug, string contentTypeSlug, int id, ApiCredentials credentials, CancellationToken ct = default)
    {
        var ctx = await ResolveForWriteAsync(tenantSlug, contentTypeSlug, credentials, "delete", ct);
        if (ctx.Error is not null) return Result.Failure(ctx.Error);
        var (tenant, config, _, credentialIdentity) = (ctx.Tenant!, ctx.Config!, ctx.Error, ctx.CredentialIdentity);

        bool deleted = await _writeRepo.SoftDeleteItemAsync(tenant.Id, id, ct);
        if (!deleted) return Result.Failure("NotFound");

        InvalidateCache(tenant.Id, config.ContentTypeId);

        await _audit.LogAsync(AuditActions.ContentItemDeleted, "ContentItem", id,
            contentTypeId: config.ContentTypeId,
            tenantIdOverride: tenant.Id, userEmailOverride: credentialIdentity, ct: ct);

        return Result.Success();
    }

    // Resolves + auth for write operations: credentials are ALWAYS required (no is_public bypass).
    // Returns credential identity alongside tenant/config so callers can use it in audit entries.
    private async Task<(TenantInfo? Tenant, ApiConfiguration? Config, string? Error, string CredentialIdentity)> ResolveForWriteAsync(
        string tenantSlug, string contentTypeSlug, ApiCredentials credentials, string verb, CancellationToken ct)
    {
        var tenant = await _tenantStore.FindBySlugAsync(tenantSlug, ct);
        if (tenant is null) return (null, null, "NotFound", "api:unknown");

        var config = await _repo.GetEnabledConfigAsync(tenant.Id, contentTypeSlug, ct);
        if (config is null) return (null, null, "NotFound", "api:unknown");

        var (valid, identity) = await ResolveCredentialAsync(config, tenant.Id, tenantSlug, contentTypeSlug, credentials, allowPublic: false, ct);
        if (!valid)
        {
            await _audit.LogAsync(AuditActions.ApiAuthFailed, "ApiCredential", config.ContentTypeId,
                contentTypeId: config.ContentTypeId, entityName: contentTypeSlug,
                tenantIdOverride: tenant.Id, userEmailOverride: identity, ct: ct);
            return (null, null, "Unauthorized", identity);
        }

        bool permitted = verb switch
        {
            "create" => config.AllowCreate,
            "update" => config.AllowUpdate,
            "delete" => config.AllowDelete,
            _ => false
        };
        if (!permitted)
        {
            await _audit.LogAsync(AuditActions.ApiVerbForbidden, "ApiCredential", config.ContentTypeId,
                contentTypeId: config.ContentTypeId, entityName: $"{verb}:{contentTypeSlug}",
                tenantIdOverride: tenant.Id, userEmailOverride: identity, ct: ct);
            return (null, null, "Forbidden", identity);
        }

        return (tenant, config, null, identity);
    }

    // Maps JSON field values (keyed by camelCase external key) to raw strings for FieldValueMapper.Apply.
    private static Dictionary<int, string?> MapJsonFields(
        Dictionary<string, JsonElement>? fields,
        Dictionary<string, ContentField> fieldByExternalKey,
        HashSet<string> visibleExternalKeys)
    {
        var result = new Dictionary<int, string?>();
        if (fields is null) return result;
        foreach (var (key, element) in fields)
        {
            if (!visibleExternalKeys.Contains(key)) continue;
            if (!fieldByExternalKey.TryGetValue(key, out var field)) continue;
            if (IsRelation(field.FieldType)) continue; // handled separately
            result[field.Id] = JsonToRaw(field.FieldType, element);
        }
        return result;
    }

    // Maps relation field camelCase external key → target ids from the request, respecting visibility.
    private static Dictionary<int, List<int>> MapRelationFields(
        Dictionary<string, List<int>>? relations,
        Dictionary<string, ContentField> fieldByExternalKey,
        HashSet<string> visibleExternalKeys)
    {
        var result = new Dictionary<int, List<int>>();
        if (relations is null) return result;
        foreach (var (key, ids) in relations)
        {
            if (!visibleExternalKeys.Contains(key)) continue;
            if (!fieldByExternalKey.TryGetValue(key, out var field)) continue;
            if (!IsRelation(field.FieldType)) continue;
            result[field.Id] = ids.Where(i => i > 0).Distinct().ToList();
        }
        return result;
    }

    // Validates required fields; returns an error string (with camelCase key) or null on success.
    private static string? ValidateRequired(
        IReadOnlyList<ContentField> fields,
        Dictionary<int, string?> rawValues,
        Dictionary<int, List<int>> relationValues,
        IReadOnlyList<(ContentField Field, string? Alias)> visibleFieldsList)
    {
        var visibleById = visibleFieldsList.ToDictionary(f => f.Field.Id, f => f);

        foreach (var f in fields)
        {
            if (!f.IsRequired) continue;
            if (!visibleById.ContainsKey(f.Id)) continue; // hidden required fields are skipped

            string extKey = visibleById.TryGetValue(f.Id, out var vf)
                ? ExternalKey(vf.Field.Slug, vf.Alias)
                : f.Slug;

            if (IsRelation(f.FieldType))
            {
                if (!relationValues.TryGetValue(f.Id, out var ids) || ids.Count == 0)
                    return $"Validation: field '{extKey}' is required";
            }
            else
            {
                if (!rawValues.TryGetValue(f.Id, out var raw) || string.IsNullOrWhiteSpace(raw))
                    return $"Validation: field '{extKey}' is required";
            }
        }
        return null;
    }

    // Builds the ContentFieldValue list, applying localization rules and FieldValueMapper.
    private static List<ContentFieldValue> BuildValueList(
        Dictionary<int, string?> rawValues,
        IReadOnlyList<ContentField> fields,
        string lang)
    {
        var fieldMap = fields.ToDictionary(f => f.Id);
        var result = new List<ContentFieldValue>();
        foreach (var (fieldId, raw) in rawValues)
        {
            if (!fieldMap.TryGetValue(fieldId, out var field)) continue;
            var cfv = new ContentFieldValue
            {
                ContentFieldId = fieldId,
                LanguageCode = field.IsLocalized ? lang : "all"
            };
            FieldValueMapper.Apply(cfv, field.FieldType, raw);
            result.Add(cfv);
        }
        return result;
    }

    // Converts a JsonElement to the raw string FieldValueMapper.Apply expects.
    private static string? JsonToRaw(FieldType type, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            return null;

        return type switch
        {
            FieldType.Number or FieldType.Decimal or FieldType.Rating =>
                element.ValueKind == JsonValueKind.Number
                    ? element.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : element.ToString(),
            FieldType.Boolean =>
                element.ValueKind == JsonValueKind.True ? "true" :
                element.ValueKind == JsonValueKind.False ? "false" : element.ToString(),
            FieldType.Date or FieldType.DateTime or FieldType.Time => element.ToString(),
            FieldType.DateRange =>
                // Expect either "start|end" string or { start, end } object.
                element.ValueKind == JsonValueKind.Object && element.TryGetProperty("start", out var s) && element.TryGetProperty("end", out var e)
                    ? $"{s}|{e}"
                    : element.ToString(),
            FieldType.Image or FieldType.Video or FieldType.Audio or FieldType.File =>
                element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString() : element.ToString(),
            _ => element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString()
        };
    }

    private static string ResolveStatus(string? requested, string? existing = null)
    {
        var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "draft", "published", "archived" };
        if (!string.IsNullOrWhiteSpace(requested) && valid.Contains(requested.Trim()))
            return requested.Trim().ToLowerInvariant();
        return existing ?? "draft";
    }

    private async Task<string?> BuildUniqueSlugAsync(
        int tenantId, int contentTypeId, string? requestedSlug, string? title, int? excludeId, CancellationToken ct)
    {
        string? baseSlug = !string.IsNullOrWhiteSpace(requestedSlug)
            ? Slugifier.ToSlug(requestedSlug)
            : !string.IsNullOrWhiteSpace(title) ? Slugifier.ToSlug(title) : null;

        if (string.IsNullOrWhiteSpace(baseSlug)) return null;

        string candidate = baseSlug;
        int suffix = 2;
        while (await _writeRepo.SlugExistsAsync(tenantId, contentTypeId, candidate, excludeId, ct))
            candidate = $"{baseSlug}-{suffix++}";
        return candidate;
    }

    private void InvalidateCache(int tenantId, int contentTypeId)
    {
        // Best-effort: removes all cached responses for this content type (list + single items).
        _cache.RemoveByPrefix($"api:{tenantId}:{contentTypeId}:");
    }
}
