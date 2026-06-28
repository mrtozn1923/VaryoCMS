# Varyo CMS — Architecture Reference

## Clean Architecture Flow

```
HTTP Request
    ↓
[Web Layer] Controller
    ↓ IApplicationService (Application interface)
[Application Layer] UseCase / Service
    ↓ IRepository (Domain interface)
[Infrastructure Layer] DapperRepository
    ↓ SQL
[SQL Server via Docker]
```

## Project Structure (Full)

```
src/
├── VaryoCms.Domain/
│   ├── Entities/
│   │   ├── Tenant.cs
│   │   ├── ContentType.cs          ← Schema definition (e.g. "Blog Post")
│   │   ├── ContentField.cs         ← Field definition per ContentType
│   │   ├── ContentItem.cs          ← An actual record of a ContentType
│   │   ├── ContentFieldValue.cs    ← EAV: actual values per field per item
│   │   ├── MediaAsset.cs           ← Images, videos, audio
│   │   ├── DictionaryEntry.cs      ← i18n keys
│   │   ├── ApiConfiguration.cs     ← Which content types are exposed
│   │   └── User.cs
│   ├── Enums/
│   │   ├── FieldType.cs            ← Text, Image, Relation, DateRange etc.
│   │   ├── UserRole.cs             ← SystemAdmin, TenantAdmin, Editor, Viewer
│   │   └── ApiAuthType.cs          ← None, ApiKey, JWT
│   ├── Interfaces/
│   │   ├── Repositories/           ← IContentTypeRepository etc.
│   │   └── Services/               ← Domain services if any
│   └── ValueObjects/
│       └── TenantContext.cs
│
├── VaryoCms.Application/
│   ├── Services/
│   │   ├── ContentTypeService.cs
│   │   ├── ContentItemService.cs
│   │   ├── ContentFieldService.cs
│   │   ├── MediaService.cs
│   │   ├── DictionaryService.cs
│   │   ├── ApiConfigurationService.cs
│   │   ├── ApiCredentialService.cs
│   │   ├── UserService.cs
│   │   ├── AuthService.cs
│   │   ├── SystemTenantService.cs
│   │   ├── AuditLogger.cs          ← IAuditLogger impl; best-effort, try/catch → ILogger on fail
│   │   ├── AuditLogService.cs      ← read-side; GetPaged/GetRecentForCT/GetRecent
│   │   └── DashboardService.cs     ← tenant stats + recent activity
│   ├── DTOs/
│   │   ├── ContentType/
│   │   ├── ContentItem/
│   │   ├── Media/
│   │   ├── Dictionary/
│   │   ├── Api/
│   │   ├── Audit/                  ← AuditLogDto
│   │   └── Dashboard/              ← TenantDashboardDto
│   ├── Validators/                 ← FluentValidation
│   ├── Interfaces/
│   │   ├── IApplicationService variants
│   │   ├── IAuditLogger.cs         ← write-side; all services inject this
│   │   ├── IAuditLogService.cs     ← read-side for controllers/ViewComponents
│   │   └── IDashboardService.cs
│   └── Common/
│       ├── Result.cs               ← Result<T> pattern
│       ├── PagedResult.cs
│       ├── AuditActions.cs         ← string constants (Auth.*, ContentItem.*, etc.)
│       └── Exceptions/
│
├── VaryoCms.Infrastructure/
│   ├── Persistence/
│   │   ├── DbConnectionFactory.cs
│   │   ├── UnitOfWork.cs
│   │   └── Repositories/
│   │       ├── ContentTypeRepository.cs
│   │       ├── ContentItemRepository.cs
│   │       ├── ContentFieldRepository.cs
│   │       ├── ContentFieldValueRepository.cs
│   │       ├── MediaRepository.cs
│   │       ├── DictionaryRepository.cs
│   │       ├── TenantRepository.cs
│   │       ├── UserRepository.cs
│   │       ├── AuditLogRepository.cs   ← INSERT + 3 SELECT variants (paged/by-CT/recent)
│   │       └── DashboardRepository.cs  ← single correlated COUNT query
│   ├── Storage/
│   │   ├── IFileStorageService.cs
│   │   └── LocalFileStorageService.cs  ← swap to S3 later
│   ├── Media/
│   │   └── ImageProcessingService.cs   ← crop/resize via SixLabors.ImageSharp
│   ├── Localization/                   ← DB-backed admin UI i18n (global, SystemAdmin-managed)
│   │   ├── DbStringLocalizer.cs        ← IStringLocalizer over ui_translations (current→default→key)
│   │   ├── DbStringLocalizerFactory.cs ← feeds IViewLocalizer / IStringLocalizer<T> / DataAnnotations
│   │   └── UiTranslationStore.cs        ← IMemoryCache cache; Invalidate() on edits
│   └── DependencyInjection.cs          ← AddInfrastructure(services, config)
│
└── VaryoCms.Web/
    ├── Controllers/
    │   ├── Admin/                  ← General Settings (admin only)
    │   │   ├── ContentTypeController.cs
    │   │   ├── UserManagementController.cs
    │   │   ├── ApiManagementController.cs
    │   │   ├── DictionaryController.cs
    │   │   └── AuditLogController.cs    ← /admin/logs (TenantAdmin+)
    │   ├── System/                 ← SystemAdmin platform console (cross-tenant, /system/*)
    │   │   ├── SystemAccountController.cs   ← cross-tenant login/logout/change-password
    │   │   ├── SystemDashboardController.cs ← tenant overview
    │   │   ├── SystemTenantsController.cs   ← tenant CRUD (+ provisioning)
    │   │   ├── ImpersonationController.cs    ← start/exit tenant impersonation
    │   │   └── SystemTranslationsController.cs ← global UI translation mgmt + import/export
    │   ├── ContentController.cs    ← Content CRUD per content type
    │   ├── MediaController.cs
    │   └── Api/
    │       └── PublicApiController.cs  ← Exposed endpoints
    ├── ViewComponents/
    │   ├── NavMenuViewComponent.cs         ← sidebar content-type list
    │   ├── ContentActivityViewComponent.cs ← recent audit log for a content type
    │   └── RecentActivityViewComponent.cs  ← tenant-wide recent audit log (dashboard)
    ├── ViewModels/                 ← NEVER pass Domain entities to Views
    ├── Views/
    ├── Middleware/
    │   ├── TenantResolutionMiddleware.cs
    │   └── LanguageResolutionMiddleware.cs
    ├── Contexts/
    │   ├── ITenantContext.cs + TenantContext.cs
    │   └── ILanguageContext.cs + LanguageContext.cs
    └── wwwroot/
        ├── css/
        │   └── site.css            ← admin design system (dynamiccms-ui skill: cms-* classes, indigo palette)
        ├── js/
        │   ├── site.js             ← admin shell behaviour (sidebar toggle, active nav) — vanilla JS
        │   ├── field-builder.js    ← drag-drop field ordering (SortableJS)
        │   ├── media-editor.js     ← crop/resize UI (Cropper.js)
        │   ├── media-picker.js     ← searchable media picker (single + Gallery)
        │   └── relation-picker.js  ← searchable Relation/MultiRelation picker (language-aware, min/max aware)
        └── lib/
```
> Forms are rendered server-side via Razor partials (`Views/ContentItem/_FieldInput.cshtml`), not a client
> framework. The UI follows the **dynamiccms-ui** skill (`.claude/skills/dynamiccms-ui/`): dark indigo
> sidebar shell + topbar + centralized page header, with a reusable `cms-*` component library in `site.css`
> (Bootstrap 5.3 + Bootstrap Icons; Inter/JetBrains Mono via CDN).

