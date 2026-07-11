-- ================================================================
-- 003_v1_demo_seed.sql
-- DEV SEED ONLY -- do NOT run against production. Change all seeded
-- passwords/API keys after first login.
--
-- Worked example: a 'dev-tenant' with its own languages, a
-- TenantAdmin user, and a small example content structure (Category /
-- Author / Blog Post content types with sample published content) —
-- showing how content types, fields, localization, and the public API
-- fit together. Skip this file if you only want a clean system with no
-- example tenant (see 002_v1_system_seed.sql).
--
-- Run AFTER 001_v1_schema.sql and 002_v1_system_seed.sql (the demo API
-- configuration/credential and dictionary entries created here don't
-- strictly need Section 2's globals, but SystemAdmin/UI translations
-- should exist first for a coherent fresh install).
--
-- Seeded:
--   TenantAdmin : admin@dev.local / Admin123! (dev-tenant)
--   Languages   : tr (default), en
--   Demo content: Category (3) · Author (2) · Blog Post (3) — tr + en
--   Demo API    : Blog Post enabled (ApiKey auth, read-only)
--                 Key (plaintext): vk_1_demo123secret
--   Dictionary  : nav.home, nav.blog, btn.read-more (tr + en)
-- ================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- 008: dev-tenant + default 'tr' language
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM tenants WHERE slug = 'dev-tenant')
BEGIN
    INSERT INTO tenants (name, slug, is_active) VALUES (N'Dev Tenant', 'dev-tenant', 1);
    PRINT 'Seeded tenant: dev-tenant';
END
ELSE
    PRINT 'Tenant already seeded: dev-tenant';
GO

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');

IF @devTenantId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM languages WHERE tenant_id = @devTenantId AND code = 'tr')
BEGIN
    INSERT INTO languages (tenant_id, code, name, is_default, is_active)
    VALUES (@devTenantId, 'tr', N'Türkçe', 1, 1);
    PRINT 'Seeded language: tr (default) for dev-tenant';
END
ELSE
    PRINT 'Language tr already seeded or tenant missing';
GO

-- ============================================================
-- 009: 'en' language for dev-tenant (multi-language testing)
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');

IF @devTenantId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM languages WHERE tenant_id = @devTenantId AND code = 'en')
BEGIN
    INSERT INTO languages (tenant_id, code, name, is_default, is_active)
    VALUES (@devTenantId, 'en', N'English', 0, 1);
    PRINT 'Seeded language: en for dev-tenant';
END
ELSE
    PRINT 'Language en already seeded or tenant missing';
GO

-- ============================================================
-- 010: TenantAdmin user for dev-tenant
--   Email:    admin@dev.local
--   Password: Admin123!
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');

IF @devTenantId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM users WHERE tenant_id = @devTenantId AND email = 'admin@dev.local')
BEGIN
    INSERT INTO users (tenant_id, email, password_hash, full_name, role, is_active)
    VALUES (@devTenantId, 'admin@dev.local',
            '$2a$12$4lCE3hHfiyShtwR7bC9CG.YVgp5pny2Qx.cSxGzCJkqvXw89r25EW',
            N'Dev Admin', 'TenantAdmin', 1);
    PRINT 'Seeded dev admin: admin@dev.local / Admin123! (TenantAdmin)';
END
ELSE
    PRINT 'Dev admin already seeded or tenant missing.';
GO

-- ============================================================
-- Demo Tech Blog — Content Types
--   Category, Author, Blog Post (all published, multi-language)
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');

IF NOT EXISTS (SELECT 1 FROM content_types WHERE tenant_id = @devTenantId AND slug = 'category')
    INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order)
    VALUES (@devTenantId, N'Category', 'category', N'Blog post categories', 'bi-tag', 1, 0);

IF NOT EXISTS (SELECT 1 FROM content_types WHERE tenant_id = @devTenantId AND slug = 'author')
    INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order)
    VALUES (@devTenantId, N'Author', 'author', N'Content authors', 'bi-person', 1, 1);

