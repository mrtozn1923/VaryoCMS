# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.5.3] - 2026-11-08

### Performance
- **Public API `fields=` projection is now applied before the database read**: previously every visible field value (including large rich-text bodies) was loaded for each list item and only dropped from the response. `PublicApiService.BuildItemsAsync` now narrows the visible fields first and `IPublicApiRepository.GetValuesAsync` takes the projected field ids (`content_field_id IN @FieldIds`); relations and media are expanded only for projected fields. A blog list of ~800 posts no longer reads every post body.
- **New index for public list queries** (`db/migrations/004_public_api_list_index.sql`, idempotent): `IX_content_items_public_list (tenant_id, content_type_id, status, created_at) WHERE is_deleted = 0` — serves the list filter and the default sort.

### Tests
- Unit tests for the projection push-down (only projected ids loaded; no projection → all fields; unknown keys → no value read).

## [1.5.2] - 2026-07-11

### Fixed
- **Every non-media-picker modal stopped receiving clicks** (regression from v1.5.0's TinyMCE stacking fix): the fix raised `.modal-backdrop` to `z-index: 1395` **globally**, which put the backdrop above Bootstrap's default `.modal` (1055) — so modals like the field builder's "Customize Field Options" rendered underneath their own backdrop and every click landed on the backdrop instead. The global CSS rule is removed; the backdrop boost is JS-only now (`media-picker.js` already sets an inline z-index on the backdrop it just created, scoped to the picker-over-TinyMCE case), so other modals are back on Bootstrap's default stack.
- **Signed-in tenant users got 403 on `/api/docs` and public API routes**: `TenantResolutionMiddleware` deliberately skips `/api/*` (the API layer resolves the tenant from the URL slug), leaving `ITenantContext.TenantId` at 0 — but `TenantAccessGuardMiddleware` compared the session's tenant claim against that 0 and rejected every authenticated non-SystemAdmin request. In practice: a TenantAdmin clicking through to the API Explorer from the admin panel hit a bare 403 (anonymous visitors were unaffected, since the guard exempts them — which is why it went unnoticed). The guard now enforces session↔tenant binding only when a tenant was actually resolved (`TenantId > 0`); `/api/*` keeps its own credential-based auth, and the admin-panel binding is unchanged.

### Docs
- README quick-start cleanup: all `docker exec` / `sqlcmd` commands single-line with single-quoted passwords (copy-paste safe in zsh — no history expansion on `!`); added the previously undocumented **"create the `Varyo` database" step** (neither docker-compose nor the migration scripts create it) to Option A, the production-Docker note, and Option C; fixed the broken `dotnet publish/VaryoCms.Web.dll` run command; Tests section now reflects the full 182-test suite and its Docker SQL dependency.

## [1.5.1] - 2026-07-11

### Changed
- **Migration files re-consolidated (7 → 3 in `db/migrations/`, plus a new `db/tenant-seeds/`)**: the migration chain, consolidated to two files at v1.0.0 (`001_v1_schema.sql` + `002_v1_dev_seed.sql`), had grown back out to seven numbered files (001-007) via additive per-feature migrations. Re-consolidated into:
  - `001_v1_schema.sql` — schema, now including what were previously separate schema-only migrations: `content_items.is_read` / `content_types.notify_on_create` / `IX_content_items_unread` (formerly migration 005) and the `login_codes.code` NVARCHAR(10)→(100) widening (formerly migration 006), baked directly into their `CREATE TABLE` statements.
  - `002_v1_system_seed.sql` (renamed from `002_v1_dev_seed.sql`) — SystemAdmin account + ~300 admin-UI translation keys only. Creates **no tenant or content structure** — the starting point for anyone cloning the project to build their own tenant(s)/content types from scratch.
  - `003_v1_demo_seed.sql` (new) — a worked example: `dev-tenant` with languages, a TenantAdmin user, demo content types (Category/Author/Blog Post) with sample content, and a demo API credential. Optional; skip it for a clean install.
  - `db/tenant-seeds/dev-tenant-seed.sql` (new location, formerly `db/migrations/003_dev_tenant_seed.sql` + `004_contact_messages.sql` + `007_dev_tenant_header_tagline.sql`) — the real "Mert Özen" tenant's content, consumed by FrontendSite. No longer part of the migration chain and no longer applied by the test suite; it's environment-specific production content, applied manually.

  Splitting system-bootstrap from demo content (rather than one combined seed file) was necessary because the demo tenant and the real dev-tenant tenant seed can't safely share a file: dev-tenant's seed does a full-instance reset (deletes all tenants before reseeding itself), which would silently wipe the generic `dev-tenant` that `VaryoCms.IntegrationTests`/`VaryoCms.UiTests` depend on if both lived in the same script.

  Verified by applying `001`→`002`→`003` against a scratch database and comparing schema deltas and row counts against the live dev database (schema-defining counts — content types/fields/API configs — matched exactly; the small difference in dev-tenant `content_items` reflects real contact-form submissions accumulated in production, not a consolidation gap). `VaryoCms.IntegrationTests.csproj` and `VaryoCms.UiTests.csproj` updated to copy the new file set; full suite (182/182) green.

## [1.5.0] - 2026-07-07

### Added
- **RichText image insertion from the media library**: TinyMCE's Insert/Edit Image dialog now opens the CMS's own media picker (search + upload tabs) via a new `file_picker_callback` integration — `richtext-editor.js` calls `media-picker.js`'s new `openForCallback()` mode instead of leaving the "Source" field to a bare URL prompt. Inserted images stay tracked `media_assets` rows instead of arbitrary pasted URLs / base64 blobs.
- **`MediaSettings.PublicBaseUrl`** (config section `Media`, e.g. `Media__PublicBaseUrl` env var): prefixes a configurable absolute base URL onto every media URL the CMS returns — admin media search/upload responses and the public API's `{ id, url }` media fields alike (`MediaService.MapToDto`, `PublicApiService.LoadMediaUrlsAsync`). Previously these were DB-relative (`/uploads/{tenant}/{file}`), which only resolved correctly when the consumer shared the CMS's own origin — breaking for any external frontend (e.g. the separate FrontendSite site) or for images embedded directly in RichText HTML. Unset/placeholder values are a no-op (URLs stay relative, current behavior preserved).

### Fixed
- **TinyMCE silently mangled inserted image URLs**: its default `relative_urls: true` rewrites *any* URL dropped into the editor (even an absolute one) into a path relative to the current admin edit page — e.g. `../../../../../uploads/1/abc.png` — which only ever resolves on that exact admin URL. Disabled (`relative_urls: false`, `remove_script_host: false`) so the URL the media picker hands back is preserved verbatim.
- **Media picker modal rendered underneath TinyMCE's own dialog**: TinyMCE's floating-UI root (`.tox-tinymce-aux`) sits at `z-index: 1300`, above Bootstrap's default modal stack (1055/1050). Fixed with a dynamic boost in `media-picker.js` that reads whatever TinyMCE UI layer is actually open at that moment and stacks itself 100 above it — robust against TinyMCE's unpinned CDN version (`tinymce@7` = latest 7.x) shifting that number later, unlike a static CSS override.

> Migration `007_dev_tenant_header_tagline.sql` (dev-tenant tenant only, additive): adds a `header-tagline` field to `site-settings` so the FrontendSite header tagline is CMS-managed instead of hardcoded in the frontend's `.resx` files (which remain as a fallback if the field is left empty).

## [1.4.3] - 2026-07-06

### Security
Full-solution re-audit (22 controllers, 25 repositories, 66 views) with automated sweeps (`dotnet list package --vulnerable` clean; SQL-interpolation / tenant_id-coverage / antiforgery / AllowAnonymous / hardcoded-secret greps) plus three parallel manual reviews (tenant-isolation+SQLi, authz+IDOR+CSRF, XSS+upload+credential). No SQL injection, no cross-tenant IDOR, no XSS, no CSRF gaps; v1.3.0 upload hardening (server-derived extension from MIME allow-map, no SVG, `/uploads` CSP sandbox), JWT HS256 pinning, and BCrypt work-factor 12 all confirmed intact. Fixed:

- **2FA login codes now hashed at rest (Medium)**: `LoginCodeService` stored and compared the 6-digit verification code in **plaintext** in `login_codes.code`. A DB read (leaked backup, insider, or SQLi elsewhere) exposed usable codes. Codes are now BCrypt-hashed via the existing `IPasswordHasher` on write and checked with `Verify` on read (expiry + max-attempts were already enforced). Migration `006_widen_login_code_hash.sql` widens `login_codes.code` from `NVARCHAR(10)` to `NVARCHAR(100)` to hold the hash (additive, idempotent; test projects copy it).
- **`content_field_values` MERGE now matches on `tenant_id` (defense-in-depth)**: `ContentFieldValueRepository.SaveValuesAsync`'s MERGE `ON` clause omitted `tenant_id` (the INSERT stamped it and callers pre-validate item ownership, so not exploitable). Added to the match predicate, mirroring `ContentItemTitleRepository`.
- **`dictionary_translations` now tenant-scoped through its parent (defense-in-depth)**: `DictionaryRepository`'s three translation methods (`GetTranslationsAsync`, `GetTranslatedCountsAsync`, `SaveTranslationsAsync`) touched the child table by `entry_id` only. Callers already validate ownership via the tenant-scoped `GetByIdAsync`, so not exploitable — but the reads now `EXISTS`-join `dictionary_entries` on `tenant_id`, and the write refuses an entry outside the current tenant, mirroring the EAV `content_field_values` pattern.

### Fixed
- **Media crop was broken (500)**: `MediaService.CropAsync` called the MIME-less `SaveAsync` overload, so the storage layer (which resolves the file extension only from a validated MIME, never the user filename) threw and crop 500'd. Now passes the asset's `MimeType`.
- **`ApiManagementController.Configure` binds to the route content type**: the save used the form-posted `ContentTypeId` instead of the route value (tenant-scoped + TenantAdmin-only, so no cross-tenant impact) — now forced to the route `{contentTypeId}`, matching the other admin controllers.

> Accepted/not-fixed (noted for the record): `ContentFieldController` Edit/Delete don't cross-check the route `{contentTypeId}` against the field's owning type — cosmetic only, since the controller is TenantAdmin/SystemAdmin-only and a TenantAdmin already has full access to every content type in their own tenant (repo stays tenant-scoped). JWT remains non-revocable by design (documented in v1.3.0; use an ApiKey credential where revocation is required).

## [1.4.2] - 2026-07-05

### Fixed
- **Language tabs discarded unsaved input**: the content-item create/edit form's TR/EN tabs were plain `<a>` links that triggered a full page GET, reloading the form from the database — any unsaved typing in the outgoing language was lost. Replaced with genuine client-side tabs (`content-item-tabs.js`): every active language's field values load in one page fetch and switching tabs is a pure DOM show/hide, no reload, no data loss. `ContentItemFormFactory.BuildAsync` now gathers every active language up front and splits values into shared (`is_localized=false` fields, `content_items.slug/status`, relations) vs. per-language (`Titles`, `LanguageActive`, `LocalizedValues`) groups. Saving loops the existing single-language `IContentItemService.CreateAsync`/`UpdateAsync` once per language via a new `ContentItemSaveCoordinator` — the Application-layer save contract is unchanged. A language with no submitted content is skipped in the save loop so an untranslated required field never blocks saving the language you're actually editing.
- **`SetRead` silently swallowed failures**: the read/unread toggle action ignored `IContentItemService.SetReadAsync`'s `Result` — a cross-content-type guard failure looked like success to the user. Now surfaces the error via `TempData["Error"]` (new banner on `ContentItem/Index.cshtml`).

### Security
Reviewed everything added since the v1.3.0 audit (submission inbox, `increment-view`, DataProtection persistence, 2FA expiry fix, and this release's language-tab rework) — no new tenant-isolation, IDOR, XSS, or CSRF issues found. `increment-view`/`SetRead` already enforce `tenant_id` + `content_type_id` scoping; the new per-language save loop only ever iterates the tenant's actual active languages (server-resolved), so a posted language code outside that set is never written. Full suite re-run clean: 182/182 (156 unit, 8 integration, 18 UI).

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
