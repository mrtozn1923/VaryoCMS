# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.1] - 2026-07-05

### Fixed
- **2FA verify-code page crashed on production (IIS)**: `Login.VerifyCode.ExpiryHint` UI translation contains a `{0}` placeholder, but `Views/Account/VerifyCode.cshtml` and `Views/System/SystemAccount/VerifyCode.cshtml` called `@L["Login.VerifyCode.ExpiryHint"]` with no format argument. `IHtmlLocalizer` always formats a `LocalizedHtmlString` via `HtmlFormattableString`, so a `{0}`-less-args mismatch throws `FormatException` — and since Razor defers `IHtmlContent` writes to the layout flush, the exception fired mid-response (after headers were already sent), producing a blank page / `ERR_HTTP2_PROTOCOL_ERROR` in the browser instead of a clean 500. Both views now pass the actual configured expiry (`ILoginCodeService.GetExpiryMinutesAsync`, new interface member) as the format argument, matching the value already used in the emailed code.
- **DataProtection key ring ephemeral on IIS/Plesk**: `Program.cs` now calls `AddDataProtection().PersistKeysToFileSystem(...)` (keys under `ContentRootPath/dp-keys`) with a fixed application name. Without a user profile or HKLM registry (shared IIS hosting), keys were regenerated on every app-pool recycle, invalidating antiforgery tokens, TempData and auth cookies across restarts.

## [1.4.0] - 2026-07-03

### Added
- **Content-item read/unread inbox**: new `content_items.is_read` column. Items created via the public API start unread (`0`). The admin item list shows an unread badge, a mark-read/unread toggle, and an "unread only" filter — but only for content types flagged as a submission inbox (see `notify_on_create` below), so ordinary content types are unaffected. New audit action `ContentItem.ReadToggled`. Endpoint: `POST /admin/content-types/{ctId}/items/{id}/read` (Update permission, antiforgery, cross-content-type IDOR guard).
- **Submission notification email**: new `content_types.notify_on_create` flag marks a content type as a "form submission inbox". When set, creating an item via the public API sends a best-effort notification email to the tenant (`ISubmissionNotifier` → `SmtpEmailSender`, per-tenant SMTP from `tenant_email_settings` with system fallback). Best-effort: a mail/SMTP failure is swallowed and never breaks the submission. `contact-message` is flagged by the migration. The same flag drives the read/unread inbox UI above.
- **Public API view-count increment**: `POST /api/v1/{tenant}/{contentTypeSlug}/{id}/increment-view` atomically does `value_number = value_number + 1` on the item's `view-count` field in a single statement (no read-modify-write race). Bound to **read** permission (a benign analytics side-effect of reading), so no new verb flag or `allow_update` is required. Deliberately not audited (high-frequency) and does not invalidate the response cache (counts may lag by the cache TTL — acceptable for a counter).
- Migration `005_inbox_and_notify.sql` (additive, idempotent): the two columns above + a filtered `IX_content_items_unread` index + the inbox UI translation keys. Test projects (integration + UI) now include `005` so repository queries against the new columns pass.

