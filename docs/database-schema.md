# Varyo CMS — Database Schema

> All tables except `tenants` and `system_admins` include `tenant_id INT NOT NULL`.
> All tables include `is_deleted BIT NOT NULL DEFAULT 0`, `created_at DATETIME2`, `updated_at DATETIME2`.

---

## Core Tables

### tenants
```sql
CREATE TABLE tenants (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(200) NOT NULL,
    slug        NVARCHAR(100) NOT NULL UNIQUE,   -- subdomain key
    is_active   BIT NOT NULL DEFAULT 1,
    created_at  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted  BIT NOT NULL DEFAULT 0
);
```

### users
```sql
CREATE TABLE users (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id       INT NOT NULL REFERENCES tenants(id),
    email           NVARCHAR(256) NOT NULL,
    password_hash   NVARCHAR(512) NOT NULL,
    full_name       NVARCHAR(200),
    role            NVARCHAR(50) NOT NULL,   -- SystemAdmin | TenantAdmin | Editor | Viewer
    is_active       BIT NOT NULL DEFAULT 1,
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL DEFAULT 0,
    UNIQUE (tenant_id, email)
);
```

### user_content_type_permissions
```sql
CREATE TABLE user_content_type_permissions (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id       INT NOT NULL REFERENCES tenants(id),
    user_id         INT NOT NULL REFERENCES users(id),
    content_type_id INT NOT NULL REFERENCES content_types(id),
    can_read        BIT NOT NULL DEFAULT 1,
    can_create      BIT NOT NULL DEFAULT 0,
    can_update      BIT NOT NULL DEFAULT 0,
    can_delete      BIT NOT NULL DEFAULT 0
);
```

---

## Content Type System (Schema Builder)

### content_types
```sql
CREATE TABLE content_types (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id       INT NOT NULL REFERENCES tenants(id),
    name            NVARCHAR(200) NOT NULL,
    slug            NVARCHAR(200) NOT NULL,   -- used in URL + API
    description     NVARCHAR(1000),
    icon            NVARCHAR(100),            -- icon class e.g. "bi-file-text"
    is_published    BIT NOT NULL DEFAULT 0,
    sort_order      INT NOT NULL DEFAULT 0,   -- menu order
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL DEFAULT 0,
    UNIQUE (tenant_id, slug)
);
```

### content_fields  ← field definitions per content type
```sql
CREATE TABLE content_fields (
    id                      INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id               INT NOT NULL REFERENCES tenants(id),
    content_type_id         INT NOT NULL REFERENCES content_types(id),
    name                    NVARCHAR(200) NOT NULL,
    slug                    NVARCHAR(200) NOT NULL,
    field_type              NVARCHAR(50) NOT NULL,    -- see FieldType enum
    is_required             BIT NOT NULL DEFAULT 0,
    is_localized            BIT NOT NULL DEFAULT 1,   -- per-language value?
    sort_order              INT NOT NULL DEFAULT 0,
    options_json            NVARCHAR(MAX),            -- JSON: validation rules, choices, relation config
    created_at              DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at              DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted              BIT NOT NULL DEFAULT 0,
    UNIQUE (content_type_id, slug)
);
```

**options_json examples by field type:**
```json
// Select / MultiSelect
{"choices": ["Option A", "Option B", "Option C"]}

// Relation / MultiRelation
{"target_content_type_id": 5, "display_field_slug": "title", "value_field_slug": "id"}

// Image / Video / Audio
{"max_size_mb": 10, "allowed_formats": ["jpg","png","webp"], "aspect_ratio": "16:9"}

// DateRange
{"min_date": null, "max_date": null}

// Number / Decimal
{"min": 0, "max": 999999, "decimals": 2}

// Text
{"max_length": 500, "placeholder": "Enter title..."}
```

---

## Content Items (Records)

### content_items
```sql
CREATE TABLE content_items (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id       INT NOT NULL REFERENCES tenants(id),
    content_type_id INT NOT NULL REFERENCES content_types(id),
    slug            NVARCHAR(500),           -- auto-generated from title field if present
    status          NVARCHAR(50) NOT NULL DEFAULT 'draft',  -- draft | published | archived
    created_by      INT REFERENCES users(id),
    updated_by      INT REFERENCES users(id),
    published_at    DATETIME2,
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL DEFAULT 0
);
```

