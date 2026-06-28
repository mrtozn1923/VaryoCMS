---
name: dotnet-multitenant
description: >
  Use when implementing or debugging multi-tenancy, tenant resolution middleware,
  ITenantContext, per-tenant data isolation, or multi-language (ILanguageContext,
  dictionary lookups, translation table joins). Triggers on: "tenant", "kiracı",
  "multi-tenant", "language", "dil", "çoklu dil", "dictionary", "sözlük",
  "translation", "çeviri", "subdomain", "language switcher".
---

# Varyo CMS Multi-Tenant & Multi-Language Patterns

## Reference Files
- Rules: @docs/multitenancy.md
- DB Schema: @docs/database-schema.md (tenants, languages, dictionary_entries, dictionary_translations)

## TenantResolutionMiddleware

```csharp
// VaryoCms.Web/Middleware/TenantResolutionMiddleware.cs
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantRepository tenantRepo,
                                   ITenantContext tenantContext, IConfiguration config)
    {
        string slug;
        var host = context.Request.Host.Host;

        // Dev mode: use config override
        if (host is "localhost" or "127.0.0.1")
        {
            slug = config["DevTenantSlug"] ?? "dev";
        }
        else
        {
            // Extract subdomain: "acme.cms.yourdomain.com" → "acme"
            slug = host.Split('.')[0];
        }

        var tenant = await tenantRepo.GetBySlugAsync(slug);
        if (tenant is null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        // Set on scoped context
        ((TenantContext)tenantContext).Set(tenant.Id, tenant.Slug, tenant.DefaultLanguageCode);
        await next(context);
    }
}
```

## ITenantContext + TenantContext

```csharp
// Domain/Interfaces/ITenantContext.cs
public interface ITenantContext
{
    int TenantId { get; }
    string TenantSlug { get; }
    string DefaultLanguageCode { get; }
}

// Web/Contexts/TenantContext.cs
public class TenantContext : ITenantContext
{
    public int TenantId { get; private set; }
    public string TenantSlug { get; private set; } = default!;
    public string DefaultLanguageCode { get; private set; } = "tr";

    internal void Set(int tenantId, string slug, string defaultLang)
    {
        TenantId = tenantId;
        TenantSlug = slug;
        DefaultLanguageCode = defaultLang;
    }
}

// Registration in Program.cs
builder.Services.AddScoped<ITenantContext, TenantContext>();
// NOTE: TenantContext is scoped — one per request
```

## LanguageResolutionMiddleware

```csharp
public class LanguageResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILanguageContext langContext,
                                   ITenantContext tenantContext)
    {
        // Priority: query param > cookie > tenant default
        var code = context.Request.Query["lang"].FirstOrDefault()
                   ?? context.Request.Cookies["lang"]
                   ?? tenantContext.DefaultLanguageCode;

        ((LanguageContext)langContext).Set(code);

        // Persist in cookie (1 year)
        if (context.Request.Query.ContainsKey("lang"))
            context.Response.Cookies.Append("lang", code, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        await next(context);
    }
}
```

## Translation Table JOIN Pattern (Dapper)

Use this pattern in every repo that fetches user-visible text:

```csharp
// ContentType with name translation (if content_type has localized name)
const string sql = @"
    SELECT ct.id, ct.slug, ct.sort_order,
           COALESCE(ctt.name, ct.name) AS Name,
           COALESCE(ctt.description, ct.description) AS Description
    FROM content_types ct
    LEFT JOIN content_type_translations ctt
        ON ctt.content_type_id = ct.id AND ctt.language_code = @LangCode
    WHERE ct.tenant_id = @TenantId AND ct.is_deleted = 0
    ORDER BY ct.sort_order ASC";
    
var result = await conn.QueryAsync<ContentTypeDto>(sql, 
    new { TenantId = tenantId, LangCode = langCode });
```

## Dictionary Service

```csharp
public interface IDictionaryService
{
    Task<string> GetAsync(string key, string languageCode, string? fallback = null);
    Task<Dictionary<string, string>> GetAllAsync(string languageCode, string? category = null);
    Task PreloadAsync(string languageCode);  // cache all entries for request
}

// Usage in Views:
// @inject IDictionaryService Dict
// @await Dict.GetAsync("nav.home", Model.CurrentLanguage, "Home")
```

### Dictionary Query
```sql
SELECT de.key_name, dt.value
FROM dictionary_entries de
JOIN dictionary_translations dt ON dt.entry_id = de.id AND dt.language_code = @LangCode
WHERE de.tenant_id = @TenantId AND de.is_deleted = 0
@if category is not null: AND de.category = @Category
```

## Content Field Value Query (with language)

```sql
-- Fetch all values for a ContentItem in a specific language
-- For non-localized fields (language_code = 'all'), return those too
SELECT
    cf.id AS FieldId, cf.slug AS FieldSlug, cf.field_type, cf.options_json,
    cfv.value_text, cfv.value_number, cfv.value_bool,
    cfv.value_date, cfv.value_date_end, cfv.value_media_id
FROM content_fields cf
LEFT JOIN content_field_values cfv
    ON cfv.content_field_id = cf.id
    AND cfv.content_item_id = @ItemId
    AND (cfv.language_code = @LangCode OR cfv.language_code = 'all')
WHERE cf.content_type_id = @ContentTypeId
  AND cf.tenant_id = @TenantId
  AND cf.is_deleted = 0
ORDER BY cf.sort_order ASC
```

## Language Switcher UI (partial view)
```html
<!-- _LanguageSwitcher.cshtml -->
<div class="dropdown">
    <button class="btn btn-sm btn-outline-secondary dropdown-toggle" data-bs-toggle="dropdown">
        <i class="bi bi-globe"></i> @currentLang.Name
    </button>
    <ul class="dropdown-menu">
        @foreach(var lang in availableLanguages) {
            <li>
                <a class="dropdown-item @(lang.Code == currentLang.Code ? "active" : "")"
                   href="?lang=@lang.Code">
                    @lang.Name
                </a>
            </li>
        }
    </ul>
</div>
```

## Registration (Program.cs order)
```csharp
app.UseMiddleware<TenantResolutionMiddleware>();   // FIRST — sets TenantId
app.UseMiddleware<LanguageResolutionMiddleware>(); // SECOND — sets LangCode
app.UseAuthentication();
app.UseAuthorization();
```