IF NOT EXISTS (SELECT 1 FROM content_types WHERE tenant_id = @devTenantId AND slug = 'blog-post')
    INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order)
    VALUES (@devTenantId, N'Blog Post', 'blog-post', N'News and blog articles', 'bi-file-text', 1, 2);
GO

-- ============================================================
-- Demo Tech Blog — Content Fields
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');
DECLARE @catId   INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'category');
DECLARE @authorId INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'author');
DECLARE @blogId  INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'blog-post');

-- Category fields
IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @catId AND slug = 'name')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @catId, N'Name', 'name', 'Text', 1, 1, 0);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @catId AND slug = 'slug')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @catId, N'Slug', 'slug', 'Slug', 0, 0, 1);

-- Author fields
IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @authorId AND slug = 'full-name')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @authorId, N'Full Name', 'full-name', 'Text', 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @authorId AND slug = 'bio')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @authorId, N'Biography', 'bio', 'RichText', 0, 1, 1);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @authorId AND slug = 'slug')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @authorId, N'Slug', 'slug', 'Slug', 0, 0, 2);

-- Blog Post fields
IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'title')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @blogId, N'Title', 'title', 'Text', 1, 1, 0);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'slug')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @blogId, N'Slug', 'slug', 'Slug', 0, 0, 1);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'summary')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @blogId, N'Summary', 'summary', 'Text', 0, 1, 2);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'body')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json)
    VALUES (@devTenantId, @blogId, N'Body', 'body', 'RichText', 0, 1, 3, NULL);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'category')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json)
    VALUES (@devTenantId, @blogId, N'Category', 'category', 'Relation', 0, 0, 4,
        '{"target_content_type_id":' + CAST(@catId AS NVARCHAR) + ',"display_field_slug":"name","value_field_slug":"id","allow_multiple":false}');

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'authors')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json)
    VALUES (@devTenantId, @blogId, N'Authors', 'authors', 'MultiRelation', 0, 0, 5,
        '{"target_content_type_id":' + CAST(@authorId AS NVARCHAR) + ',"display_field_slug":"full-name","value_field_slug":"id","allow_multiple":true,"min_items":0,"max_items":5}');

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'published-at')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @blogId, N'Published At', 'published-at', 'DateTime', 0, 0, 6);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'featured')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @blogId, N'Featured', 'featured', 'Boolean', 0, 0, 7);

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'read-time')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order,
        options_json)
    VALUES (@devTenantId, @blogId, N'Read Time (min)', 'read-time', 'Number', 0, 0, 8,
        '{"min":1,"max":120}');

IF NOT EXISTS (SELECT 1 FROM content_fields WHERE content_type_id = @blogId AND slug = 'tags')
    INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order)
    VALUES (@devTenantId, @blogId, N'Tags', 'tags', 'Tags', 0, 0, 9);
GO

-- ============================================================
-- Demo Tech Blog — Category Items
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');
DECLARE @catId INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'category');
DECLARE @catNameFieldId INT = (SELECT id FROM content_fields WHERE content_type_id = @catId AND slug = 'name');

-- Insert category items
IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'technology')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @catId, 'technology', 'published', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'design')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @catId, 'design', 'published', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'development')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @catId, 'development', 'published', GETUTCDATE());

-- Category name field values (is_localized=1 → store per language)
DECLARE @cat1Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'technology');
DECLARE @cat2Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'design');
DECLARE @cat3Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'development');

MERGE content_field_values AS t
USING (VALUES
    (@cat1Id, @catNameFieldId, 'tr', N'Teknoloji'),
    (@cat1Id, @catNameFieldId, 'en', N'Technology'),
    (@cat2Id, @catNameFieldId, 'tr', N'Tasarım'),
    (@cat2Id, @catNameFieldId, 'en', N'Design'),
    (@cat3Id, @catNameFieldId, 'tr', N'Geliştirme'),
    (@cat3Id, @catNameFieldId, 'en', N'Development')
) AS s (content_item_id, content_field_id, language_code, value_text)
ON t.content_item_id = s.content_item_id
   AND t.content_field_id = s.content_field_id
   AND t.language_code = s.language_code
