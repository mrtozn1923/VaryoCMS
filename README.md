# Varyo CMS

**Multi-tenant, multilingual headless Content Management System**  
Built with ASP.NET Core 8 · Dapper · SQL Server · Docker

> **v1.0.0** — Production-ready. All features end-to-end verified, 0 security findings (semgrep OWASP + csharp).

---

## What is Varyo CMS?

Varyo CMS is a platform where each tenant has their own isolated content universe — separate users, languages, content types, and API endpoints. Tenants define their own schema (content types + fields), manage content in multiple languages, and expose it via a fully configurable REST API.

**Key differentiators:**
- **Schema-first EAV model** — define any content structure without DB migrations
- **Multi-tenant isolation** — every query scoped to `tenant_id`; no data leakage
- **30+ field types** — from Text/RichText to Relation, Gallery, GeoLocation, Rating
- **Headless API** — fully configurable per content type: auth, rate limiting, field visibility, camelCase keys
- **Dual admin layer** — TenantAdmin manages their space; SystemAdmin manages all tenants cross-tenant
- **2FA email verification** — optional SMTP-backed 6-digit code on every login

---

## Feature Highlights

| Area | What's included |
|---|---|
| **Content Types** | Schema builder with drag-drop field ordering; 30+ field types; `is_published` toggle |
| **Content Items** | EAV-based dynamic forms; multi-language tabs; status (draft/published/archived); slug |
| **Media Library** | Upload image/video/audio/file; in-browser crop (Cropper.js + ImageSharp); searchable picker |
| **Dictionary** | Per-tenant i18n key-value store; multi-language translations |
| **Users & Permissions** | BCrypt passwords; 4 roles; per-content-type CRUD permission matrix |
| **Public REST API** | ApiKey + JWT auth; field visibility/alias; camelCase keys; rate limiting; response caching |
| **API Explorer** | `/api/docs/{tenantSlug}` — credential-gated documentation page |
| **SystemAdmin Console** | `/system` — tenant CRUD, impersonation, global UI translation management |
| **Audit Log** | Two-channel logging: Serilog (diagnostic) + Dapper audit trail; `/admin/logs` UI |
| **2FA** | Optional email verification code (MailKit SMTP; disabled by default) |
| **Docker** | Multi-stage Dockerfile; `docker-compose.yml` with SQL Server + web + named volumes |

---

## Architecture

```
HTTP Request
    ↓
[Web — ASP.NET Core MVC]      Controllers (≤80 lines, 0 business logic)
    ↓ Application interfaces
[Application]                  Services + DTOs + FluentValidation + Result<T>
    ↓ Domain interfaces
[Infrastructure]               Dapper Repositories + FileStorage + ImageSharp
    ↓
[SQL Server via Docker]
```

**Layer dependency rule:** Domain ← Application ← Infrastructure ← Web (one-way only)

### Multi-Tenancy

Every request goes through `TenantResolutionMiddleware`:
1. Extract subdomain: `acme.cms.yourdomain.com` → slug `acme`
2. Lookup tenant in DB (`is_active = 1 AND is_deleted = 0`)
3. Set `ITenantContext.TenantId` (scoped DI) for the lifetime of the request
4. Every repository query **must** include `tenant_id = @TenantId AND is_deleted = 0`

**Dev:** `DevTenantSlug = "dev-tenant"` in `appsettings.Development.json` — localhost skips subdomain.

### SystemAdmin Console

SystemAdmins live in a separate root table (`system_admins`, no `tenant_id`) and sign in at `/system/login`. The console at `/system` provides:
- **Tenant dashboard** — overview of all tenants with user/content counts
- **Tenant CRUD** — create (atomically provisions tenant + language + first admin), edit, soft-delete
- **Impersonation** — re-issues auth cookie with `ImpersonatedTenantId` claim; a banner shows while active; `/system/impersonation/exit` to leave. All tenant-scoped repositories pick up the overridden context without code changes.
- **UI Translation management** — global admin panel i18n (all tenants share one set); import/export JSON; instant cache invalidation

### EAV Content Model

```
content_types  (schema: "Blog Post", "Product", ...)
    └── content_fields  (field definitions: title:Text, body:RichText, cover:Image, ...)
            └── content_items  (actual records)
                    ├── content_field_values  (scalar values: value_text / value_number / value_bool / value_date / value_media_id)
                    ├── content_field_relations  (Relation / MultiRelation links to other items)
                    └── content_item_titles  (per-language display titles, required by Public API)
```

---

## User Roles

| Role | Access |
|---|---|
| `SystemAdmin` | Everything across all tenants; separate login at `/system/login` |
| `TenantAdmin` | Full access within own tenant: Settings, Dictionary, Users, API, Audit Log |
| `Editor` | Content items (per content type, per CRUD action via permission matrix); **no Media library** |
| `Viewer` | Read-only access to permitted content types |

---

## Public REST API

The most powerful feature of Varyo CMS — every content type can be independently configured and exposed.

### Endpoints

