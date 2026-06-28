---
name: dotnet-api-endpoint
description: >
  Use when building or modifying the public API layer: exposing content types as endpoints,
  field visibility, API authentication (ApiKey/JWT), rate limiting, or the API management
  admin UI. Triggers on: "public api", "api endpoint", "api yönetimi", "api management",
  "expose content type", "api key", "rate limit", "field visibility", "api response".
---

# Varyo CMS Public API System

## Reference Files
- API design rules: @docs/api-design.md
- DB schema: @docs/database-schema.md (api_configurations, api_field_visibility)
- Architecture: @docs/architecture.md

## Public API Controller

```csharp
// VaryoCms.Web/Controllers/Api/PublicApiController.cs
[ApiController]
[Route("api/v1/{tenantSlug}/{contentTypeSlug}")]
public class PublicApiController(IPublicApiService apiService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        string tenantSlug, string contentTypeSlug,
        [FromQuery] ApiListRequest request)
    {
        var result = await apiService.GetListAsync(tenantSlug, contentTypeSlug, request);
        if (!result.IsSuccess) return result.Error == "NotFound" ? NotFound() : Unauthorized();
        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(string tenantSlug, string contentTypeSlug, int id,
        [FromQuery] string? lang = null)
    {
        var result = await apiService.GetByIdAsync(tenantSlug, contentTypeSlug, id, lang);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }
}
```

## ApiListRequest DTO
```csharp
public record ApiListRequest(
    string? Lang = null,
    int Page = 1,
    int PageSize = 20,
    [FromQuery(Name = "filter")] Dictionary<string, string>? Filters = null,
    string? Sort = null,         // "created_at:desc"
    string? Fields = null        // "title,slug,image" — projection
);
```

## PublicApiService — Core Logic

```csharp
public async Task<Result<ApiListResponse>> GetListAsync(
    string tenantSlug, string contentTypeSlug, ApiListRequest request)
{
    // 1. Resolve tenant
    var tenant = await _tenantRepo.GetBySlugAsync(tenantSlug);
    if (tenant is null) return Result<ApiListResponse>.Failure("NotFound");

    // 2. Get api configuration
    var config = await _apiConfigRepo.GetByContentTypeSlugAsync(
        tenant.Id, contentTypeSlug);
    if (config is null || !config.IsEnabled)
        return Result<ApiListResponse>.Failure("NotFound");

    // 3. Check auth
    var authResult = await CheckAuthAsync(config, HttpContext.Request);
    if (!authResult) return Result<ApiListResponse>.Failure("Unauthorized");

    // 4. Get visible fields
    var visibleFields = await _apiConfigRepo.GetVisibleFieldsAsync(config.Id);

    // 5. Query content items with field values
    var lang = request.Lang ?? tenant.DefaultLanguageCode;
    var items = await _contentItemRepo.GetForApiAsync(new ApiQueryParams(
        tenant.Id, config.ContentTypeId, lang,
        request.Page, request.PageSize,
        request.Filters, request.Sort,
        config.AllowFiltering, config.AllowSorting, config.AllowPagination));

    // 6. Apply field visibility + aliases + projection
    var response = MapToApiResponse(items, visibleFields, request.Fields);
    return Result<ApiListResponse>.Success(response);
}
```

## Auth Check Logic

```csharp
private async Task<bool> CheckAuthAsync(ApiConfiguration config, HttpRequest request)
{
    return config.AuthType switch
    {
        "None" => true,
        "ApiKey" => request.Headers.TryGetValue("X-API-Key", out var key)
                    && BCrypt.Verify(key!, config.ApiKeyHash),
        "JWT"   => await ValidateJwtAsync(request, config.JwtSecret),
        _       => false
    };
}
```

## Rate Limit Middleware

```csharp
// VaryoCms.Web/Middleware/ApiRateLimitMiddleware.cs
public class ApiRateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
{
    public async Task InvokeAsync(HttpContext context, IApiConfigRepository apiConfigRepo)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await next(context);
            return;
        }

        // key: "ratelimit:{tenantId}:{contentTypeSlug}:{clientIp}"
        var key = $"ratelimit:{context.Items["TenantId"]}:{GetClientIp(context)}";
        var count = cache.GetOrCreate(key, e => { e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1); return 0; });

        var config = (ApiConfiguration?)context.Items["ApiConfig"];
        var limit = config?.RateLimitPerMin ?? 60;

        if (count >= limit)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = "60";
            return;
        }

        cache.Set(key, count + 1);
        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (limit - count - 1).ToString();
        await next(context);
    }
}
```

## Field Visibility Mapping

```csharp
private object MapItemToApiObject(
    ContentItemWithValues item,
    List<ApiFieldVisibilityDto> visibleFields,
    string? fieldProjection)
{
    var requestedFields = fieldProjection?.Split(',').Select(f => f.Trim()).ToHashSet();

    var fields = new Dictionary<string, object?>();
    foreach (var value in item.FieldValues)
    {
        var visibility = visibleFields.FirstOrDefault(v => v.ContentFieldId == value.FieldId);
        if (visibility is { IsVisible: false }) continue;
        if (requestedFields is not null && !requestedFields.Contains(value.FieldSlug)) continue;

        var key = visibility?.ResponseKeyAlias ?? value.FieldSlug;
        fields[key] = value.TypedValue; // resolved based on field_type
    }

    return new { item.Id, item.Slug, fields, meta = new { item.CreatedAt, item.UpdatedAt, item.Status } };
}
```

## API Management Admin UI

Route: `/admin/api-management`

Key views:
1. `Index.cshtml` — Table of content types with "API" toggle switch per row
2. `Configure.cshtml` — Per content type: auth type, rate limit, cache TTL
3. `_FieldVisibilityTable.cshtml` — Partial: list of fields with visible toggle + alias input
4. `_ApiPreview.cshtml` — Shows sample curl + sample JSON response

Actions:
```csharp
[HttpPost("{contentTypeId}/toggle")]  // enable/disable API exposure
[HttpPost("{contentTypeId}/configure")]  // save auth + rate limit settings
[HttpPost("{contentTypeId}/fields")]  // save field visibility
[HttpPost("{contentTypeId}/rotate-key")]  // generate new API key
```
