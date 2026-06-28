# Varyo CMS — API Design Reference

## Public API Endpoint Pattern

```
GET    /api/v1/{tenantSlug}/{contentTypeSlug}
GET    /api/v1/{tenantSlug}/{contentTypeSlug}/{id}
GET    /api/v1/{tenantSlug}/{contentTypeSlug}/slug/{slug}
POST   /api/v1/{tenantSlug}/{contentTypeSlug}        — requires allow_create = 1
PUT    /api/v1/{tenantSlug}/{contentTypeSlug}/{id}   — requires allow_update = 1
DELETE /api/v1/{tenantSlug}/{contentTypeSlug}/{id}   — requires allow_delete = 1
```

---

## camelCase Field Key Convention

All field keys in the `fields` dictionary are **camelCase**. This is the single source of truth:

```
ExternalKey(field, alias) = Slugifier.ToCamelCase(alias ?? field.Slug)
```

| Field slug | Alias set in admin | API key in response |
|---|---|---|
| `title` | — | `title` |
| `hero-title` | — | `heroTitle` |
| `published-at` | — | `publishedAt` |
| `seo-meta-description` | — | `seoMetaDescription` |
| `body` | `content` | `content` |
| `read-time` | `reading-time` | `readingTime` |

**Rule:** Enter aliases as kebab-case in the admin panel. The system normalizes to camelCase automatically. Entering `readingTime` directly would produce `readingtime` (wrong).

Same keys apply for:
- **Projection** (`?fields=heroTitle,category`) — camelCase only
- **Filtering** (`?filter[readingTime]=5`) — camelCase only
- **Sorting** (`?sort=publishedAt:desc`) — camelCase + built-in columns (`id`, `createdAt`, `updatedAt`, `publishedAt`, `status`)

---

## Request Parameters

```
GET /api/v1/acme/blog-post?
    lang=tr                    ← language (default: tenant default)
    &page=1                    ← pagination (if allow_pagination=1)
    &pageSize=20
    &filter[featured]=true     ← camelCase field filter (if allow_filtering=1)
    &filter[readingTime]=5
    &sort=publishedAt:desc     ← camelCase sort key (if allow_sorting=1)
    &fields=title,summary,category   ← field projection (camelCase)
```

---

## Authentication

Based on `api_configurations.auth_type`:

| Type | Header | Notes |
|---|---|---|
| `None` | — | Public (`is_public = 1`). Requires allow_read=1 in config. |
| `ApiKey` | `X-API-Key: vk_{credId}_{secret}` | Credential id embedded in key; BCrypt Verify in O(1) |
| `JWT` | `Authorization: Bearer {token}` | CMS-issued HS256 token |

### API Key Format

```
vk_{credentialId}_{plaintext-secret}
```

- `credentialId` = `api_credentials.id` (int)  
- `plaintext-secret` = random string set when credential was created  
- Stored as BCrypt hash in `api_credentials.api_key`  
- On verify: parse id → load hash → `BCrypt.Verify(secret, hash)`

### JWT Model

The CMS issues and validates tokens (not an external IdP).

- Signed HS256 with `Jwt:SigningKey` from `appsettings.json`  
- Claims: `tenant` (slug) + one or more `ct` (content-type slug)  
- Expiry: 365 days (configurable)  
- Validated: signature + issuer + expiry + tenant/ct claim match  
- Stateless (no DB storage)

Generate via admin: `POST /admin/api-management/{contentTypeId}/generate-token` — shown **once**.

---

## Credentials vs Configurations

These are separate concepts:

| | `api_credentials` | `api_configurations` |
|---|---|---|
| What | Auth identity (key/token) | Per-content-type settings |
| Scope | One credential → N content types | One configuration per content type |
| Junction | `api_credential_content_types` | — |

A single API Key can cover multiple content types. Each content type has its own rate limit, cache, field visibility, and verb permissions.

---

## Response Format