## Key Design Decisions

### EAV (Entity-Attribute-Value) for Content Fields
ContentItems store values in `content_field_values` table.
- Pro: fully dynamic, no schema change per content type
- Con: complex queries — mitigate with JSON aggregation in SQL

### Tenant Resolution
1. Request arrives at `TenantResolutionMiddleware`
2. Extract subdomain: `{slug}.cms.yourdomain.com`
3. Lookup `SELECT id, name FROM tenants WHERE slug = @Slug AND is_active = 1`
4. Set `ITenantContext.TenantId` in scoped DI
5. All repositories read from `ITenantContext` — never from route/query params

### Field Ordering
- `content_fields.sort_order INT` column
- Frontend: SortableJS drag-drop → AJAX PATCH `/admin/content-types/{id}/fields/reorder`
- Backend: bulk UPDATE sort_order in single transaction

### API Exposure
- `api_configurations` table: per-tenant, per-content-type
- `api_field_visibility` table: field-level show/hide per API config
- Public endpoint: `GET /api/v1/{tenant-slug}/{content-type-slug}`
- Auth options: None, ApiKey (header `X-API-Key`), JWT Bearer

## Logging Architecture (Two Channels)

### Channel 1 — Diagnostic (Serilog)
- **NuGet**: `Serilog.AspNetCore`, `Serilog.Sinks.MSSqlServer`, `Serilog.Sinks.Graylog` (Web project)
- `builder.Host.UseSerilog(ctx, sp, cfg => cfg.ReadFrom.Configuration(...).ReadFrom.Services(sp).Enrich.FromLogContext())`
- `app.UseSerilogRequestLogging(...)` enriches with `TenantId`, `UserId`, `Role`, `RequestPath`
- Level policy: 5xx/exception → `Error`, 4xx → `Warning`, 2xx/3xx → `Debug` (filtered out of SQL by min-level)
- Sink config in `appsettings.json` under `Serilog:WriteTo`; add a Graylog block there to enable — **zero code change**
- Table `logs` (migration 031): standard Serilog columns + `TenantId`, `UserId`, `Role`, `RequestPath`