> **Frontend (separate FrontendSite repo):** the blog detail page now triggers the view-count increment once per unique visitor, deduplicated by `IP + slug` via `IMemoryCache` (a 6-hour window, so refreshes don't recount; `X-Forwarded-For`-aware for proxies). The contact form (wired in 1.3.x) posts submissions that now land as unread inbox items and fire the notification email.

---

## [1.3.1] - 2026-07-02

### Added
- **`contact-message` content type (dev seed)**: new additive migration `db/migrations/004_contact_messages.sql` creates a contact-message content type for the `dev-tenant` tenant (fields: `name`, `email`, `subject`, `message`) so the FrontendSite frontend contact form can store submissions in the CMS. Exposed via a **create-only** public API config (`allow_create=1`, read/update/delete off, `is_public=0`, `rate_limit=20/min`) and granted to the existing Tenant API key. Idempotent + additive (does not reset); run after `003`.

> Frontend wiring (ContactController POST → `SubmitContactMessageAsync`, antiforgery + PRG, honeypot/timestamp anti-spam) lives in the separate FrontendSite repository.

---

## [1.3.0] - 2026-07-02

### Security
Full audit pass (SQL injection, tenant isolation, CSRF, XSS, authz/IDOR, upload, auth, API credentials) across the whole solution. SQLi and tenant-isolation were clean; the findings below were fixed.

- **Arbitrary file extension on upload → stored XSS (Critical)**: `LocalFileStorageService` no longer falls back to the user-supplied filename extension. The stored extension is now always resolved from the validated MIME type via a single allow-map (`MediaAllowedTypes.TryGetExtension`); an allowed-but-unmapped MIME is rejected instead of stored as an attacker-chosen `.html`/`.js`. The MIME allow-list and the MIME→extension map are now the same map (previously the list was a superset, which enabled the bypass).
- **SVG stored XSS (High)**: `image/svg+xml` removed from the media allow-list (script-capable, served inline). As defense-in-depth, `/uploads` is now served with `Content-Security-Policy: sandbox; default-src 'none'` + `X-Content-Type-Options: nosniff`, so any stray active-content file cannot execute in the app origin.
- **Public API DELETE cross-content-type IDOR (High)**: `PublicApiWriteRepository.SoftDeleteItemAsync` now filters `content_type_id` in addition to `tenant_id`. A credential granted delete on content type A can no longer delete items of content type B in the same tenant (Update/Create were already content-type-scoped).
- **Admin content-item cross-content-type IDOR (High)**: `ContentItemService.UpdateAsync`/`DeleteAsync` and the edit-form factory now verify the item belongs to the route content type the caller was permission-checked against. Previously an Editor with rights on one content type could edit/delete items of any content type in the tenant by crafting the item id.
- **Login rate limiter now per-IP (Medium)**: the `"login"` limiter is partitioned by `RemoteIpAddress` (was a single global bucket that let one attacker lock out all users). 10 attempts / 5 min per IP.
- **Session bound to resolved tenant (Medium)**: new `TenantAccessGuardMiddleware` rejects (403) an authenticated non-SystemAdmin request whose cookie `TenantId` claim doesn't match the subdomain-resolved tenant — closes a cross-tenant gap should a cookie ever reach another tenant's host.
- **Auth cookie `Secure` flag (Medium)**: `Cookie.SecurePolicy = Always` in non-Development (Dev stays `SameAsRequest` for plain-HTTP localhost).
- **JWT lifetime reduced 365 → 90 days (Medium)**: stateless JWTs can't be individually revoked, so the shorter lifetime bounds a leaked token's window. ApiKey credentials remain the revocable option (checked against `is_active`/`is_deleted` per request).
- **JWT algorithm pinning (Low)**: `TokenValidationParameters.ValidAlgorithms = ["HS256"]` on both validation paths — defense-in-depth against algorithm-confusion.
- **User-permission content-type validation (Low)**: `UserService.SavePermissionsAsync` drops permission rows referencing content types that don't belong to the tenant instead of persisting orphan grants.

### Chore
- `003_dev_tenant_seed.sql` (dev-only): the 14 blog posts' `body` fields were expanded from single-paragraph stubs into full multi-section articles (H2/H3 headings, code blocks, lists) in both `tr` and `en`, so the frontend table-of-contents (derived from `<h2>`/`<h3>`) renders meaningfully.

---

## [1.2.0] - 2026-07-01

### Added
- **RichText WYSIWYG editor (TinyMCE)**: RichText fields now use TinyMCE v7 (CDN, oxide-dark skin) instead of a plain textarea. Markdown fields remain as plain textarea. New `richtext-editor.js` initialises all `.cms-richtext` textareas on the page.
- **Content-Security-Policy header**: CSP added to the security headers middleware. Allows `unsafe-inline` for TinyMCE/Monaco; `frame-ancestors 'none'` blocks clickjacking.

### Fixed
- **CSRF on language switcher**: `LanguagePreferenceController.Set()` changed from `GET` to `POST + [ValidateAntiForgeryToken]`; view updated to render a `<form>` instead of an anchor link.
- **XSS in JS string context** (`Views/Media/Index.cshtml`): `@Html.Raw(L["..."].Value)` replaced with `@Json.Serialize(L["..."].Value)` in inline script blocks.
- **Filename injection** (`SystemTranslationsController.Export`): `culture` parameter sanitized with `Regex.Replace` before use in the `Content-Disposition` filename.
- **EAV tenant isolation** (`PublicApiRepository`): Filter and sort subqueries over `content_field_values` now include `fv.tenant_id = ci.tenant_id` guard.
- **`[AllowAnonymous]` on error page** (`HomeController.Error`): Error action was behind the global `[Authorize]` filter, causing authenticated-only error pages in production.
- **`TrustServerCertificate=False`** in `appsettings.Production.json`: Was incorrectly `True`.
- **JWT `SigningKey` placeholder**: Production config now uses `SET_VIA_ENV_Jwt__SigningKey` to signal the env-var pattern instead of a literal placeholder.
- **Test isolation**: Integration and UI test projects now copy only `001_v1_schema.sql` + `002_v1_dev_seed.sql`; the `003_dev_tenant_seed.sql` dev reset seed is excluded so `dev-tenant` exists for all test fixtures.

### Changed
- `AllowedHosts` restricted from `"*"` to `"localhost;127.0.0.1;[::1]"` in `appsettings.json` (development); override for production domains via environment variable.
- `appsettings.Production.json` added to `.gitignore` — no longer tracked after this release.

### Chore
- `003_dev_tenant_seed.sql` (dev-only): 13 content types (`site-settings`, `category`, `series`, `post`, `video`, `about`, `video-list`, `experience`, `education`, `skill-group`, `book`, `movie`, `activity`), ApiKey auth, 14 posts, 8 videos, series Relation, `youtubeId` field, `site-settings` singleton for social links.

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