```
GET    /api/v1/{tenantSlug}/{contentTypeSlug}
GET    /api/v1/{tenantSlug}/{contentTypeSlug}/{id}
GET    /api/v1/{tenantSlug}/{contentTypeSlug}/slug/{slug}
POST   /api/v1/{tenantSlug}/{contentTypeSlug}        (if allow_create = 1)
PUT    /api/v1/{tenantSlug}/{contentTypeSlug}/{id}   (if allow_update = 1)
DELETE /api/v1/{tenantSlug}/{contentTypeSlug}/{id}   (if allow_delete = 1)
```

### Authentication

| Type | Header | Notes |
|---|---|---|
| `None` | — | Public endpoint (`is_public = 1`) |
| `ApiKey` | `X-API-Key: vk_{credId}_{secret}` | BCrypt-verified in O(1) credential lookup |
| `JWT` | `Authorization: Bearer eyJ...` | CMS-signed HS256, scoped to tenant + content type(s) |

**Credentials are separate from content types** — one credential can cover multiple content types (`api_credential_content_types` junction).

### camelCase Field Keys

All field keys in the API response (`fields` dictionary) are **camelCase**, derived from the field slug:

| Field slug | API key |
|---|---|
| `title` | `title` |
| `hero-title` | `heroTitle` |
| `published-at` | `publishedAt` |
| `seo-meta-description` | `seoMetaDescription` |

**Aliases** set in the admin panel are also normalized to camelCase:
- Alias `reading-time` → response key `readingTime`
- Alias `content` → response key `content`

### Query Parameters

```
GET /api/v1/acme/blog-post?
    lang=en                    ← language (default: tenant default)
    &page=1&pageSize=20        ← pagination (if allow_pagination=1)
    &filter[featured]=true     ← EAV field filter (if allow_filtering=1)
    &filter[readingTime]=5     ← uses camelCase API key
    &sort=publishedAt:desc     ← field sort (if allow_sorting=1)
    &fields=title,summary,category   ← field projection (camelCase keys)
```

### Response Format

```json
{
  "data": [
    {
      "id": 1,
      "slug": "getting-started-with-varyo-cms",
      "fields": {
        "title": "Getting Started with Varyo CMS",
        "summary": "Step-by-step guide to setting up your first tenant.",
        "content": "<p>...</p>",
        "category": { "id": 1, "displayValue": "Technology" },
        "authors": [{ "id": 2, "displayValue": "Alice Johnson" }],
        "publishedAt": "2026-01-10T09:00:00",
        "featured": true,
        "readingTime": 5.0,
        "tags": "[\"cms\",\"tutorial\"]"
      },
      "meta": {
        "createdAt": "2026-01-10T08:00:00Z",
        "updatedAt": "2026-01-10T08:30:00Z",
        "status": "published",
        "language": "en"
      }
    }
  ],
  "pagination": { "page": 1, "pageSize": 20, "total": 3, "totalPages": 1 }
}
```

### Admin Configuration (per content type)

In `/admin/api-management`:
1. Toggle **"Expose as API"**
2. Choose **auth type**: None / ApiKey / JWT
3. Set **verb permissions**: allow_read / allow_create / allow_update / allow_delete
4. Configure rate limit (req/min) and response cache (seconds)
5. **Field visibility table** — toggle each field visible/hidden; set optional alias (enter as kebab-case, output is camelCase)
6. **Model preview** — live request/response JSON example using your actual fields
7. **Generate/rotate** API key or JWT token (shown once)

### Rate Limiting & Caching

```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1705312800
```

- Sliding-window in-memory rate limiter, per `tenant_id + api_config_id`
- Response cache: configurable `cache_seconds` per content type (0 = disabled)

### Write API

Write operations (POST/PUT/DELETE) require a credential (no public write endpoints) and the corresponding verb flag (`allow_create`, `allow_update`, `allow_delete`).

```bash
# Create a blog post
curl -X POST "http://localhost:5267/api/v1/dev-tenant/blog-post" \
  -H "X-API-Key: vk_1_demo123secret" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "draft",
    "slug": "my-new-post",
    "fields": {
      "title": "My New Post",
      "summary": "A short summary"
    }
  }'
```

---

## Demo Data (Dev Seed)

After running `002_v1_dev_seed.sql`, the dev tenant has:

| Resource | Details |
|---|---|
| **Tenant** | `dev-tenant` (localhost → auto-resolved) |
| **TenantAdmin** | `admin@dev.local` / `Admin123!` |
| **SystemAdmin** | `root@system.local` / `Admin123!` |
| **Languages** | `tr` (default), `en` |
| **Content Types** | Category, Author, Blog Post (all published, tr + en) |
| **API** | Blog Post: ApiKey auth, read-only; `vk_{credId}_demo123secret` |

**Test the API:**
```bash
# Check credential id
docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "Varyo_Dev2024!" -C -d Varyo \
  -Q "SELECT id FROM api_credentials WHERE name='Demo Blog API Key';"

# List blog posts in English (replace 1 with actual credential id)
curl "http://localhost:5267/api/v1/dev-tenant/blog-post?lang=en" \
  -H "X-API-Key: vk_1_demo123secret"
```

