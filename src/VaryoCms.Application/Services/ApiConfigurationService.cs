using System.Text.Json;
using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

// Manages per-content-type API config (is_enabled, is_public, allow_* flags, rate limit,
// cache seconds, field visibility). Credential management has moved to ApiCredentialService.
public class ApiConfigurationService : IApiConfigurationService
{
    private readonly IApiConfigurationRepository _apiConfig;
    private readonly IContentTypeRepository _contentTypes;
    private readonly IContentFieldRepository _contentFields;
    private readonly IStringLocalizer<SharedResource> _t;

    public ApiConfigurationService(
        IApiConfigurationRepository apiConfig,
        IContentTypeRepository contentTypes,
        IContentFieldRepository contentFields,
        IStringLocalizer<SharedResource> t)
    {
        _apiConfig = apiConfig;
        _contentTypes = contentTypes;
        _contentFields = contentFields;
        _t = t;
    }

    public async Task<Result<IReadOnlyList<ApiConfigListItemDto>>> GetListAsync(CancellationToken ct = default)
    {
        var contentTypes = await _contentTypes.GetAllAsync(ct);
        var configs = (await _apiConfig.GetAllAsync(ct)).ToDictionary(c => c.ContentTypeId);

        IReadOnlyList<ApiConfigListItemDto> list = contentTypes.Select(ct2 =>
        {
            configs.TryGetValue(ct2.Id, out var c);
            return new ApiConfigListItemDto
            {
                ContentTypeId = ct2.Id,
                ContentTypeName = ct2.Name,
                ContentTypeSlug = ct2.Slug,
                IsConfigured = c is not null,
                IsEnabled = c?.IsEnabled ?? false,
                IsPublic = c?.IsPublic ?? false,
                AllowWrite = c is not null && (c.AllowCreate || c.AllowUpdate || c.AllowDelete)
            };
        }).ToList();

        return Result<IReadOnlyList<ApiConfigListItemDto>>.Success(list);
    }

    public async Task<Result<ApiConfigEditDto>> GetForEditAsync(int contentTypeId, CancellationToken ct = default)
    {
        var contentType = await _contentTypes.GetByIdAsync(contentTypeId, ct);
        if (contentType is null) return Result<ApiConfigEditDto>.Failure(_t["Err.ContentTypeNotFound"]);

        var config = await _apiConfig.GetByContentTypeIdAsync(contentTypeId, ct);
        var fields = await _contentFields.GetByContentTypeAsync(contentTypeId, ct);
        var visibility = config is null
            ? new List<ApiFieldVisibility>()
            : (await _apiConfig.GetFieldVisibilityAsync(config.Id, ct)).ToList();
        var visByField = visibility.ToDictionary(v => v.ContentFieldId);

        IReadOnlyList<ApiFieldVisibilityDto> fieldDtos = fields.Select(f =>
        {
            visByField.TryGetValue(f.Id, out var v);
            return new ApiFieldVisibilityDto
            {
                ContentFieldId = f.Id,
                FieldName = f.Name,
                FieldSlug = f.Slug,
                IsVisible = v?.IsVisible ?? true,   // unconfigured fields default to visible
                ResponseKeyAlias = v?.ResponseKeyAlias
            };
        }).ToList();

        var (requestJson, responseJson) = BuildModelPreview(contentType.Slug, fieldDtos);

        return Result<ApiConfigEditDto>.Success(new ApiConfigEditDto
        {
            ContentTypeId = contentType.Id,
            ContentTypeName = contentType.Name,
            ContentTypeSlug = contentType.Slug,
            IsEnabled = config?.IsEnabled ?? false,
            IsPublic = config?.IsPublic ?? false,
            AllowFiltering = config?.AllowFiltering ?? true,
            AllowSorting = config?.AllowSorting ?? true,
            AllowPagination = config?.AllowPagination ?? true,
            RateLimitPerMin = config?.RateLimitPerMin ?? 60,
            CacheSeconds = config?.CacheSeconds ?? 0,
            AllowRead = config?.AllowRead ?? true,
            AllowCreate = config?.AllowCreate ?? false,
            AllowUpdate = config?.AllowUpdate ?? false,
            AllowDelete = config?.AllowDelete ?? false,
            Fields = fieldDtos,
            RequestExampleJson = requestJson,
            ResponseExampleJson = responseJson
        });
    }

