SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- TENANTS & USERS
-- ============================================================

CREATE TABLE tenants (
    id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tenants PRIMARY KEY,
    name        NVARCHAR(200) NOT NULL,
    slug        NVARCHAR(100) NOT NULL,            -- subdomain key
    is_active   BIT NOT NULL CONSTRAINT DF_tenants_is_active DEFAULT 1,
    created_at  DATETIME2 NOT NULL CONSTRAINT DF_tenants_created_at DEFAULT GETUTCDATE(),
    updated_at  DATETIME2 NOT NULL CONSTRAINT DF_tenants_updated_at DEFAULT GETUTCDATE(),
    is_deleted  BIT NOT NULL CONSTRAINT DF_tenants_is_deleted DEFAULT 0
);
GO

-- Unique slug among non-deleted tenants (soft-delete safe; frees slug on delete)
CREATE UNIQUE NONCLUSTERED INDEX IX_tenants_slug ON tenants (slug) WHERE is_deleted = 0;
GO

CREATE TABLE users (
    id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_users PRIMARY KEY,
    tenant_id       INT NOT NULL CONSTRAINT FK_users_tenants REFERENCES tenants(id),
    email           NVARCHAR(256) NOT NULL,
    password_hash   NVARCHAR(512) NOT NULL,
    full_name       NVARCHAR(200) NULL,
    role            NVARCHAR(50) NOT NULL,         -- TenantAdmin | Editor | Viewer
    is_active       BIT NOT NULL CONSTRAINT DF_users_is_active DEFAULT 1,
    created_at      DATETIME2 NOT NULL CONSTRAINT DF_users_created_at DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL CONSTRAINT DF_users_updated_at DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL CONSTRAINT DF_users_is_deleted DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_users_tenant_id ON users (tenant_id) WHERE is_deleted = 0;
GO

-- Unique email per tenant among non-deleted users
CREATE UNIQUE NONCLUSTERED INDEX IX_users_tenant_email ON users (tenant_id, email) WHERE is_deleted = 0;
GO

-- ============================================================
-- SYSTEM ADMINS  (root table — no tenant_id; migration 011)
-- ============================================================

CREATE TABLE system_admins (
    id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_system_admins PRIMARY KEY,
    email         NVARCHAR(256) NOT NULL,
    password_hash NVARCHAR(512) NOT NULL,         -- BCrypt hash
    full_name     NVARCHAR(200) NULL,
    is_active     BIT NOT NULL CONSTRAINT DF_system_admins_is_active DEFAULT 1,
    created_at    DATETIME2 NOT NULL CONSTRAINT DF_system_admins_created_at DEFAULT GETUTCDATE(),
    updated_at    DATETIME2 NOT NULL CONSTRAINT DF_system_admins_updated_at DEFAULT GETUTCDATE(),
    is_deleted    BIT NOT NULL CONSTRAINT DF_system_admins_is_deleted DEFAULT 0
);
GO

-- Unique email among non-deleted system admins
CREATE UNIQUE NONCLUSTERED INDEX IX_system_admins_email ON system_admins (email) WHERE is_deleted = 0;
GO

-- ============================================================
-- CONTENT TYPES & FIELDS
-- (parent_id self-ref added in migration 039 — included here for fresh installs)
-- ============================================================

CREATE TABLE content_types (
    id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_content_types PRIMARY KEY,
    tenant_id       INT NOT NULL CONSTRAINT FK_content_types_tenants REFERENCES tenants(id),
    name            NVARCHAR(200) NOT NULL,
    slug            NVARCHAR(200) NOT NULL,        -- used in URL + API
    description     NVARCHAR(1000) NULL,
    icon            NVARCHAR(100) NULL,            -- icon class e.g. "bi-file-text"
    is_published    BIT NOT NULL CONSTRAINT DF_content_types_is_published DEFAULT 0,
    sort_order      INT NOT NULL CONSTRAINT DF_content_types_sort_order DEFAULT 0,   -- menu order
    parent_id       INT NULL CONSTRAINT FK_content_types_parent REFERENCES content_types(id),  -- migration 039
    created_at      DATETIME2 NOT NULL CONSTRAINT DF_content_types_created_at DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL CONSTRAINT DF_content_types_updated_at DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL CONSTRAINT DF_content_types_is_deleted DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_content_types_tenant_id ON content_types (tenant_id) WHERE is_deleted = 0;
GO

-- Unique slug per tenant among non-deleted rows (soft-delete safe)
CREATE UNIQUE NONCLUSTERED INDEX IX_content_types_slug ON content_types (tenant_id, slug) WHERE is_deleted = 0;
GO

CREATE TABLE content_fields (
    id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_content_fields PRIMARY KEY,
    tenant_id       INT NOT NULL CONSTRAINT FK_content_fields_tenants REFERENCES tenants(id),
    content_type_id INT NOT NULL CONSTRAINT FK_content_fields_content_types REFERENCES content_types(id),
    name            NVARCHAR(200) NOT NULL,
    slug            NVARCHAR(200) NOT NULL,
    field_type      NVARCHAR(50) NOT NULL,         -- see FieldType enum
    is_required     BIT NOT NULL CONSTRAINT DF_content_fields_is_required DEFAULT 0,
    is_localized    BIT NOT NULL CONSTRAINT DF_content_fields_is_localized DEFAULT 1,
    sort_order      INT NOT NULL CONSTRAINT DF_content_fields_sort_order DEFAULT 0,
    options_json    NVARCHAR(MAX) NULL,            -- JSON: validation rules, choices, relation config
    created_at      DATETIME2 NOT NULL CONSTRAINT DF_content_fields_created_at DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL CONSTRAINT DF_content_fields_updated_at DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL CONSTRAINT DF_content_fields_is_deleted DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_content_fields_tenant_id ON content_fields (tenant_id) WHERE is_deleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_content_fields_content_type_id ON content_fields (content_type_id);
GO

-- Unique slug per content type among non-deleted rows
CREATE UNIQUE NONCLUSTERED INDEX IX_content_fields_slug ON content_fields (content_type_id, slug) WHERE is_deleted = 0;
GO

-- junction: no soft-delete / timestamps per schema
CREATE TABLE user_content_type_permissions (
    id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_user_content_type_permissions PRIMARY KEY,
    tenant_id       INT NOT NULL CONSTRAINT FK_uctp_tenants REFERENCES tenants(id),
    user_id         INT NOT NULL CONSTRAINT FK_uctp_users REFERENCES users(id),
    content_type_id INT NOT NULL CONSTRAINT FK_uctp_content_types REFERENCES content_types(id),
    can_read        BIT NOT NULL CONSTRAINT DF_uctp_can_read DEFAULT 1,
    can_create      BIT NOT NULL CONSTRAINT DF_uctp_can_create DEFAULT 0,
    can_update      BIT NOT NULL CONSTRAINT DF_uctp_can_update DEFAULT 0,
    can_delete      BIT NOT NULL CONSTRAINT DF_uctp_can_delete DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_uctp_tenant_id ON user_content_type_permissions (tenant_id);
GO

CREATE NONCLUSTERED INDEX IX_uctp_content_type_id ON user_content_type_permissions (content_type_id);
GO

-- One permission row per (user, content_type)
CREATE UNIQUE NONCLUSTERED INDEX IX_uctp_user_content_type ON user_content_type_permissions (user_id, content_type_id);
GO

-- ============================================================
-- CONTENT ITEMS & VALUES
-- ============================================================

CREATE TABLE content_items (
    id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_content_items PRIMARY KEY,
    tenant_id       INT NOT NULL CONSTRAINT FK_content_items_tenants REFERENCES tenants(id),
    content_type_id INT NOT NULL CONSTRAINT FK_content_items_content_types REFERENCES content_types(id),
    slug            NVARCHAR(500) NULL,            -- auto-generated from title field if present
    status          NVARCHAR(50) NOT NULL CONSTRAINT DF_content_items_status DEFAULT 'draft',  -- draft | published | archived
    created_by      INT NULL CONSTRAINT FK_content_items_created_by REFERENCES users(id),
    updated_by      INT NULL CONSTRAINT FK_content_items_updated_by REFERENCES users(id),
    published_at    DATETIME2 NULL,
    created_at      DATETIME2 NOT NULL CONSTRAINT DF_content_items_created_at DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL CONSTRAINT DF_content_items_updated_at DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL CONSTRAINT DF_content_items_is_deleted DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_content_items_tenant_id ON content_items (tenant_id) WHERE is_deleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_content_items_content_type_id ON content_items (content_type_id);
GO

-- Slug lookup for public API (GET /{content-type}/slug/{slug})
CREATE NONCLUSTERED INDEX IX_content_items_slug ON content_items (tenant_id, content_type_id, slug) WHERE is_deleted = 0;
GO

-- EAV: actual data; no soft-delete per schema
-- Note: value_media_id FK -> media_assets added after media_assets is created (see below)
CREATE TABLE content_field_values (
    id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_content_field_values PRIMARY KEY,
    tenant_id        INT NOT NULL CONSTRAINT FK_cfv_tenants REFERENCES tenants(id),
    content_item_id  INT NOT NULL CONSTRAINT FK_cfv_content_items REFERENCES content_items(id),
    content_field_id INT NOT NULL CONSTRAINT FK_cfv_content_fields REFERENCES content_fields(id),
    language_code    CHAR(5) NOT NULL CONSTRAINT DF_cfv_language_code DEFAULT 'tr',   -- 'tr', 'en', 'de' ...
    value_text       NVARCHAR(MAX) NULL,    -- Text, RichText, Markdown, Email, URL, JSON etc.
    value_number     DECIMAL(18,6) NULL,    -- Number, Decimal, Rating
    value_bool       BIT NULL,              -- Boolean
    value_date       DATETIME2 NULL,        -- Date, DateTime, Time
    value_date_end   DATETIME2 NULL,        -- DateRange end
    value_media_id   INT NULL,              -- Image, Video, Audio, File (FK -> media_assets added below)
    created_at       DATETIME2 NOT NULL CONSTRAINT DF_cfv_created_at DEFAULT GETUTCDATE(),
    updated_at       DATETIME2 NOT NULL CONSTRAINT DF_cfv_updated_at DEFAULT GETUTCDATE()
);
GO

-- One value per (item, field, language)
CREATE UNIQUE NONCLUSTERED INDEX IX_cfv_item_field_lang ON content_field_values (content_item_id, content_field_id, language_code);
GO

CREATE NONCLUSTERED INDEX IX_cfv_tenant_id ON content_field_values (tenant_id);
GO

CREATE NONCLUSTERED INDEX IX_cfv_content_field_id ON content_field_values (content_field_id);
GO

-- Relation / MultiRelation links; no soft-delete per schema
CREATE TABLE content_field_relations (
    id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_content_field_relations PRIMARY KEY,
    tenant_id       INT NOT NULL CONSTRAINT FK_cfr_tenants REFERENCES tenants(id),
    source_item_id  INT NOT NULL CONSTRAINT FK_cfr_source_item REFERENCES content_items(id),
    source_field_id INT NOT NULL CONSTRAINT FK_cfr_source_field REFERENCES content_fields(id),
    target_item_id  INT NOT NULL CONSTRAINT FK_cfr_target_item REFERENCES content_items(id),
    sort_order      INT NOT NULL CONSTRAINT DF_cfr_sort_order DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_cfr_tenant_id ON content_field_relations (tenant_id);
GO

-- Lookup relations by source (item + field) — the common read path
CREATE NONCLUSTERED INDEX IX_cfr_source ON content_field_relations (source_item_id, source_field_id);
GO

CREATE NONCLUSTERED INDEX IX_cfr_target_item_id ON content_field_relations (target_item_id);
GO

-- ============================================================
-- MEDIA
-- ============================================================

CREATE TABLE media_assets (
    id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_media_assets PRIMARY KEY,
    tenant_id       INT NOT NULL CONSTRAINT FK_media_assets_tenants REFERENCES tenants(id),
    file_name       NVARCHAR(500) NOT NULL,
    original_name   NVARCHAR(500) NOT NULL,
    file_path       NVARCHAR(1000) NOT NULL,
    media_type      NVARCHAR(50) NOT NULL,         -- image | video | audio | file
    mime_type       NVARCHAR(100) NULL,
    file_size_bytes BIGINT NULL,
    width           INT NULL,                       -- for images/video
    height          INT NULL,
    duration_secs   DECIMAL(10,2) NULL,            -- for video/audio
    alt_text        NVARCHAR(500) NULL,
    metadata_json   NVARCHAR(MAX) NULL,            -- EXIF, crop info etc.
    uploaded_by     INT NULL CONSTRAINT FK_media_assets_uploaded_by REFERENCES users(id),
    created_at      DATETIME2 NOT NULL CONSTRAINT DF_media_assets_created_at DEFAULT GETUTCDATE(),
    updated_at      DATETIME2 NOT NULL CONSTRAINT DF_media_assets_updated_at DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL CONSTRAINT DF_media_assets_is_deleted DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_media_assets_tenant_id ON media_assets (tenant_id) WHERE is_deleted = 0;
GO

-- Filter by media type within a tenant (media library tabs)
CREATE NONCLUSTERED INDEX IX_media_assets_media_type ON media_assets (tenant_id, media_type) WHERE is_deleted = 0;
GO

-- Deferred FK: content_field_values.value_media_id -> media_assets(id)
-- (media_assets did not exist when content_field_values was created)
ALTER TABLE content_field_values
    ADD CONSTRAINT FK_cfv_media FOREIGN KEY (value_media_id) REFERENCES media_assets(id);
GO

CREATE NONCLUSTERED INDEX IX_cfv_value_media_id ON content_field_values (value_media_id) WHERE value_media_id IS NOT NULL;
GO

-- ============================================================
-- TENANT LOCALIZATION (LANGUAGES & DICTIONARY)
-- ============================================================

-- per-tenant; no soft-delete / timestamps per schema
CREATE TABLE languages (
    id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_languages PRIMARY KEY,
    tenant_id   INT NOT NULL CONSTRAINT FK_languages_tenants REFERENCES tenants(id),
    code        CHAR(5) NOT NULL,              -- 'tr', 'en', 'de'
    name        NVARCHAR(100) NOT NULL,        -- 'Türkçe', 'English'
    is_default  BIT NOT NULL CONSTRAINT DF_languages_is_default DEFAULT 0,
    is_active   BIT NOT NULL CONSTRAINT DF_languages_is_active DEFAULT 1,
    flag_icon   NVARCHAR(100) NULL
);
GO

CREATE NONCLUSTERED INDEX IX_languages_tenant_id ON languages (tenant_id);
GO

-- One language code per tenant
CREATE UNIQUE NONCLUSTERED INDEX IX_languages_tenant_code ON languages (tenant_id, code);
GO

CREATE TABLE dictionary_entries (
    id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_dictionary_entries PRIMARY KEY,
    tenant_id   INT NOT NULL CONSTRAINT FK_dictionary_entries_tenants REFERENCES tenants(id),
    key_name    NVARCHAR(300) NOT NULL,        -- e.g. 'nav.home', 'btn.save'
    category    NVARCHAR(100) NULL,            -- 'navigation', 'buttons', 'errors'
    created_at  DATETIME2 NOT NULL CONSTRAINT DF_dictionary_entries_created_at DEFAULT GETUTCDATE(),
    updated_at  DATETIME2 NOT NULL CONSTRAINT DF_dictionary_entries_updated_at DEFAULT GETUTCDATE(),
    is_deleted  BIT NOT NULL CONSTRAINT DF_dictionary_entries_is_deleted DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_dictionary_entries_tenant_id ON dictionary_entries (tenant_id) WHERE is_deleted = 0;
GO

-- Unique key per tenant among non-deleted entries
CREATE UNIQUE NONCLUSTERED INDEX IX_dictionary_entries_key ON dictionary_entries (tenant_id, key_name) WHERE is_deleted = 0;
GO

-- values; no tenant_id / soft-delete per schema (resolved via entry_id)
CREATE TABLE dictionary_translations (
    id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_dictionary_translations PRIMARY KEY,
    entry_id      INT NOT NULL CONSTRAINT FK_dict_trans_entries REFERENCES dictionary_entries(id),
    language_code CHAR(5) NOT NULL,
    value         NVARCHAR(MAX) NOT NULL
);
GO

-- One translation per (entry, language)
CREATE UNIQUE NONCLUSTERED INDEX IX_dict_trans_entry_lang ON dictionary_translations (entry_id, language_code);
GO

-- ============================================================
-- API MANAGEMENT
-- Columns from later migrations included for fresh installs:
--   is_public    (migration 027)
--   allow_read / allow_create / allow_update / allow_delete (migration 034)
-- ============================================================

-- no soft-delete per schema
CREATE TABLE api_configurations (
    id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_api_configurations PRIMARY KEY,
    tenant_id          INT NOT NULL CONSTRAINT FK_api_configs_tenants REFERENCES tenants(id),
    content_type_id    INT NOT NULL CONSTRAINT FK_api_configs_content_types REFERENCES content_types(id),
    is_enabled         BIT NOT NULL CONSTRAINT DF_api_configs_is_enabled DEFAULT 0,
    auth_type          NVARCHAR(50) NOT NULL CONSTRAINT DF_api_configs_auth_type DEFAULT 'None',  -- None | ApiKey | JWT
    api_key            NVARCHAR(500) NULL,
    allow_filtering    BIT NOT NULL CONSTRAINT DF_api_configs_allow_filtering DEFAULT 1,
    allow_sorting      BIT NOT NULL CONSTRAINT DF_api_configs_allow_sorting DEFAULT 1,
    allow_pagination   BIT NOT NULL CONSTRAINT DF_api_configs_allow_pagination DEFAULT 1,
    rate_limit_per_min INT NULL CONSTRAINT DF_api_configs_rate_limit DEFAULT 60,
    cache_seconds      INT NULL CONSTRAINT DF_api_configs_cache_seconds DEFAULT 0,
    is_public          BIT NOT NULL CONSTRAINT DF_api_configs_is_public DEFAULT 0,      -- migration 027
    allow_read         BIT NOT NULL CONSTRAINT DF_api_configs_allow_read DEFAULT 1,     -- migration 034
    allow_create       BIT NOT NULL CONSTRAINT DF_api_configs_allow_create DEFAULT 0,   -- migration 034
    allow_update       BIT NOT NULL CONSTRAINT DF_api_configs_allow_update DEFAULT 0,   -- migration 034
    allow_delete       BIT NOT NULL CONSTRAINT DF_api_configs_allow_delete DEFAULT 0,   -- migration 034
    created_at         DATETIME2 NOT NULL CONSTRAINT DF_api_configs_created_at DEFAULT GETUTCDATE(),
    updated_at         DATETIME2 NOT NULL CONSTRAINT DF_api_configs_updated_at DEFAULT GETUTCDATE()
);
GO

CREATE NONCLUSTERED INDEX IX_api_configs_tenant_id ON api_configurations (tenant_id);
GO

-- One API config per (tenant, content type)
CREATE UNIQUE NONCLUSTERED INDEX IX_api_configs_tenant_content_type
    ON api_configurations (tenant_id, content_type_id);
GO

-- no tenant_id / soft-delete per schema
CREATE TABLE api_field_visibility (
    id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_api_field_visibility PRIMARY KEY,
    api_configuration_id INT NOT NULL CONSTRAINT FK_afv_api_configs REFERENCES api_configurations(id),
    content_field_id     INT NOT NULL CONSTRAINT FK_afv_content_fields REFERENCES content_fields(id),
    is_visible           BIT NOT NULL CONSTRAINT DF_afv_is_visible DEFAULT 1,
    response_key_alias   NVARCHAR(200) NULL    -- rename field in JSON response
);
GO

-- One visibility row per (config, field)
CREATE UNIQUE NONCLUSTERED INDEX IX_afv_config_field
    ON api_field_visibility (api_configuration_id, content_field_id);
GO

CREATE NONCLUSTERED INDEX IX_afv_content_field_id ON api_field_visibility (content_field_id);
GO

-- Shared named credentials (migration 027)
CREATE TABLE api_credentials (
    id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_api_credentials PRIMARY KEY,
    tenant_id   INT NOT NULL CONSTRAINT FK_api_cred_tenants REFERENCES tenants(id),
    name        NVARCHAR(200) NOT NULL,
    auth_type   NVARCHAR(50)  NOT NULL,           -- ApiKey | JWT
    api_key     NVARCHAR(500) NULL,               -- BCrypt hash (ApiKey only); NULL for JWT (stateless)
    is_active   BIT NOT NULL CONSTRAINT DF_api_cred_is_active DEFAULT 1,
    created_at  DATETIME2 NOT NULL CONSTRAINT DF_api_cred_created_at DEFAULT GETUTCDATE(),
    updated_at  DATETIME2 NOT NULL CONSTRAINT DF_api_cred_updated_at DEFAULT GETUTCDATE(),
    is_deleted  BIT NOT NULL CONSTRAINT DF_api_cred_is_deleted DEFAULT 0
);
GO

CREATE NONCLUSTERED INDEX IX_api_cred_tenant_id ON api_credentials (tenant_id) WHERE is_deleted = 0;
GO

-- Many-to-many: which content types a credential covers (migration 027)
CREATE TABLE api_credential_content_types (
    id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_api_cred_ct PRIMARY KEY,
    tenant_id         INT NOT NULL CONSTRAINT FK_api_cred_ct_tenants REFERENCES tenants(id),
    api_credential_id INT NOT NULL CONSTRAINT FK_api_cred_ct_cred REFERENCES api_credentials(id),
    content_type_id   INT NOT NULL CONSTRAINT FK_api_cred_ct_ct REFERENCES content_types(id)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_api_cred_ct_unique
    ON api_credential_content_types (api_credential_id, content_type_id);
GO

CREATE NONCLUSTERED INDEX IX_api_cred_ct_tenant_id ON api_credential_content_types (tenant_id);
GO

-- ============================================================
-- GLOBAL UI LOCALIZATION  (root tables — no tenant_id; migration 013)
-- ============================================================

CREATE TABLE ui_cultures (
    id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ui_cultures PRIMARY KEY,
    code        NVARCHAR(10) NOT NULL,
    name        NVARCHAR(100) NOT NULL,
    is_default  BIT NOT NULL CONSTRAINT DF_ui_cultures_is_default DEFAULT 0,
    is_active   BIT NOT NULL CONSTRAINT DF_ui_cultures_is_active DEFAULT 1
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_ui_cultures_code ON ui_cultures (code);
GO

CREATE TABLE ui_translations (
    id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ui_translations PRIMARY KEY,
    culture      NVARCHAR(10) NOT NULL,
    resource_key NVARCHAR(300) NOT NULL,
    value        NVARCHAR(MAX) NOT NULL
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_ui_translations_culture_key
    ON ui_translations (culture, resource_key);
GO

-- ============================================================
-- CONTENT ITEM TITLES  (migration 022 + is_active from 024)
-- ============================================================

-- Language-specific display titles; falls back to slug when absent.
-- is_active = 0 means this language version is not yet live.
CREATE TABLE content_item_titles (
    id               INT           IDENTITY(1,1) NOT NULL,
    tenant_id        INT           NOT NULL,
    content_item_id  INT           NOT NULL,
    language_code    CHAR(5)       NOT NULL DEFAULT 'tr',
    title            NVARCHAR(500) NOT NULL,
    is_active        BIT           NOT NULL DEFAULT 0,    -- migration 024; app sets to 1 explicitly
    created_at       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    updated_at       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_content_item_titles PRIMARY KEY (id),
    CONSTRAINT FK_cit_content_items FOREIGN KEY (content_item_id) REFERENCES content_items(id),
    CONSTRAINT FK_cit_tenants FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_cit_item_lang ON content_item_titles (content_item_id, language_code);
GO

CREATE NONCLUSTERED INDEX IX_cit_tenant_id ON content_item_titles (tenant_id);
GO

-- ============================================================
-- AUDIT & DIAGNOSTIC LOGS
-- ============================================================

-- Tenant-scoped business event log (migration 030); rows are immutable (no is_deleted)
CREATE TABLE audit_logs (
    id               BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_audit_logs PRIMARY KEY,
    tenant_id        INT           NOT NULL CONSTRAINT FK_audit_logs_tenants REFERENCES tenants(id),
    user_id          INT           NULL,
    user_email       NVARCHAR(256) NULL,
    user_role        NVARCHAR(50)  NULL,
    action           NVARCHAR(100) NOT NULL,
    entity_type      NVARCHAR(100) NULL,
    entity_id        INT           NULL,
    content_type_id  INT           NULL,
    entity_name      NVARCHAR(500) NULL,
    description      NVARCHAR(MAX) NULL,
    metadata_json    NVARCHAR(MAX) NULL,
    ip_address       NVARCHAR(64)  NULL,
    created_at       DATETIME2     NOT NULL CONSTRAINT DF_audit_logs_created_at DEFAULT GETUTCDATE()
);
GO

CREATE NONCLUSTERED INDEX IX_audit_tenant_created ON audit_logs (tenant_id, created_at DESC);
GO

CREATE NONCLUSTERED INDEX IX_audit_tenant_ct_created ON audit_logs (tenant_id, content_type_id, created_at DESC)
    WHERE content_type_id IS NOT NULL;
GO

-- Serilog MSSqlServer diagnostic sink (migration 031)
-- Standard Serilog columns + custom enrichment: TenantId, UserId, Role, RequestPath
CREATE TABLE logs (
    id               BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_logs PRIMARY KEY,
    message          NVARCHAR(MAX) NULL,
    message_template NVARCHAR(MAX) NULL,
    level            NVARCHAR(50)  NULL,
    time_stamp       DATETIME2     NULL,
    exception        NVARCHAR(MAX) NULL,
    properties       NVARCHAR(MAX) NULL,
    -- custom enrichment columns
    tenant_id        INT           NULL,
    user_id          INT           NULL,
    role             NVARCHAR(50)  NULL,
    request_path     NVARCHAR(500) NULL
);
GO

CREATE NONCLUSTERED INDEX IX_logs_level_time ON logs (level, time_stamp DESC);
GO

CREATE NONCLUSTERED INDEX IX_logs_tenant_time ON logs (tenant_id, time_stamp DESC)
    WHERE tenant_id IS NOT NULL;
GO

-- ============================================================
-- EMAIL VERIFICATION  (migration 036)
-- ============================================================

CREATE TABLE login_codes (
    id          BIGINT        IDENTITY(1,1) PRIMARY KEY,
    email       NVARCHAR(256) NOT NULL,
    tenant_type NVARCHAR(20)  NOT NULL,  -- 'tenant' | 'system'
    tenant_id   INT           NULL,       -- NULL for system admins
    code        NVARCHAR(10)  NOT NULL,
    attempts    INT           NOT NULL DEFAULT 0,
    expires_at  DATETIME2     NOT NULL,
    used_at     DATETIME2     NULL,
    created_at  DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE INDEX IX_login_codes_lookup
    ON login_codes (email, tenant_type, tenant_id)
    WHERE used_at IS NULL;
GO

-- ============================================================
-- TENANT EMAIL SETTINGS  (migration 038)
-- ============================================================

CREATE TABLE tenant_email_settings (
    tenant_id                   INT NOT NULL PRIMARY KEY REFERENCES tenants(id),
    email_verification_enabled  BIT NOT NULL DEFAULT 0,
    smtp_host                   NVARCHAR(200) NOT NULL DEFAULT '',
    smtp_port                   INT NOT NULL DEFAULT 587,
    smtp_use_ssl                BIT NOT NULL DEFAULT 0,
    smtp_user                   NVARCHAR(200) NOT NULL DEFAULT '',
    smtp_password               NVARCHAR(500) NOT NULL DEFAULT '',
    from_address                NVARCHAR(200) NOT NULL DEFAULT '',
    from_name                   NVARCHAR(200) NOT NULL DEFAULT 'Varyo CMS',
    code_expiry_minutes         INT NOT NULL DEFAULT 5,
    max_attempts                INT NOT NULL DEFAULT 3,
    updated_at                  DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO
