# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.0] - 2026-06-29

### Added
- **Modal-based media picker**: Replaced inline search-dropdown with a "Dosya Seç" trigger button that opens a shared Bootstrap modal per page. Modal has two tabs: **Kütüphane** (library search + thumbnail grid) and **Bilgisayardan Yükle** (local file upload). Gallery fields show a multi-select grid with "Seçilenleri ekle" commit button.
- **In-form file upload**: Editors and admins can now upload media directly from a content edit form without leaving the page. Upload auto-selects the freshly uploaded asset.
- **Editor upload permission**: `Editor` role can now upload to the tenant media library (previously TenantAdmin/SystemAdmin only).
- **Per-field upload constraints**: `IMediaService.UploadAsync` accepts optional `maxSizeMb` and `allowedFormats` parameters; both client-side (JS validation) and server-side (service layer) enforcement. Global 50 MB + MIME allow-list remains the authoritative upper bound.
- 9 new UI translation keys (`ContentItem.MediaSelectButton`, `MediaModalTitle`, `MediaTabLibrary`, `MediaTabUpload`, `MediaModalDone`, `MediaModalEmpty`, `MediaTooLarge`, `MediaFormatNotAllowed`) in `002_v1_dev_seed.sql` (tr + en).

---

## [1.0.0] - 2026-06-27

### Added — Core Platform
- Multi-tenant architecture with subdomain-based resolution (`TenantResolutionMiddleware`)
- Clean Architecture (4 layers: Domain, Application, Infrastructure, Web)
- Dapper-only data access layer; all queries parameterized and tenant-scoped
- `Result<T>` pattern throughout Application layer
- FluentValidation for all request DTOs
- Cookie authentication with sliding expiration (8h) and remember-me (30d)
- Global `AuthorizeFilter` with `[AllowAnonymous]` on public endpoints
- BCrypt password hashing (workfactor 12)

### Added — Content System
- Dynamic Content Types with name, slug, icon, description, sort_order, parent hierarchy
- 30+ field types: Text, RichText, Markdown, Number, Decimal, Boolean, Date, DateTime, Time, DateRange, Email, URL, Phone, Color, JSON, CodeSnippet, Image, Video, Audio, File, Gallery, Select, MultiSelect, Tags, Relation, MultiRelation, Slug, Password, GeoLocation, Rating
- EAV storage (`content_field_values`) with per-field localization flag
- Drag-and-drop field reordering (SortableJS + PATCH endpoint)
- Content item CRUD with dynamic server-rendered forms
- Content item status: draft / published / archived
- Soft-delete everywhere with filtered unique indexes
- Collapsible tree sidebar (parent/child content type hierarchy)

### Added — Media
- File upload to `/wwwroot/uploads/{tenantId}/` (GUID filenames, MIME-derived extension)
- Format whitelist (image, video, audio, PDF, Office, TXT)
- In-place image crop (Cropper.js + SixLabors.ImageSharp 2.1.10)
- Searchable media picker for all media field types (AJAX, thumbnail grid)
- Gallery field: multi-select media picker, stored as JSON array
- Filename rename, alt text edit, file size display, type filter tabs

### Added — Multilingual
- Per-tenant language management (`languages` table)
- Content field values stored per language_code
- Language switcher tabs in content edit form
- DB-backed admin UI i18n (`ui_cultures` + `ui_translations`, ~300 keys, tr + en)
- `DbStringLocalizer` / `DbStringLocalizerFactory` (IMemoryCache, fallback chain)
- `LanguageResolutionMiddleware` sets `CultureInfo` from `cms_lang` cookie

### Added — Public REST API
- GET list, GET by ID, GET by slug endpoints
- POST (create), PUT (update), DELETE (soft-delete) with credential requirement
- Authentication: None / ApiKey (`vk_{id}_{secret}` BCrypt-hashed) / JWT (CMS-signed HS256, per-CT scope)
- Per-verb permission flags: `allow_read/create/update/delete`
- EAV field filtering (`filter[slug]=value`), sorting, pagination
- Field projection (`fields=`), response key alias, visibility control
- In-memory response caching (`cache_seconds`) with cache invalidation on write
- Fixed-window rate limiting (configurable per API config)
- Relation field expansion (`{id, displayValue}`), Gallery/media expansion (`{id, url}`)

### Added — Roles & Permissions
- 4 roles: SystemAdmin, TenantAdmin, Editor, Viewer
- Per-content-type CRUD permission matrix (`user_content_type_permissions`)
- Dynamic sidebar filtered by accessible content types
- Action-level authorization via `RequireContentTypePermissionAttribute`
- UI button visibility based on effective permissions

### Added — SystemAdmin Console
- Separate `system_admins` table (no tenant_id)
- `/system/login` with email 2FA support
- `/system` platform dashboard
- `/system/tenants` CRUD + atomic tenant provisioning
- Tenant impersonation via `ImpersonationMiddleware`
- `/system/translations` global UI i18n management + JSON import/export

### Added — Audit & Logging
- `audit_logs` table — tenant-scoped business events, best-effort IAuditLogger
- `logs` table — Serilog MSSqlServer sink (Warning+); Graylog-ready
- Audit UI: `/admin/logs` (filterable, paged), content item widget, dashboard widget

### Added — Dictionary
- Per-tenant key-value translation store (`dictionary_entries` + `dictionary_translations`)
- MERGE upsert; empty value → DELETE

### Added — Email 2FA
- Optional 6-digit code via MailKit SMTP
- `login_codes` table (5-min TTL, 3 max attempts, used_at invalidation)
- Configurable globally (appsettings) and per-tenant (DB)

### Added — Security
- CSRF antiforgery on all state-changing forms + `X-CSRF-TOKEN` header for AJAX
- Security headers middleware (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy)
- Login rate limiting: 10 attempts / 5 minutes (ASP.NET Core AddRateLimiter)
- Path traversal guard in file storage (canonical path validation)
- MIME-controlled file extension (server-side MimeExtensions map)
- Privilege escalation prevention (UserService rejects SystemAdmin role assignment)
- semgrep p/owasp-top-ten + p/csharp: 0 findings

### Added — Docker & Deployment
- Multi-stage Dockerfile (.NET 8 Alpine, port 8080)
- `docker-compose.yml` with `sqlserver` + `web` services, named volume for media persistence
- `docker/.env.example` template
- `appsettings.Production.json` (Warning log level, Serilog SQL sink)

### Changed
- All 39 incremental migration files consolidated into 2 files:
  - `db/migrations/001_v1_schema.sql` — production schema
  - `db/migrations/002_v1_dev_seed.sql` — dev-only seed data

[1.0.0]: https://github.com/mrtozln1923/VaryoCMS/releases/tag/v1.0.0