WHEN MATCHED THEN UPDATE SET t.value_text = s.value_text
WHEN NOT MATCHED THEN INSERT (tenant_id, content_item_id, content_field_id, language_code, value_text)
    VALUES (@devTenantId, s.content_item_id, s.content_field_id, s.language_code, s.value_text);
GO

-- ============================================================
-- Demo Tech Blog — Author Items
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');
DECLARE @authorId INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'author');
DECLARE @authorNameFieldId INT = (SELECT id FROM content_fields WHERE content_type_id = @authorId AND slug = 'full-name');
DECLARE @authorBioFieldId  INT = (SELECT id FROM content_fields WHERE content_type_id = @authorId AND slug = 'bio');

IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'alice-johnson')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @authorId, 'alice-johnson', 'published', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'bob-martinez')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @authorId, 'bob-martinez', 'published', GETUTCDATE());

DECLARE @alice INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'alice-johnson');
DECLARE @bob   INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'bob-martinez');

-- full-name (is_localized=0 → lang='all')
MERGE content_field_values AS t
USING (VALUES
    (@alice, @authorNameFieldId, 'all', N'Alice Johnson'),
    (@bob,   @authorNameFieldId, 'all', N'Bob Martinez')
) AS s (content_item_id, content_field_id, language_code, value_text)
ON t.content_item_id = s.content_item_id AND t.content_field_id = s.content_field_id AND t.language_code = s.language_code
WHEN MATCHED THEN UPDATE SET t.value_text = s.value_text
WHEN NOT MATCHED THEN INSERT (tenant_id, content_item_id, content_field_id, language_code, value_text)
    VALUES (@devTenantId, s.content_item_id, s.content_field_id, s.language_code, s.value_text);

-- bio (is_localized=1 → tr + en)
MERGE content_field_values AS t
USING (VALUES
    (@alice, @authorBioFieldId, 'tr', N'<p>Kıdemli içerik stratejisti ve teknik yazar. 10 yıldır teknoloji ekosisteminde içerik üretimi yapıyor.</p>'),
    (@alice, @authorBioFieldId, 'en', N'<p>Senior content strategist and technical writer with 10 years of experience in the tech ecosystem.</p>'),
    (@bob,   @authorBioFieldId, 'tr', N'<p>Full-stack geliştirici ve açık kaynak tutkunu. API tasarımı ve headless CMS mimarileri üzerine yazıyor.</p>'),
    (@bob,   @authorBioFieldId, 'en', N'<p>Full-stack developer and open-source enthusiast. Writes about API design and headless CMS architectures.</p>')
) AS s (content_item_id, content_field_id, language_code, value_text)
ON t.content_item_id = s.content_item_id AND t.content_field_id = s.content_field_id AND t.language_code = s.language_code
WHEN MATCHED THEN UPDATE SET t.value_text = s.value_text
WHEN NOT MATCHED THEN INSERT (tenant_id, content_item_id, content_field_id, language_code, value_text)
    VALUES (@devTenantId, s.content_item_id, s.content_field_id, s.language_code, s.value_text);
GO

-- ============================================================
-- Demo Tech Blog — Blog Post Items
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');
DECLARE @catId   INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'category');
DECLARE @authorId INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'author');
DECLARE @blogId  INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'blog-post');

-- Field IDs for blog post
DECLARE @fTitle       INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'title');
DECLARE @fSlug        INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'slug');
DECLARE @fSummary     INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'summary');
DECLARE @fBody        INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'body');
DECLARE @fCategory    INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'category');
DECLARE @fAuthors     INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'authors');
DECLARE @fPublished   INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'published-at');
DECLARE @fFeatured    INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'featured');
DECLARE @fReadTime    INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'read-time');
DECLARE @fTags        INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'tags');

-- Category item IDs
DECLARE @cat1Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'technology');
DECLARE @cat2Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'design');
DECLARE @cat3Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'development');

-- Author item IDs
DECLARE @alice INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'alice-johnson');
DECLARE @bob   INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'bob-martinez');