### content_field_values  ← EAV: actual data
```sql
CREATE TABLE content_field_values (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id       INT NOT NULL REFERENCES tenants(id),
    content_item_id INT NOT NULL REFERENCES content_items(id),
    content_field_id INT NOT NULL REFERENCES content_fields(id),
    language_code   CHAR(5) NOT NULL DEFAULT 'tr',   -- 'tr', 'en', 'de' etc.
    value_text      NVARCHAR(MAX),    -- Text, RichText, Markdown, Email, URL, JSON etc.
    value_number    DECIMAL(18,6),    -- Number, Decimal, Rating
    value_bool      BIT,              -- Boolean
    value_date      DATETIME2,        -- Date, DateTime, Time
    value_date_end  DATETIME2,        -- DateRange end
    value_media_id  INT REFERENCES media_assets(id),  -- Image, Video, Audio, File
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE (content_item_id, content_field_id, language_code)
);
```

### content_field_relations  ← for Relation / MultiRelation fields
```sql
CREATE TABLE content_field_relations (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id           INT NOT NULL REFERENCES tenants(id),
    source_item_id      INT NOT NULL REFERENCES content_items(id),
    source_field_id     INT NOT NULL REFERENCES content_fields(id),
    target_item_id      INT NOT NULL REFERENCES content_items(id),
    sort_order          INT NOT NULL DEFAULT 0
);
```

---

## Media

### media_assets
```sql
CREATE TABLE media_assets (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id       INT NOT NULL REFERENCES tenants(id),
    file_name       NVARCHAR(500) NOT NULL,
    original_name   NVARCHAR(500) NOT NULL,
    file_path       NVARCHAR(1000) NOT NULL,
    media_type      NVARCHAR(50) NOT NULL,    -- image | video | audio | file
    mime_type       NVARCHAR(100),
    file_size_bytes BIGINT,
    width           INT,                       -- for images/video
    height          INT,
    duration_secs   DECIMAL(10,2),            -- for video/audio
    alt_text        NVARCHAR(500),
    metadata_json   NVARCHAR(MAX),            -- EXIF, crop info etc.
    uploaded_by     INT REFERENCES users(id),
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL DEFAULT 0
);
```

---

## Localization

### languages  ← per tenant
```sql
CREATE TABLE languages (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id       INT NOT NULL REFERENCES tenants(id),
    code            CHAR(5) NOT NULL,         -- 'tr', 'en', 'de'
    name            NVARCHAR(100) NOT NULL,   -- 'Türkçe', 'English'
    is_default      BIT NOT NULL DEFAULT 0,
    is_active       BIT NOT NULL DEFAULT 1,
    flag_icon       NVARCHAR(100)
);
```

### dictionary_entries  ← i18n keys
```sql
CREATE TABLE dictionary_entries (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id   INT NOT NULL REFERENCES tenants(id),
    key_name    NVARCHAR(300) NOT NULL,       -- e.g. 'nav.home', 'btn.save'
    category    NVARCHAR(100),               -- 'navigation', 'buttons', 'errors'
    created_at  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted  BIT NOT NULL DEFAULT 0,
    UNIQUE (tenant_id, key_name)
);

CREATE TABLE dictionary_translations (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    entry_id        INT NOT NULL REFERENCES dictionary_entries(id),
    language_code   CHAR(5) NOT NULL,
    value           NVARCHAR(MAX) NOT NULL,
    UNIQUE (entry_id, language_code)
);
```

---

## API Management

### api_configurations
```sql
CREATE TABLE api_configurations (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    tenant_id           INT NOT NULL REFERENCES tenants(id),
    content_type_id     INT NOT NULL REFERENCES content_types(id),
    is_enabled          BIT NOT NULL DEFAULT 0,
    auth_type           NVARCHAR(50) NOT NULL DEFAULT 'None',  -- None | ApiKey | JWT
    api_key             NVARCHAR(500),
    allow_filtering     BIT NOT NULL DEFAULT 1,
    allow_sorting       BIT NOT NULL DEFAULT 1,
    allow_pagination    BIT NOT NULL DEFAULT 1,
    rate_limit_per_min  INT DEFAULT 60,
    cache_seconds       INT DEFAULT 0,
    created_at          DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at          DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE (tenant_id, content_type_id)
);
```

### api_field_visibility
```sql
CREATE TABLE api_field_visibility (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    api_configuration_id INT NOT NULL REFERENCES api_configurations(id),
    content_field_id    INT NOT NULL REFERENCES content_fields(id),
    is_visible          BIT NOT NULL DEFAULT 1,
    response_key_alias  NVARCHAR(200)    -- rename field in JSON response
);
```

---

## Migration Files (v1.0.0 — consolidated)

