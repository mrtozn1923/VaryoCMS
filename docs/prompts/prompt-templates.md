# Varyo CMS — Prompt Templates

## Kullanım: Bu dosyayı Claude Code'a şu şekilde ver:
## "Use the prompt template from @docs/prompts/prompt-templates.md section: [SECTION NAME]"

---

## SESSION START (Her yeni session başında kullan)

```
Read @CLAUDE.md and @docs/prompts/current-task.md.
Summarize what we've built so far and what the next step is.
Don't write any code yet — just confirm you understand the context.
```

---

## NEW FEATURE: Content Type CRUD

```
Implement [ContentType / ContentItem / Media / Dictionary / User / ApiConfig] feature.

Follow Clean Architecture:
- Domain entity: @docs/architecture.md
- DB schema: @docs/database-schema.md
- Layer rules from @CLAUDE.md

Steps:
1. Domain entity (if not exists)
2. Repository interface (Domain layer)
3. Dapper repository implementation (Infrastructure)
4. Application service interface + implementation
5. DTOs (Request + Response)
6. Controller (Web layer, max 80 lines)
7. ViewModels
8. Views (Razor, Bootstrap 5)
9. Register DI in respective DependencyInjection.cs

Always:
- Include tenant_id filter in every query
- Use ITenantContext
- Async all the way
- Soft delete (is_deleted = 0)
```

---

## NEW MIGRATION

```
Write SQL migration file db/migrations/[NNN]_[description].sql

Rules:
- IF NOT EXISTS checks on all CREATE TABLE
- snake_case table and column names
- Include tenant_id on every table (except tenants, system_admins)
- Every table: created_at DATETIME2, updated_at DATETIME2, is_deleted BIT DEFAULT 0
- Add indexes for: tenant_id, foreign keys, slug columns
- Schema: @docs/database-schema.md
```

---

## NEW SKILL / REPOSITORY METHOD

```
Add method [MethodName] to [IRepositoryName] and [RepositoryName].

Rules from @CLAUDE.md:
- Dapper only, parameterized queries
- Always filter: tenant_id = @TenantId AND is_deleted = 0
- Join translation tables if multilingual data needed (language_code = @LangCode)
- Return: [specific type]
- Register nothing new in DI (already registered)

DB schema reference: @docs/database-schema.md
```

---

## CONTENT FIELD BUILDER (Drag-Drop)

```
Implement the field builder UI for content type [ID/Name].

Requirements:
- SortableJS for drag-and-drop reordering
- Each field type renders its own options panel (see @docs/content-types.md)
- AJAX PATCH /admin/content-types/{id}/fields/reorder on drag end
- Side panel for adding new field: type selector → dynamic options form
- Relation field type: AJAX search for target content type items

Stack: Razor Views + vanilla JS (no framework), Bootstrap 5, SortableJS CDN
```

---

## DYNAMIC FORM RENDERER (Content Item Edit)

```
Implement the content item edit form for content type [ID/Name].

Requirements:
- Load field definitions from ContentType
- Render correct UI component per FieldType (see @docs/content-types.md)
- Language tabs at top for localized fields
- Media fields: open media library modal on click
- Relation fields: searchable AJAX dropdown
- DateRange: Flatpickr range mode
- RichText: TinyMCE
- Save as draft / Publish buttons
```

---

## API ENDPOINT EXPOSURE

```
Implement public API for content type: [slug]

Requirements from @docs/api-design.md:
- Route: GET /api/v1/{tenant-slug}/[slug]
- Auth check: api_configurations.auth_type
- Field visibility: api_field_visibility table
- Pagination, filtering, sorting if enabled
- Rate limiting middleware
- JSON response format per @docs/api-design.md
```

---

## BUG FIX

```
Bug: [describe the symptom]
File: [suspected file path]
Expected: [what should happen]
Actual: [what is happening]

Check:
1. Is tenant_id filter present in the query?
2. Is language_code filter present for localized fields?
3. Is the Result<T> error being swallowed somewhere?
4. Read @CLAUDE.md rules before suggesting fix.
```

---

## MEDIA PROCESSING

```
Implement media [upload / crop / resize / thumbnail generation] for [Image/Video/Audio].

Stack: SixLabors.ImageSharp (already in Infrastructure/Media/ImageProcessingService.cs)
Upload path: /wwwroot/uploads/{tenant_id}/{year}/{month}/
Rules:
- Validate file type from MIME, not extension
- Validate file size against options_json.max_size_mb
- Generate thumbnail at 300x300 for images
- Store result in media_assets table
```

---

## USER PERMISSIONS

```
Implement permission check for [feature].

Role hierarchy: SystemAdmin > TenantAdmin > Editor > Viewer
Table: user_content_type_permissions (can_read, can_create, can_update, can_delete)

Rules:
- TenantAdmin has full access to all content types in their tenant
- Editor: check user_content_type_permissions for specific content_type_id
- Use [Authorize] attribute + custom IAuthorizationHandler
- Never expose another tenant's data
```