    public async Task<Result> SaveAsync(SaveApiConfigRequest request, CancellationToken ct = default)
    {
        var contentType = await _contentTypes.GetByIdAsync(request.ContentTypeId, ct);
        if (contentType is null) return Result.Failure(_t["Err.ContentTypeNotFound"]);

        if (request.RateLimitPerMin is < 1 or > 100000)
            return Result.Failure(_t["Err.RateLimitRange"]);
        if (request.CacheSeconds < 0)
            return Result.Failure(_t["Err.CacheNegative"]);

        int configId = await _apiConfig.UpsertAsync(new ApiConfiguration
        {
            ContentTypeId = request.ContentTypeId,
            IsEnabled = request.IsEnabled,
            IsPublic = request.IsPublic,
            AllowFiltering = request.AllowFiltering,
            AllowSorting = request.AllowSorting,
            AllowPagination = request.AllowPagination,
            RateLimitPerMin = request.RateLimitPerMin,
            CacheSeconds = request.CacheSeconds,
            AllowRead = request.AllowRead,
            AllowCreate = request.AllowCreate,
            AllowUpdate = request.AllowUpdate,
            AllowDelete = request.AllowDelete
        }, ct);

        IReadOnlyList<ApiFieldVisibility> rows = request.Fields.Select(f => new ApiFieldVisibility
        {
            ContentFieldId = f.ContentFieldId,
            IsVisible = f.IsVisible,
            ResponseKeyAlias = string.IsNullOrWhiteSpace(f.ResponseKeyAlias) ? null : f.ResponseKeyAlias.Trim()
        }).ToList();
        await _apiConfig.ReplaceFieldVisibilityAsync(configId, rows, ct);

        return Result.Success();
    }

    // Builds request/response example JSON strings from the visible field list.
    private static (string Request, string Response) BuildModelPreview(
        string contentTypeSlug, IReadOnlyList<ApiFieldVisibilityDto> fields)
    {
        var visibleFields = fields.Where(f => f.IsVisible).ToList();

        // Request body example (POST / PUT)
        var reqFields = new Dictionary<string, object?>();
        var reqRelations = new Dictionary<string, object?>();
        foreach (var f in visibleFields)
        {
            string rawKey = string.IsNullOrWhiteSpace(f.FieldSlug) ? $"field_{f.ContentFieldId}" : f.FieldSlug;
            string key = Slugifier.ToCamelCase(rawKey);
            if (IsRelationType(f))
                reqRelations[key] = new[] { 1 };
            else
                reqFields[key] = SampleValue(f.FieldSlug ?? string.Empty);
        }

        var reqObj = new Dictionary<string, object?>
        {
            ["status"] = "draft",
            ["slug"] = $"example-{contentTypeSlug}",
            ["title"] = "Example Title",
            ["fields"] = reqFields
        };
        if (reqRelations.Count > 0) reqObj["relations"] = reqRelations;

        // Response body example (GET) — keys are always camelCase (alias also normalized)
        var resFields = new Dictionary<string, object?>();
        foreach (var f in visibleFields)
        {
            string rawKey = string.IsNullOrWhiteSpace(f.ResponseKeyAlias)
                ? (f.FieldSlug ?? $"field_{f.ContentFieldId}")
                : f.ResponseKeyAlias;
            string key = Slugifier.ToCamelCase(rawKey);
            if (IsRelationType(f))
                resFields[key] = new { id = 1, displayValue = "Example Item" };
            else if (IsMediaType(f.FieldSlug ?? string.Empty))
                resFields[key] = new { id = 1, url = "/uploads/1/example.jpg" };
            else
                resFields[key] = SampleValue(f.FieldSlug ?? string.Empty);
        }

        var resObj = new
        {
            id = 1,
            slug = $"example-{contentTypeSlug}",
            fields = resFields,
            meta = new { createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z", status = "published", language = "tr" }
        };

        var opts = new JsonSerializerOptions { WriteIndented = true };
        return (JsonSerializer.Serialize(reqObj, opts), JsonSerializer.Serialize(resObj, opts));
    }

    private static bool IsRelationType(ApiFieldVisibilityDto f)
    {
        // We only have the slug available — detect by common naming conventions.
        // The field name from the db is available; relation type hints are checked in service when we have ContentField.
        // Here we use a safe heuristic based on field name suffix.
        return f.FieldName is not null && (
            f.FieldName.Contains("Relation", StringComparison.OrdinalIgnoreCase) ||
            f.FieldSlug is not null && (f.FieldSlug.EndsWith("_id", StringComparison.OrdinalIgnoreCase) ||
                                        f.FieldSlug.EndsWith("_ids", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsMediaType(string slug) =>
        slug is "image" or "video" or "audio" or "file" or "cover" or "thumbnail" or "photo" or "attachment";

    private static object? SampleValue(string slug) => slug switch
    {
        var s when s.Contains("date", StringComparison.OrdinalIgnoreCase) => "2026-01-01",
        var s when s.Contains("price", StringComparison.OrdinalIgnoreCase)
                || s.Contains("count", StringComparison.OrdinalIgnoreCase)
                || s.Contains("views", StringComparison.OrdinalIgnoreCase) => 0,
        var s when s.Contains("active", StringComparison.OrdinalIgnoreCase)
                || s.Contains("enabled", StringComparison.OrdinalIgnoreCase)
                || s.Contains("featured", StringComparison.OrdinalIgnoreCase) => false,
        _ => "example text"
    };
}