-- Insert blog posts
IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'getting-started-with-varyo-cms')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @blogId, 'getting-started-with-varyo-cms', 'published', '2026-01-10T09:00:00');

IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'mastering-content-types-in-varyo')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @blogId, 'mastering-content-types-in-varyo', 'published', '2026-01-20T10:00:00');

IF NOT EXISTS (SELECT 1 FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'building-headless-apps-with-varyo-api')
    INSERT INTO content_items (tenant_id, content_type_id, slug, status, published_at)
    VALUES (@devTenantId, @blogId, 'building-headless-apps-with-varyo-api', 'published', '2026-02-05T11:00:00');

DECLARE @post1 INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'getting-started-with-varyo-cms');
DECLARE @post2 INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'mastering-content-types-in-varyo');
DECLARE @post3 INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'building-headless-apps-with-varyo-api');

-- Blog Post: localized text fields (title, summary, body) — tr + en
MERGE content_field_values AS t
USING (VALUES
    -- Post 1: title
    (@post1, @fTitle, 'tr', N'Varyo CMS ile Başlarken', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post1, @fTitle, 'en', N'Getting Started with Varyo CMS', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 1: summary
    (@post1, @fSummary, 'tr', N'Varyo CMS kurulumu, ilk tenant oluşturma ve içerik tipi tanımlamayı adım adım öğrenin.', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post1, @fSummary, 'en', N'Learn how to set up Varyo CMS, create your first tenant, and define content types step by step.', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 1: body
    (@post1, @fBody, 'tr', N'<h2>Neden Varyo CMS?</h2><p>Varyo CMS, multi-tenant mimarisi ve headless API desteğiyle modern içerik yönetimini kolaylaştırır. Bu yazıda sıfırdan nasıl kurulacağını inceliyoruz.</p><h2>Kurulum</h2><p>Docker Compose ile dakikalar içinde çalışır hale getirin.</p>', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post1, @fBody, 'en', N'<h2>Why Varyo CMS?</h2><p>Varyo CMS simplifies modern content management with its multi-tenant architecture and headless API support. In this article we explore how to set it up from scratch.</p><h2>Installation</h2><p>Get it running in minutes with Docker Compose.</p>', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 2: title
    (@post2, @fTitle, 'tr', N'Varyo''da İçerik Tiplerinde Uzmanlaşma', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post2, @fTitle, 'en', N'Mastering Content Types in Varyo', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 2: summary
    (@post2, @fSummary, 'tr', N'30+ alan tipini keşfedin: Metin, Medya, İlişki ve daha fazlası. Güçlü içerik şemaları nasıl oluşturulur?', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post2, @fSummary, 'en', N'Explore 30+ field types: Text, Media, Relation and more. How to build powerful content schemas.', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 2: body
    (@post2, @fBody, 'tr', N'<h2>İçerik Tipi Nedir?</h2><p>İçerik tipleri, içeriklerinizin şemasını tanımlar. Blog Post, Ürün, Etkinlik gibi yapıları modelleyebilirsiniz.</p><h2>Alan Tipleri</h2><p>Text, RichText, Image, Relation, MultiRelation ve daha fazlası için yerleşik destek mevcuttur.</p>', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post2, @fBody, 'en', N'<h2>What Is a Content Type?</h2><p>Content types define the schema of your content. You can model structures like Blog Post, Product, or Event.</p><h2>Field Types</h2><p>Built-in support for Text, RichText, Image, Relation, MultiRelation and many more.</p>', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 3: title
    (@post3, @fTitle, 'tr', N'Varyo API ile Headless Uygulamalar Geliştirme', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post3, @fTitle, 'en', N'Building Headless Apps with Varyo API', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 3: summary
    (@post3, @fSummary, 'tr', N'REST API, ApiKey/JWT kimlik doğrulama ve camelCase alan isimlendirmesiyle frontend entegrasyonu nasıl yapılır?', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post3, @fSummary, 'en', N'How to integrate frontend apps using the REST API, ApiKey/JWT authentication and camelCase field naming.', NULL, NULL, NULL, NULL, NULL, NULL),
    -- Post 3: body
    (@post3, @fBody, 'tr', N'<h2>Headless CMS Avantajları</h2><p>İçerik ve sunum katmanını birbirinden ayırarak aynı içeriği web, mobil ve IoT''ye aynı anda sunabilirsiniz.</p><h2>Varyo Public API</h2><p>Her content type için ayrı endpoint, camelCase alan anahtarları ve ApiKey/JWT kimlik doğrulama desteği ile entegrasyon kolaylaşır.</p>', NULL, NULL, NULL, NULL, NULL, NULL),
    (@post3, @fBody, 'en', N'<h2>Headless CMS Advantages</h2><p>By decoupling content from presentation, you can deliver the same content to web, mobile and IoT simultaneously.</p><h2>Varyo Public API</h2><p>Separate endpoints per content type, camelCase field keys and ApiKey/JWT auth make integration straightforward.</p>', NULL, NULL, NULL, NULL, NULL, NULL)
) AS s (content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, value_date_end, value_media_id, v_unused)
ON t.content_item_id = s.content_item_id AND t.content_field_id = s.content_field_id AND t.language_code = s.language_code
WHEN MATCHED THEN UPDATE SET t.value_text = s.value_text
WHEN NOT MATCHED THEN INSERT (tenant_id, content_item_id, content_field_id, language_code, value_text)
    VALUES (@devTenantId, s.content_item_id, s.content_field_id, s.language_code, s.value_text);

-- Blog Post: non-localized fields (lang='all') — featured, read-time, tags, published-at
MERGE content_field_values AS t
USING (VALUES
    (@post1, @fFeatured, 'all', NULL, NULL, CAST(1 AS BIT), NULL, NULL, NULL),
    (@post1, @fReadTime, 'all', NULL, CAST(5 AS DECIMAL(18,6)), NULL, NULL, NULL, NULL),
    (@post1, @fTags,     'all', N'["cms","tutorial","getting-started"]', NULL, NULL, NULL, NULL, NULL),
    (@post1, @fPublished,'all', NULL, NULL, NULL, CAST('2026-01-10T09:00:00' AS DATETIME2), NULL, NULL),
    (@post2, @fFeatured, 'all', NULL, NULL, CAST(0 AS BIT), NULL, NULL, NULL),
    (@post2, @fReadTime, 'all', NULL, CAST(8 AS DECIMAL(18,6)), NULL, NULL, NULL, NULL),
    (@post2, @fTags,     'all', N'["content-types","schema","fields"]', NULL, NULL, NULL, NULL, NULL),
    (@post2, @fPublished,'all', NULL, NULL, NULL, CAST('2026-01-20T10:00:00' AS DATETIME2), NULL, NULL),
    (@post3, @fFeatured, 'all', NULL, NULL, CAST(1 AS BIT), NULL, NULL, NULL),
    (@post3, @fReadTime, 'all', NULL, CAST(12 AS DECIMAL(18,6)), NULL, NULL, NULL, NULL),
    (@post3, @fTags,     'all', N'["api","headless","integration","rest"]', NULL, NULL, NULL, NULL, NULL),
    (@post3, @fPublished,'all', NULL, NULL, NULL, CAST('2026-02-05T11:00:00' AS DATETIME2), NULL, NULL)
) AS s (content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, value_date_end, value_media_id)
ON t.content_item_id = s.content_item_id AND t.content_field_id = s.content_field_id AND t.language_code = s.language_code
WHEN MATCHED THEN UPDATE SET
    t.value_text   = s.value_text,
    t.value_number = s.value_number,
    t.value_bool   = s.value_bool,
    t.value_date   = s.value_date
WHEN NOT MATCHED THEN INSERT (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date)
    VALUES (@devTenantId, s.content_item_id, s.content_field_id, s.language_code, s.value_text, s.value_number, s.value_bool, s.value_date);

-- Relation: post → category (one per post, via content_field_relations)
IF NOT EXISTS (SELECT 1 FROM content_field_relations WHERE source_item_id = @post1 AND source_field_id = @fCategory AND target_item_id = @cat1Id)
    INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
    VALUES (@devTenantId, @post1, @fCategory, @cat1Id, 0);

IF NOT EXISTS (SELECT 1 FROM content_field_relations WHERE source_item_id = @post2 AND source_field_id = @fCategory AND target_item_id = @cat2Id)
    INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
    VALUES (@devTenantId, @post2, @fCategory, @cat2Id, 0);

IF NOT EXISTS (SELECT 1 FROM content_field_relations WHERE source_item_id = @post3 AND source_field_id = @fCategory AND target_item_id = @cat3Id)
    INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
    VALUES (@devTenantId, @post3, @fCategory, @cat3Id, 0);

-- MultiRelation: post → authors
IF NOT EXISTS (SELECT 1 FROM content_field_relations WHERE source_item_id = @post1 AND source_field_id = @fAuthors AND target_item_id = @alice)
    INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
    VALUES (@devTenantId, @post1, @fAuthors, @alice, 0);

IF NOT EXISTS (SELECT 1 FROM content_field_relations WHERE source_item_id = @post1 AND source_field_id = @fAuthors AND target_item_id = @bob)
    INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
    VALUES (@devTenantId, @post1, @fAuthors, @bob, 1);

IF NOT EXISTS (SELECT 1 FROM content_field_relations WHERE source_item_id = @post2 AND source_field_id = @fAuthors AND target_item_id = @alice)
    INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
    VALUES (@devTenantId, @post2, @fAuthors, @alice, 0);

IF NOT EXISTS (SELECT 1 FROM content_field_relations WHERE source_item_id = @post3 AND source_field_id = @fAuthors AND target_item_id = @bob)
    INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
    VALUES (@devTenantId, @post3, @fAuthors, @bob, 0);
GO

-- ============================================================
-- Demo Tech Blog — content_item_titles
--   Required by PublicApiRepository.GetItemsAsync (EXISTS filter).
--   is_active = 1 means this language version is live.
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');
DECLARE @catId   INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'category');
DECLARE @authorId INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'author');
DECLARE @blogId  INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'blog-post');

DECLARE @cat1Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'technology');
DECLARE @cat2Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'design');
DECLARE @cat3Id INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @catId AND slug = 'development');
DECLARE @alice  INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'alice-johnson');
DECLARE @bob    INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @authorId AND slug = 'bob-martinez');
DECLARE @post1  INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'getting-started-with-varyo-cms');
DECLARE @post2  INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'mastering-content-types-in-varyo');
DECLARE @post3  INT = (SELECT id FROM content_items WHERE tenant_id = @devTenantId AND content_type_id = @blogId AND slug = 'building-headless-apps-with-varyo-api');