```json
{
  "data": [
    {
      "id": 1,
      "slug": "getting-started-with-varyo-cms",
      "fields": {
        "title": "Getting Started with Varyo CMS",
        "summary": "A quick-start guide.",
        "content": "<h2>Introduction</h2><p>...</p>",
        "category": { "id": 1, "displayValue": "Technology" },
        "authors": [
          { "id": 2, "displayValue": "Alice Johnson" },
          { "id": 3, "displayValue": "Bob Martinez" }
        ],
        "publishedAt": "2026-01-10T09:00:00",
        "featured": true,
        "readingTime": 5.0,
        "tags": "[\"cms\",\"tutorial\"]"
      },
      "meta": {
        "createdAt": "2026-01-10T08:00:00Z",
        "updatedAt": "2026-01-10T08:30:00Z",
        "status": "published",
        "language": "tr"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 42,
    "totalPages": 3
  }
}
```

### Field Value Types in Response

| Field type | JSON representation |
|---|---|
| Text, Email, URL, Phone, Color, Slug, Password | `string` |
| RichText | `string` (HTML) |
| Markdown | `string` (Markdown) |
| Number, Rating | `number` (integer) |
| Decimal | `number` (decimal) |
| Boolean | `boolean` |
| Date | `string` (YYYY-MM-DD) |
| DateTime | `string` (ISO 8601) |
| Time | `string` (HH:mm) |
| DateRange | `{ "start": "...", "end": "..." }` |
| Tags | `string` (JSON array: `"[\"a\",\"b\"]"`) |
| Select, MultiSelect | `string` |
| Image, Video, Audio, File | `{ "id": 1, "url": "/uploads/1/abc.jpg" }` |
| Gallery | `[{ "id": 1, "url": "..." }, ...]` |
| Relation | `{ "id": 1, "displayValue": "Item Title" }` |
| MultiRelation | `[{ "id": 1, "displayValue": "..." }, ...]` |
| GeoLocation | `{ "lat": 41.0, "lng": 29.0 }` |
| JSON | `any` (raw JSON) |

---

## Write Request Format (POST / PUT)

```json
{
  "status": "draft",
  "slug": "optional-custom-slug",
  "languageCode": "tr",
  "title": "Display title for this language",
  "isLanguageActive": true,
  "fields": {
    "title": "My Article",
    "summary": "Short description",
    "featured": false,
    "readingTime": 5
  },
  "relations": {
    "category": [1],
    "authors": [2, 3]
  }
}
```

- `fields` keys are camelCase (same as response)
- `relations` keys are camelCase field slugs; values are arrays of target item ids
- Write verbs require a credential with the matching allow_* flag; `is_public = 1` does **not** bypass auth for writes

---

## Field Visibility Rules

- `api_field_visibility.is_visible = false` → field excluded from response
- `api_field_visibility.response_key_alias` → rename field in JSON; enter as kebab-case, stored as-is, output as camelCase
- Unconfigured fields (no row in `api_field_visibility`) default to **visible**
- Relation fields expand to `{ id, displayValue }` by default

---

## Rate Limiting

Implemented via in-memory sliding window per `tenant_id + api_config_id`.  
Default: 60 req/min. Configurable per `api_configurations.rate_limit_per_min`.

Response headers:
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1705312800
```

HTTP 429 on limit exceeded; `Retry-After` header included.

---

## Response Caching

`api_configurations.cache_seconds` — if > 0, successful GET responses are cached in `IMemoryCache`.

Cache keys include: `tenant:ct:lang:page:pageSize:sort:fields + filters`.  
Cache is invalidated when content is written via the API (`IResponseCache.RemoveByPrefix`).

---

## API Explorer

Anyone with a valid credential can access self-documentation at:
```
GET /api/docs/{tenantSlug}
```

Enter an API Key (`vk_...`) or JWT Bearer (`eyJ...`) to see:
- Accessible endpoints (content types)
- Field list with types, localization status, camelCase API keys
- Example request/response JSON

---

## API Management UI Rules

Admin panel at `/admin/api-management`:

1. **Content Types tab** — list of all content types with API toggle
2. **Credentials tab** — create/edit/delete API credentials; one credential can cover multiple content types
3. For each enabled content type → **Configure** page:
   - Enable/disable; choose auth type; set verb flags; set rate limit & cache
   - Field visibility table: checkbox per field + optional alias input (enter kebab-case)
   - Model preview: example request and response JSON with camelCase keys
   - Generate/rotate API key or JWT token (shown once, copy immediately)

---

## Middleware Registration Order

```csharp
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<LanguageResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiRateLimitMiddleware>();   // only for /api/ routes
```