As of v1.0.0, all 39 incremental migrations have been consolidated into two files:

```
db/migrations/
├── 001_v1_schema.sql      — Complete production schema (all tables, indexes, constraints)
└── 002_v1_dev_seed.sql    — DEV-ONLY: tenant, users, languages, ~300 UI translation keys (tr+en)
```

> **Production:** run only `001_v1_schema.sql` and insert your own tenant/user seed data.
> **Development:** run both files in order.
>
> For v1.1.0+ migrations, add numbered files: `003_<description>.sql`, `004_<description>.sql`, etc.

### ui_cultures / ui_translations  (root tables — no tenant_id; global admin-UI i18n, SystemAdmin-managed)
```sql
CREATE TABLE ui_cultures (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(10) NOT NULL,   -- 'tr','en','de'  (unique)
    name NVARCHAR(100) NOT NULL,
    is_default BIT NOT NULL DEFAULT 0,
    is_active  BIT NOT NULL DEFAULT 1
);
CREATE TABLE ui_translations (
    id INT IDENTITY(1,1) PRIMARY KEY,
    culture      NVARCHAR(10) NOT NULL,
    resource_key NVARCHAR(300) NOT NULL,
    value        NVARCHAR(MAX) NOT NULL,
    -- UNIQUE (culture, resource_key)
);
```
> Global (no tenant_id) — one admin-UI translation set for all tenants, edited by SystemAdmin at
> `/system/translations`. A custom `IStringLocalizer` reads these (IMemoryCache-cached; invalidated on edit).

### system_admins  (root table — no tenant_id; cross-tenant platform owners)
```sql
CREATE TABLE system_admins (
    id            INT IDENTITY(1,1) PRIMARY KEY,
    email         NVARCHAR(256) NOT NULL,    -- unique among non-deleted (filtered index)
    password_hash NVARCHAR(512) NOT NULL,    -- BCrypt
    full_name     NVARCHAR(200),
    is_active     BIT NOT NULL DEFAULT 1,
    created_at    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted    BIT NOT NULL DEFAULT 0
);
```
> No `tenant_id` and no `role` column — every row is a SystemAdmin. Separate from `users` so the
> `users.tenant_id NOT NULL` invariant and tenant-scoped queries stay intact.
> Note: 007 (indexes/constraints) was never created — filtered unique indexes and FKs were folded into
> migrations 001–006. Numbering jumps from 006 to the 008+ dev seeds. Dev seeds are idempotent; do NOT
> run against production (and change the seeded admin password after first login).

### audit_logs  (tenant-scoped business event log — migration 030)
```sql
CREATE TABLE audit_logs (
    id             BIGINT IDENTITY(1,1) PRIMARY KEY,
    tenant_id      INT NOT NULL REFERENCES tenants(id),
    user_id        INT NULL,
    user_email     NVARCHAR(256) NULL,
    user_role      NVARCHAR(50) NULL,
    action         NVARCHAR(100) NOT NULL,   -- e.g. 'ContentItem.Created'
    entity_type    NVARCHAR(100) NULL,
    entity_id      INT NULL,
    content_type_id INT NULL,                -- for ContentItem/Field events
    entity_name    NVARCHAR(500) NULL,
    description    NVARCHAR(MAX) NULL,
    metadata_json  NVARCHAR(MAX) NULL,
    ip_address     NVARCHAR(64) NULL,
    created_at     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    -- NO is_deleted — logs are immutable
);
-- Indexes: IX_audit_tenant_created (tenant_id, created_at DESC)
--          IX_audit_tenant_ct_created (tenant_id, content_type_id, created_at DESC) WHERE content_type_id IS NOT NULL
```
> Queried by `IAuditLogRepository` (tenant-scoped). Written by `AuditLogger` (best-effort, non-blocking).
> System-level operations (SystemTenantService, ImpersonationController) pass `tenantIdOverride` to log
> against the affected tenant. If `tenantId <= 0` the entry is silently skipped (no FK violation).

### logs  (Serilog diagnostic sink — migration 031)
Standard Serilog MSSqlServer columns (`Message`, `Level`, `TimeStamp`, `Exception`, `Properties`) plus
`TenantId INT NULL`, `UserId INT NULL`, `Role NVARCHAR(50) NULL`, `RequestPath NVARCHAR(500) NULL`.
> Populated automatically by `UseSerilogRequestLogging` for Warning+ events (4xx, 5xx, exceptions).
> Switch to Graylog by adding a sink block to `Serilog:WriteTo` in `appsettings.json` — no code change.