MERGE content_item_titles AS t
USING (VALUES
    -- Categories (tr + en)
    (@cat1Id, 'tr', N'Teknoloji',   1),
    (@cat1Id, 'en', N'Technology',  1),
    (@cat2Id, 'tr', N'Tasarım',     1),
    (@cat2Id, 'en', N'Design',      1),
    (@cat3Id, 'tr', N'Geliştirme',  1),
    (@cat3Id, 'en', N'Development', 1),
    -- Authors (non-localized, use 'tr' as primary, 'en' also active)
    (@alice, 'tr', N'Alice Johnson', 1),
    (@alice, 'en', N'Alice Johnson', 1),
    (@bob,   'tr', N'Bob Martinez',  1),
    (@bob,   'en', N'Bob Martinez',  1),
    -- Blog Posts (tr + en)
    (@post1, 'tr', N'Varyo CMS ile Başlarken',                    1),
    (@post1, 'en', N'Getting Started with Varyo CMS',             1),
    (@post2, 'tr', N'Varyo''da İçerik Tiplerinde Uzmanlaşma',     1),
    (@post2, 'en', N'Mastering Content Types in Varyo',           1),
    (@post3, 'tr', N'Varyo API ile Headless Uygulamalar Geliştirme', 1),
    (@post3, 'en', N'Building Headless Apps with Varyo API',      1)
) AS s (content_item_id, language_code, title, is_active)
ON t.content_item_id = s.content_item_id AND t.language_code = s.language_code
WHEN MATCHED THEN UPDATE SET t.title = s.title, t.is_active = s.is_active
WHEN NOT MATCHED THEN INSERT (tenant_id, content_item_id, language_code, title, is_active)
    VALUES (@devTenantId, s.content_item_id, s.language_code, s.title, s.is_active);