---

## Quick Start

### Option A — Docker (recommended)

```bash
# 1. Clone
git clone https://github.com/mrtozn1923/VaryoCMS.git && cd VaryoCMS

# 2. Start SQL Server
docker-compose -f docker/docker-compose.yml up -d sqlserver

# 3. Wait ~10 sec for SQL Server to initialize, then apply migrations
docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "Varyo_Dev2024!" -C -d Varyo \
  -i /migrations/001_v1_schema.sql -b

docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "Varyo_Dev2024!" -C -d Varyo \
  -i /migrations/002_v1_dev_seed.sql -b

# 4. Run the web app
dotnet run --project src/VaryoCms.Web

# Open http://localhost:5267
# Login: admin@dev.local / Admin123!
```

**Production Docker (web + DB together):**
```bash
cp docker/.env.example docker/.env
# Edit docker/.env: set DB_PASSWORD and JWT_SIGNING_KEY
docker-compose -f docker/docker-compose.yml up --build
# Apply migrations (same sqlcmd commands, now against varyo_db container)
```

### Option B — Pre-built image from GitHub Packages (GHCR)

Each version tag automatically publishes a Docker image to GHCR via GitHub Actions.

```bash
# Pull a specific version
docker pull ghcr.io/mrtozn1923/varyocms:1.0.0

# Or use it in compose (edit docker/docker-compose.yml, replace build: ... with):
# image: ghcr.io/mrtozn1923/varyocms:1.0.0
```

Available tags per release: `1.0.0`, `1.0`, `1`, `latest`.
> The image may be private — visit GitHub → Packages → varyocms → Change visibility to make it public.

**Releasing a new version:**
```bash
git tag v1.1.0
git push origin v1.1.0
# GitHub Actions builds and pushes ghcr.io/mrtozn1923/varyocms:1.1.0 automatically
```

### Option C — Classic publish

```bash
# 1. Apply schema to your SQL Server
sqlcmd -S YOUR_SERVER -U sa -P YOUR_PASS -d Varyo \
  -i db/migrations/001_v1_schema.sql -b

# 2. Apply dev seed (optional)
sqlcmd -S YOUR_SERVER -U sa -P YOUR_PASS -d Varyo \
  -i db/migrations/002_v1_dev_seed.sql -b

# 3. Set production config (env vars or appsettings.Production.json)
export ConnectionStrings__VaryoCms="Server=...;Database=Varyo;..."
export Jwt__SigningKey="your-secret-key-32-chars-minimum"

# 4. Publish and run
dotnet publish src/VaryoCms.Web -c Release -o ./publish
dotnet publish/VaryoCms.Web.dll
```

---

## Migration Files

```
db/migrations/
├── 001_v1_schema.sql     — Complete production schema (all 26 tables, indexes, constraints)
└── 002_v1_dev_seed.sql   — DEV ONLY: tenant, users, languages, 300+ UI translations (tr+en),
                            audit action labels, Tech Blog demo content + API credential
```

**Production:** run only `001_v1_schema.sql`, then insert your own tenant/user data.  
For v1.1.0+ changes, add `003_<description>.sql`, `004_<description>.sql`, etc.

---

## Continuing Development

### With Claude Code

All project context is pre-loaded via `CLAUDE.md` and `@docs/...` references. Start any session:
```
Read @docs/prompts/current-task.md and continue from where we left off.
```

### Without Claude Code

All architectural decisions and invariants are documented in:

| File | Content |
|---|---|
| `CLAUDE.md` | Layer rules, naming, coding standards, what NOT to do |
| `docs/architecture.md` | Full architecture reference, logging, result pattern |
| `docs/database-schema.md` | All 26 tables with column details |
| `docs/content-types.md` | All 30+ field types with storage and UI details |
| `docs/api-design.md` | API contract, camelCase convention, auth modes |
| `docs/multitenancy.md` | Tenant resolution, impersonation, SystemAdmin console |
| `docs/prompts/current-task.md` | Active task tracker |

---

## Tests

```bash
dotnet test VaryoCms.sln
# 156 unit tests (xUnit + NSubstitute); no DB required
# Tests: FieldValueMapper, RelationOptions, BCryptPasswordHasher,
#        JwtTokenService, PermissionService, ContentItemService,
#        PublicApiService, SlugifierTests (including ToCamelCase)
```

---

## Security

- semgrep `p/owasp-top-ten + p/csharp` — 0 findings (v1.0.0)
- Global `AuthorizeFilter`; explicit `[Authorize]` on all controllers
- BCrypt workfactor 12 for all passwords and API key hashes
- Parameterized SQL only; no string interpolation in queries
- `tenant_id` filter on every repository query
- Login rate limiting; CSRF on all state-changing forms; `HttpOnly + SameSite=Lax` cookies
- Path traversal sanitization in media upload/rename; MIME type whitelist

---

## License

MIT