### Channel 2 — Audit Trail (Dapper)
- **Interface**: `IAuditLogger` (Application) → `AuditLogger` (Application/Services)
- **Pattern**: every successful write operation ends with `await _audit.LogAsync(AuditActions.Xyz, ...)`
- **Best-effort**: `AuditLogger` wraps the DB insert in try/catch; on failure logs to `ILogger` (diagnostic channel) — audit never breaks the main operation
- **Covered services**: Auth, ContentItem, ContentType, ContentField, Media, User, Dictionary, ApiCredential, SystemTenant + ImpersonationController
- **Actions**: string constants in `AuditActions` (e.g. `AuditActions.ContentItemCreated = "ContentItem.Created"`)
- Table `audit_logs` (migration 030): `tenant_id`, `user_id`, `user_email`, `user_role`, `action`, `entity_type`, `entity_id`, `content_type_id`, `entity_name`, `created_at`

### Override Parameters
`IAuditLogger.LogAsync` accepts:
- `userEmailOverride` / `userIdOverride` — used at login (HttpContext.User not yet set)
- `tenantIdOverride` — used for system-level operations (SystemTenantService, ImpersonationController) where `ITenantContext.TenantId` is 0

### Rule: Adding a New Feature
Every new service method that creates, updates, or deletes data **must** call `await _audit.LogAsync(...)` after the successful operation. Use the appropriate `AuditActions.*` constant (add a new one if none fits). ViewComponents, read operations, and validation failures do **not** need audit entries.

### Audit UI Surfaces
- `/admin/logs` — filterable, paged audit log (`AuditLogController`, TenantAdmin+)
- `ContentItem/Index` bottom — last 10 events for that content type (`ContentActivityViewComponent`)
- `/` dashboard — stats cards + last 10 tenant-wide events (`RecentActivityViewComponent` via `DashboardService`)

## Result Pattern (use everywhere in Application layer)
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```