GO

-- ============================================================
-- Demo Tech Blog — API Configuration & Credentials
--
-- Demo API Key: vk_{credentialId}_demo123secret
--   hash: $2a$12$In4tcJ1dvy6E0dvUEzZ0Teyb1npS6XpfhKIwBgwFoApQtWKuPyoGO
--   The credential id is auto-assigned (1 on a fresh DB, may differ if re-seeded).
--   Run: SELECT id FROM api_credentials WHERE name='Demo Blog API Key';
--        then use: vk_{id}_demo123secret
--
-- Usage (fresh DB, credential id=1):
--   GET /api/v1/dev-tenant/blog-post  X-API-Key: vk_1_demo123secret
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');
DECLARE @blogId INT = (SELECT id FROM content_types WHERE tenant_id = @devTenantId AND slug = 'blog-post');

-- api_configurations: enable Blog Post with ApiKey auth, read-only
IF NOT EXISTS (SELECT 1 FROM api_configurations WHERE tenant_id = @devTenantId AND content_type_id = @blogId)
    INSERT INTO api_configurations (
        tenant_id, content_type_id, is_enabled, is_public, auth_type,
        allow_read, allow_create, allow_update, allow_delete,
        allow_filtering, allow_sorting, allow_pagination,
        rate_limit_per_min, cache_seconds)
    VALUES (
        @devTenantId, @blogId, 1, 0, 'ApiKey',
        1, 0, 0, 0,
        1, 1, 1,
        60, 30);

-- api_credentials: demo key
IF NOT EXISTS (SELECT 1 FROM api_credentials WHERE tenant_id = @devTenantId AND name = 'Demo Blog API Key')
    INSERT INTO api_credentials (tenant_id, name, auth_type, api_key, is_active)
    VALUES (@devTenantId, 'Demo Blog API Key', 'ApiKey',
        '$2a$12$In4tcJ1dvy6E0dvUEzZ0Teyb1npS6XpfhKIwBgwFoApQtWKuPyoGO', 1);

-- Grant credential access to blog-post content type
DECLARE @credId INT = (SELECT id FROM api_credentials WHERE tenant_id = @devTenantId AND name = 'Demo Blog API Key' AND is_deleted = 0);
DECLARE @apiCfgId INT = (SELECT id FROM api_configurations WHERE tenant_id = @devTenantId AND content_type_id = @blogId);

IF @credId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM api_credential_content_types
    WHERE api_credential_id = @credId AND content_type_id = @blogId)
    INSERT INTO api_credential_content_types (tenant_id, api_credential_id, content_type_id)
    VALUES (@devTenantId, @credId, @blogId);

-- Field visibility for blog-post API
-- Fields: title, summary, body (alias→content), category, authors, featured, read-time (alias→readingTime), tags, published-at
-- Slug field is hidden from the API (it's in the top-level response already)
DECLARE @fTitle     INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'title');
DECLARE @fSummary   INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'summary');
DECLARE @fBody      INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'body');
DECLARE @fCategory  INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'category');
DECLARE @fAuthors   INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'authors');
DECLARE @fPublished INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'published-at');
DECLARE @fFeatured  INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'featured');
DECLARE @fReadTime  INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'read-time');
DECLARE @fTags      INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'tags');
DECLARE @fSlugField INT = (SELECT id FROM content_fields WHERE content_type_id = @blogId AND slug = 'slug');

IF @apiCfgId IS NOT NULL
BEGIN
    MERGE api_field_visibility AS t
    USING (VALUES
        (@apiCfgId, @fTitle,     1, NULL),          -- title → camelCase: "title"
        (@apiCfgId, @fSummary,   1, NULL),          -- summary → camelCase: "summary"
        (@apiCfgId, @fBody,      1, 'content'),     -- body → alias "content" → camelCase: "content"
        (@apiCfgId, @fCategory,  1, NULL),          -- category → camelCase: "category"
        (@apiCfgId, @fAuthors,   1, NULL),          -- authors → camelCase: "authors"
        (@apiCfgId, @fPublished, 1, 'published-at'), -- published-at → alias kebab → camelCase: "publishedAt"
        (@apiCfgId, @fFeatured,  1, NULL),           -- featured → camelCase: "featured"
        (@apiCfgId, @fReadTime,  1, 'reading-time'), -- read-time → alias kebab → camelCase: "readingTime"
        (@apiCfgId, @fTags,      1, NULL),          -- tags → camelCase: "tags"
        (@apiCfgId, @fSlugField, 0, NULL)           -- slug hidden (already in top-level)
    ) AS s (api_configuration_id, content_field_id, is_visible, response_key_alias)
    ON t.api_configuration_id = s.api_configuration_id AND t.content_field_id = s.content_field_id
    WHEN MATCHED THEN UPDATE SET t.is_visible = s.is_visible, t.response_key_alias = s.response_key_alias
    WHEN NOT MATCHED THEN INSERT (api_configuration_id, content_field_id, is_visible, response_key_alias)
        VALUES (s.api_configuration_id, s.content_field_id, s.is_visible, s.response_key_alias);
END
GO

-- ============================================================
-- Demo Tech Blog — Sample Dictionary Entries
-- ============================================================

DECLARE @devTenantId INT = (SELECT id FROM tenants WHERE slug = 'dev-tenant');

IF NOT EXISTS (SELECT 1 FROM dictionary_entries WHERE tenant_id = @devTenantId AND key_name = 'nav.home')
BEGIN
    INSERT INTO dictionary_entries (tenant_id, key_name, category) VALUES (@devTenantId, 'nav.home', 'navigation');
    DECLARE @navHomeId INT = SCOPE_IDENTITY();
    INSERT INTO dictionary_translations (entry_id, language_code, value) VALUES (@navHomeId, 'tr', N'Ana Sayfa');
    INSERT INTO dictionary_translations (entry_id, language_code, value) VALUES (@navHomeId, 'en', N'Home');
END

IF NOT EXISTS (SELECT 1 FROM dictionary_entries WHERE tenant_id = @devTenantId AND key_name = 'nav.blog')
BEGIN
    INSERT INTO dictionary_entries (tenant_id, key_name, category) VALUES (@devTenantId, 'nav.blog', 'navigation');
    DECLARE @navBlogId INT = SCOPE_IDENTITY();
    INSERT INTO dictionary_translations (entry_id, language_code, value) VALUES (@navBlogId, 'tr', N'Blog');
    INSERT INTO dictionary_translations (entry_id, language_code, value) VALUES (@navBlogId, 'en', N'Blog');
END

IF NOT EXISTS (SELECT 1 FROM dictionary_entries WHERE tenant_id = @devTenantId AND key_name = 'btn.read-more')
BEGIN
    INSERT INTO dictionary_entries (tenant_id, key_name, category) VALUES (@devTenantId, 'btn.read-more', 'buttons');
    DECLARE @btnReadMoreId INT = SCOPE_IDENTITY();
    INSERT INTO dictionary_translations (entry_id, language_code, value) VALUES (@btnReadMoreId, 'tr', N'Devamını Oku');
    INSERT INTO dictionary_translations (entry_id, language_code, value) VALUES (@btnReadMoreId, 'en', N'Read More');
END
GO

PRINT 'DEMO SEED COMPLETE.';
PRINT '  Tenant      : dev-tenant';
PRINT '  TenantAdmin : admin@dev.local / Admin123!';
PRINT '  Languages   : tr (default), en';
PRINT '  Demo content : Category (3) · Author (2) · Blog Post (3) — tr + en';
PRINT '  Demo API     : Blog Post enabled (ApiKey auth, read-only)';
PRINT '                 Key (plaintext): vk_1_demo123secret';
PRINT '                 Test: GET /api/v1/dev-tenant/blog-post  X-API-Key: vk_1_demo123secret';
GO
