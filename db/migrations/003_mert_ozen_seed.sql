-- ================================================================
-- 003_mert_ozen_seed.sql
-- Mert Özen kişisel blog platformu — tam sıfırlama + tohum verisi
-- v3: 13 content type, ApiKey auth, 14 post, 8 video, series içerik tipi
--
-- ⚠  DİKKAT: Tüm tenant verilerini siler. Sadece dev ortamında çalıştır.
--
-- API Key: vk_1_M3rtOzen_SecretKey2026!
--
-- Uygula:
--   docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P "Varyo_Dev2024!" -C -d Varyo \
--     -i /migrations/003_mert_ozen_seed.sql -b
-- ================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
  BEGIN TRANSACTION;

-- ================================================================
-- ADIM 1: Cleanup — child tables first
-- ================================================================
DELETE FROM content_item_titles;
DELETE FROM content_field_relations;
DELETE FROM content_field_values;
DELETE FROM api_field_visibility;
DELETE FROM user_content_type_permissions;
DELETE FROM api_credential_content_types;
DELETE FROM api_configurations;
DELETE FROM api_credentials;
DELETE FROM content_items;
DELETE FROM content_fields;
UPDATE content_types SET parent_id = NULL WHERE parent_id IS NOT NULL;
DELETE FROM content_types;
DELETE FROM media_assets;
DELETE FROM dictionary_translations;
DELETE FROM dictionary_entries;
DELETE FROM tenant_email_settings;
DELETE FROM languages;
DELETE FROM users;
DELETE FROM audit_logs;
DELETE FROM tenants;

DBCC CHECKIDENT ('tenants', RESEED, 0);
DBCC CHECKIDENT ('users', RESEED, 0);
DBCC CHECKIDENT ('languages', RESEED, 0);
DBCC CHECKIDENT ('content_types', RESEED, 0);
DBCC CHECKIDENT ('content_fields', RESEED, 0);
DBCC CHECKIDENT ('content_items', RESEED, 0);
DBCC CHECKIDENT ('content_field_values', RESEED, 0);
DBCC CHECKIDENT ('audit_logs', RESEED, 0);
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'api_credentials')
    DBCC CHECKIDENT ('api_credentials', RESEED, 0);
DBCC CHECKIDENT ('api_configurations', RESEED, 0);

-- ================================================================
-- ADIM 2: Tenant
-- ================================================================
DECLARE @TId INT;
INSERT INTO tenants (name, slug, is_active, created_at, updated_at, is_deleted)
VALUES (N'Mert Özen', N'mert-ozen', 1, GETUTCDATE(), GETUTCDATE(), 0);
SET @TId = SCOPE_IDENTITY();

-- ================================================================
-- ADIM 3: Admin user (password: Admin123!)
-- ================================================================
DECLARE @AdminId INT;
INSERT INTO users (tenant_id, email, password_hash, full_name, role, is_active, created_at, updated_at, is_deleted)
VALUES (@TId, N'admin@mert-ozen.local',
        N'$2a$12$4lCE3hHfiyShtwR7bC9CG.YVgp5pny2Qx.cSxGzCJkqvXw89r25EW',
        N'Mert Özen', N'TenantAdmin', 1, GETUTCDATE(), GETUTCDATE(), 0);
SET @AdminId = SCOPE_IDENTITY();

-- ================================================================
-- ADIM 4: Languages
-- ================================================================
INSERT INTO languages (tenant_id, code, name, is_default, is_active, flag_icon)
VALUES
  (@TId, N'tr', N'Türkçe', 1, 1, N'fi fi-tr'),
  (@TId, N'en', N'English', 0, 1, N'fi fi-gb');

-- ================================================================
-- ADIM 5: Content Types (13 adet)
-- ================================================================
DECLARE @CtSS INT, @CtCat INT, @CtSer INT, @CtPost INT, @CtVid INT,
        @CtAbout INT, @CtVL INT, @CtExp INT, @CtEdu INT, @CtSK INT,
        @CtBk INT, @CtMv INT, @CtAct INT;

-- 1. site-settings
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Site Ayarları', N'site-settings', NULL, N'bi-gear', 1, 0, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtSS = SCOPE_IDENTITY();

-- 2. category
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Kategori', N'category', NULL, N'bi-folder', 1, 1, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtCat = SCOPE_IDENTITY();

-- 3. series
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Seri', N'series', NULL, N'bi-collection', 1, 2, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtSer = SCOPE_IDENTITY();

-- 4. post
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Blog Yazısı', N'post', NULL, N'bi-file-text', 1, 3, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtPost = SCOPE_IDENTITY();

-- 5. video
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Video', N'video', NULL, N'bi-play-btn', 1, 4, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtVid = SCOPE_IDENTITY();

-- 6. about
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Hakkımda', N'about', NULL, N'bi-person', 1, 5, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtAbout = SCOPE_IDENTITY();

-- 7. video-list
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Video Listesi', N'video-list', NULL, N'bi-collection-play', 1, 6, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtVL = SCOPE_IDENTITY();

-- 8. experience
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Deneyim', N'experience', NULL, N'bi-briefcase', 1, 7, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtExp = SCOPE_IDENTITY();

-- 9. education
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Eğitim', N'education', NULL, N'bi-mortarboard', 1, 8, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtEdu = SCOPE_IDENTITY();

-- 10. skill-group
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Yetkinlik Grubu', N'skill-group', NULL, N'bi-tools', 1, 9, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtSK = SCOPE_IDENTITY();

-- 11. book
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Kitap', N'book', NULL, N'bi-book', 1, 10, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtBk = SCOPE_IDENTITY();

-- 12. movie
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Film & Dizi', N'movie', NULL, N'bi-film', 1, 11, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtMv = SCOPE_IDENTITY();

-- 13. activity
INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
VALUES (@TId, N'Aktivite', N'activity', NULL, N'bi-activity', 1, 12, GETUTCDATE(), GETUTCDATE(), 0);
SET @CtAct = SCOPE_IDENTITY();

-- ================================================================
-- ADIM 6: Content Fields (77 adet)
-- ================================================================
DECLARE
  @FSsTitle INT, @FSsTagline INT, @FSsCopyright INT, @FSsEmail INT,
  @FSsLinkedin INT, @FSsGithub INT, @FSsYoutube INT, @FSsInsta INT,
  @FCatName INT, @FCatAbbr INT, @FCatColor INT, @FCatDesc INT,
  @FSerName INT, @FSerDesc INT, @FSerSort INT,
  @FPostTitle INT, @FPostSummary INT, @FPostBody INT, @FPostImage INT,
  @FPostCat INT, @FPostReadMin INT, @FPostViewCount INT, @FPostFeatured INT,
  @FPostSeries INT, @FPostSerOrd INT, @FPostTags INT, @FPostPubAt INT,
  @FVidTitle INT, @FVidYtId INT, @FVidDesc INT, @FVidDuration INT,
  @FVidTags INT, @FVidPubAt INT, @FVidList INT,
  @FAbName INT, @FAbTitle INT, @FAbBioShort INT, @FAbBioLong INT, @FAbAvatar INT,
  @FAbExp INT, @FAbEdu INT, @FAbSK INT, @FAbBk INT, @FAbMv INT, @FAbAct INT,
  @FVLName INT, @FVLDesc INT, @FVLSort INT,
  @FExpCo INT, @FExpRole INT, @FExpStart INT, @FExpEnd INT, @FExpDesc INT, @FExpSort INT,
  @FEduInst INT, @FEduDegree INT, @FEduField INT, @FEduStartY INT, @FEduEndY INT, @FEduSort INT,
  @FSKName INT, @FSKItems INT, @FSKSort INT,
  @FBkTitle INT, @FBkAuthor INT, @FBkYear INT, @FBkStatus INT, @FBkProgress INT, @FBkSort INT,
  @FMvTitle INT, @FMvCreator INT, @FMvYear INT, @FMvType INT, @FMvNote INT,
  @FActName INT, @FActIcon INT, @FActDesc INT;

-- ── site-settings fields ────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'Site Başlığı', N'site-title', N'Text', 1, 1, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsTitle = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'Footer Tagline', N'footer-tagline', N'Text', 0, 1, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsTagline = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'Telif Hakkı Metni', N'copyright-text', N'Text', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsCopyright = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'E-posta', N'contact-email', N'Email', 0, 0, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsEmail = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'LinkedIn URL', N'linkedin-url', N'URL', 0, 0, 4, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsLinkedin = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'GitHub URL', N'github-url', N'URL', 0, 0, 5, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsGithub = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'YouTube URL', N'youtube-url', N'URL', 0, 0, 6, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsYoutube = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSS, N'Instagram URL', N'instagram-url', N'URL', 0, 0, 7, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSsInsta = SCOPE_IDENTITY();

-- ── category fields ─────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtCat, N'Ad', N'name', N'Text', 1, 1, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FCatName = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtCat, N'Kısaltma', N'abbreviation', N'Text', 1, 0, 1, N'{"max_length":5}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FCatAbbr = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtCat, N'Renk', N'color', N'Color', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FCatColor = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtCat, N'Açıklama', N'description', N'Text', 0, 1, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FCatDesc = SCOPE_IDENTITY();

-- ── series fields ────────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSer, N'Ad', N'name', N'Text', 1, 1, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSerName = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSer, N'Açıklama', N'description', N'Text', 0, 1, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSerDesc = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSer, N'Sıra', N'sort-order', N'Number', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSerSort = SCOPE_IDENTITY();

-- ── post fields ──────────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Başlık', N'title', N'Text', 1, 1, 0, N'{"max_length":300}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostTitle = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Özet', N'summary', N'Text', 0, 1, 1, N'{"max_length":500}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostSummary = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'İçerik', N'body', N'RichText', 0, 1, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostBody = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Kapak Görseli', N'cover-image', N'Image', 0, 0, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostImage = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Kategori', N'category', N'Relation', 1, 0, 4,
        CONCAT(N'{"target_content_type_id":', @CtCat, N',"display_field_slug":"name","value_field_slug":"id"}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostCat = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Okuma Süresi (dk)', N'read-time-min', N'Number', 0, 0, 5, N'{"min":1,"max":120}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostReadMin = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Görüntülenme', N'view-count', N'Number', 0, 0, 6, N'{"min":0}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostViewCount = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Öne Çıkan', N'is-featured', N'Boolean', 0, 0, 7, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostFeatured = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Seri', N'series', N'Relation', 0, 0, 8,
        CONCAT(N'{"target_content_type_id":', @CtSer, N',"display_field_slug":"name","value_field_slug":"id"}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostSeries = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Seri Sırası', N'series-order', N'Number', 0, 0, 9, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostSerOrd = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Etiketler', N'tags', N'Tags', 0, 0, 10, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostTags = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtPost, N'Yayın Tarihi', N'published-at', N'DateTime', 0, 0, 11, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FPostPubAt = SCOPE_IDENTITY();

-- ── video fields ─────────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVid, N'Başlık', N'title', N'Text', 1, 1, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FVidTitle = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVid, N'YouTube ID', N'youtube-id', N'Text', 1, 0, 1, N'{"max_length":20,"placeholder":"aqz-KE-bpKQ"}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FVidYtId = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVid, N'Açıklama', N'description', N'Text', 0, 1, 2, N'{"max_length":500}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FVidDesc = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVid, N'Süre', N'duration', N'Text', 0, 0, 3, N'{"max_length":20,"placeholder":"42:30"}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FVidDuration = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVid, N'Etiketler', N'tags', N'Tags', 0, 0, 4, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FVidTags = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVid, N'Yayın Tarihi', N'published-at', N'DateTime', 0, 0, 5, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FVidPubAt = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVid, N'Video Listesi', N'video-list', N'Relation', 0, 0, 6,
        CONCAT(N'{"target_content_type_id":', @CtVL, N',"display_field_slug":"name","value_field_slug":"id"}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FVidList = SCOPE_IDENTITY();

-- ── about fields ─────────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Ad Soyad', N'full-name', N'Text', 1, 0, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbName = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Unvan', N'title-line', N'Text', 0, 1, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbTitle = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Kısa Biyografi', N'bio-short', N'Text', 0, 1, 2, N'{"max_length":500}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbBioShort = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Uzun Biyografi', N'bio-long', N'RichText', 0, 1, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbBioLong = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Avatar', N'avatar', N'Image', 0, 0, 4, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbAvatar = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Deneyimler', N'experiences', N'MultiRelation', 0, 0, 5,
        CONCAT(N'{"target_content_type_id":', @CtExp, N',"display_field_slug":"role","value_field_slug":"id","allow_multiple":true}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbExp = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Eğitimler', N'educations', N'MultiRelation', 0, 0, 6,
        CONCAT(N'{"target_content_type_id":', @CtEdu, N',"display_field_slug":"degree","value_field_slug":"id","allow_multiple":true}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbEdu = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Yetkinlikler', N'skill-groups', N'MultiRelation', 0, 0, 7,
        CONCAT(N'{"target_content_type_id":', @CtSK, N',"display_field_slug":"name","value_field_slug":"id","allow_multiple":true}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbSK = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Kitaplar', N'books', N'MultiRelation', 0, 0, 8,
        CONCAT(N'{"target_content_type_id":', @CtBk, N',"display_field_slug":"title","value_field_slug":"id","allow_multiple":true}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbBk = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Film & Dizi', N'movies-series', N'MultiRelation', 0, 0, 9,
        CONCAT(N'{"target_content_type_id":', @CtMv, N',"display_field_slug":"title","value_field_slug":"id","allow_multiple":true}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbMv = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAbout, N'Aktiviteler', N'activities', N'MultiRelation', 0, 0, 10,
        CONCAT(N'{"target_content_type_id":', @CtAct, N',"display_field_slug":"name","value_field_slug":"id","allow_multiple":true}'),
        GETUTCDATE(), GETUTCDATE(), 0);
SET @FAbAct = SCOPE_IDENTITY();

-- ── video-list fields ────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVL, N'Ad', N'name', N'Text', 1, 1, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FVLName = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVL, N'Açıklama', N'description', N'Text', 0, 1, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FVLDesc = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtVL, N'Sıra', N'sort-order', N'Number', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FVLSort = SCOPE_IDENTITY();

-- ── experience fields ────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtExp, N'Şirket', N'company', N'Text', 1, 0, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FExpCo = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtExp, N'Rol', N'role', N'Text', 1, 1, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FExpRole = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtExp, N'Başlangıç', N'start-date', N'Date', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FExpStart = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtExp, N'Bitiş', N'end-date', N'Date', 0, 0, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FExpEnd = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtExp, N'Açıklama', N'description', N'Text', 0, 1, 4, N'{"max_length":1000}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FExpDesc = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtExp, N'Sıra', N'sort-order', N'Number', 0, 0, 5, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FExpSort = SCOPE_IDENTITY();

-- ── education fields ─────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtEdu, N'Kurum', N'institution', N'Text', 1, 0, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FEduInst = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtEdu, N'Derece', N'degree', N'Text', 1, 1, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FEduDegree = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtEdu, N'Alan', N'field', N'Text', 0, 1, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FEduField = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtEdu, N'Başlangıç Yılı', N'start-year', N'Number', 0, 0, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FEduStartY = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtEdu, N'Bitiş Yılı', N'end-year', N'Number', 0, 0, 4, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FEduEndY = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtEdu, N'Sıra', N'sort-order', N'Number', 0, 0, 5, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FEduSort = SCOPE_IDENTITY();

-- ── skill-group fields ───────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSK, N'Grup Adı', N'name', N'Text', 1, 1, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSKName = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSK, N'Yetkinlikler', N'items', N'Tags', 0, 0, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSKItems = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtSK, N'Sıra', N'sort-order', N'Number', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FSKSort = SCOPE_IDENTITY();

-- ── book fields ──────────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtBk, N'Başlık', N'title', N'Text', 1, 0, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FBkTitle = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtBk, N'Yazar', N'author', N'Text', 0, 0, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FBkAuthor = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtBk, N'Yıl', N'year', N'Number', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FBkYear = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtBk, N'Durum', N'status', N'Select', 0, 0, 3, N'{"choices":["reading","read"]}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FBkStatus = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtBk, N'İlerleme (%)', N'progress', N'Number', 0, 0, 4, N'{"min":0,"max":100}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FBkProgress = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtBk, N'Sıra', N'sort-order', N'Number', 0, 0, 5, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FBkSort = SCOPE_IDENTITY();

-- ── movie fields ─────────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtMv, N'Başlık', N'title', N'Text', 1, 0, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FMvTitle = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtMv, N'Yapımcı / Yönetmen', N'creator', N'Text', 0, 0, 1, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FMvCreator = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtMv, N'Yıl', N'year', N'Number', 0, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FMvYear = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtMv, N'Tür', N'type', N'Select', 0, 0, 3, N'{"choices":["film","dizi"]}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FMvType = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtMv, N'Not', N'note', N'Text', 0, 1, 4, N'{"max_length":500}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FMvNote = SCOPE_IDENTITY();

-- ── activity fields ──────────────────────────────────────────────
INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAct, N'Ad', N'name', N'Text', 1, 1, 0, NULL, GETUTCDATE(), GETUTCDATE(), 0);
SET @FActName = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAct, N'İkon', N'icon', N'Text', 0, 0, 1, N'{"max_length":50}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FActIcon = SCOPE_IDENTITY();

INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
VALUES (@TId, @CtAct, N'Açıklama', N'description', N'Text', 0, 1, 2, N'{"max_length":300}', GETUTCDATE(), GETUTCDATE(), 0);
SET @FActDesc = SCOPE_IDENTITY();

-- ================================================================
-- ADIM 7: API Credential
-- ================================================================
DECLARE @Cred1 INT;
INSERT INTO api_credentials (tenant_id, name, auth_type, api_key, is_active, created_at, updated_at, is_deleted)
VALUES (@TId, N'MertOzen API Anahtarı', N'ApiKey',
        N'$2a$12$1WELAjAcWU6K6W.0kpasS.6K4loSFLjByh/26gsXFo/WTX/qsHaQK',
        1, GETUTCDATE(), GETUTCDATE(), 0);
SET @Cred1 = SCOPE_IDENTITY();

-- ================================================================
-- ADIM 8: API Configurations (13 adet)
-- ================================================================
INSERT INTO api_configurations (tenant_id, content_type_id, is_enabled, auth_type, allow_filtering, allow_sorting, allow_pagination, is_public, allow_read, allow_create, allow_update, allow_delete, rate_limit_per_min, cache_seconds, created_at, updated_at)
VALUES
  (@TId, @CtSS,    1, N'ApiKey', 0, 0, 0, 0, 1, 0, 0, 0,  60, 600, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtCat,   1, N'ApiKey', 1, 1, 1, 0, 1, 0, 0, 0, 120, 300, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtSer,   1, N'ApiKey', 1, 1, 1, 0, 1, 0, 0, 0, 120, 300, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtPost,  1, N'ApiKey', 1, 1, 1, 0, 1, 0, 0, 0, 120,  60, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtVid,   1, N'ApiKey', 1, 1, 1, 0, 1, 0, 0, 0, 120,  60, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtAbout, 1, N'ApiKey', 0, 0, 0, 0, 1, 0, 0, 0,  60, 600, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtVL,    1, N'ApiKey', 1, 1, 1, 0, 1, 0, 0, 0, 120, 300, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtExp,   1, N'ApiKey', 0, 1, 0, 0, 1, 0, 0, 0,  60, 600, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtEdu,   1, N'ApiKey', 0, 1, 0, 0, 1, 0, 0, 0,  60, 600, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtSK,    1, N'ApiKey', 0, 1, 1, 0, 1, 0, 0, 0,  60, 600, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtBk,    1, N'ApiKey', 1, 1, 1, 0, 1, 0, 0, 0, 120, 300, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtMv,    1, N'ApiKey', 1, 1, 1, 0, 1, 0, 0, 0, 120, 300, GETUTCDATE(), GETUTCDATE()),
  (@TId, @CtAct,   1, N'ApiKey', 0, 1, 1, 0, 1, 0, 0, 0,  60, 600, GETUTCDATE(), GETUTCDATE());

-- ================================================================
-- ADIM 9: API Credential Content Types
-- ================================================================
INSERT INTO api_credential_content_types (tenant_id, api_credential_id, content_type_id)
VALUES
  (@TId, @Cred1, @CtSS),   (@TId, @Cred1, @CtCat),  (@TId, @Cred1, @CtSer),
  (@TId, @Cred1, @CtPost),  (@TId, @Cred1, @CtVid),  (@TId, @Cred1, @CtAbout),
  (@TId, @Cred1, @CtVL),    (@TId, @Cred1, @CtExp),  (@TId, @Cred1, @CtEdu),
  (@TId, @Cred1, @CtSK),    (@TId, @Cred1, @CtBk),   (@TId, @Cred1, @CtMv),
  (@TId, @Cred1, @CtAct);

-- ================================================================
-- ADIM 10: API Field Visibility (tüm alanlar görünür)
-- ================================================================
INSERT INTO api_field_visibility (api_configuration_id, content_field_id, is_visible, response_key_alias)
SELECT ac.id, cf.id, 1, NULL
FROM api_configurations ac
JOIN content_fields cf
  ON  cf.content_type_id = ac.content_type_id
  AND cf.is_deleted = 0
  AND cf.tenant_id = ac.tenant_id
WHERE ac.tenant_id = @TId;
  -- ════════════════════════════════════════════════════════════════
  -- ADIM 11: SİTE AYARLARI
  -- ════════════════════════════════════════════════════════════════
  DECLARE @ISS INT;

  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSS, N'site-settings', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @ISS = SCOPE_IDENTITY();

  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @ISS, @FSsTitle,     N'tr',  N'Mert Özen',                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsTitle,     N'en',  N'Mert Özen',                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsTagline,   N'tr',  N'Yazılım · Mimari · Öğrenme',               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsTagline,   N'en',  N'Software · Architecture · Learning',        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsCopyright, N'all', N'© 2026 Mert Özen. Tüm hakları saklıdır.',  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsEmail,     N'all', N'mert@ozen.dev',                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsLinkedin,  N'all', N'https://linkedin.com/in/mrtozn',            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsGithub,    N'all', N'https://github.com/mrtozn1923',             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsYoutube,   N'all', N'https://youtube.com/@mertozen1923',         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @ISS, @FSsInsta,     N'all', N'https://instagram.com/mrtozn1923',          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 12: KATEGORİLER (8 ADET)
  -- ════════════════════════════════════════════════════════════════
  DECLARE @CatBE INT, @CatFE INT, @CatOPS INT, @CatDB INT, @CatAI INT, @CatMOB INT, @CatSEC INT, @CatSYS INT;

  -- Backend
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'backend', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatBE = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatBE, @FCatName,  N'tr',  N'Backend',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatBE, @FCatName,  N'en',  N'Backend',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatBE, @FCatAbbr,  N'all', N'BE',                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatBE, @FCatColor, N'all', N'#3B82F6',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatBE, @FCatDesc,  N'tr',  N'Sunucu tarafı yazılım geliştirme', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatBE, @FCatDesc,  N'en',  N'Server-side software development', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Frontend
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'frontend', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatFE = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatFE, @FCatName,  N'tr',  N'Frontend',                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatFE, @FCatName,  N'en',  N'Frontend',                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatFE, @FCatAbbr,  N'all', N'FE',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatFE, @FCatColor, N'all', N'#F59E0B',                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatFE, @FCatDesc,  N'tr',  N'İstemci tarafı geliştirme', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatFE, @FCatDesc,  N'en',  N'Client-side development',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- DevOps
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'devops', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatOPS = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatOPS, @FCatName,  N'tr',  N'DevOps',                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatOPS, @FCatName,  N'en',  N'DevOps',                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatOPS, @FCatAbbr,  N'all', N'DO',                                     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatOPS, @FCatColor, N'all', N'#10B981',                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatOPS, @FCatDesc,  N'tr',  N'Geliştirme ve operasyon pratikleri',     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatOPS, @FCatDesc,  N'en',  N'Development and operations practices',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Veritabanı
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'veritabani', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatDB = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatDB, @FCatName,  N'tr',  N'Veritabanı',                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatDB, @FCatName,  N'en',  N'Database',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatDB, @FCatAbbr,  N'all', N'DB',                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatDB, @FCatColor, N'all', N'#8B5CF6',                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatDB, @FCatDesc,  N'tr',  N'Veritabanı tasarımı ve yönetimi', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatDB, @FCatDesc,  N'en',  N'Database design and management',  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Yapay Zeka
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'yapay-zeka', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatAI = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatAI, @FCatName,  N'tr',  N'Yapay Zeka',                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatAI, @FCatName,  N'en',  N'Artificial Intelligence',         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatAI, @FCatAbbr,  N'all', N'AI',                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatAI, @FCatColor, N'all', N'#06B6D4',                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatAI, @FCatDesc,  N'tr',  N'Makine öğrenmesi ve yapay zeka',  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatAI, @FCatDesc,  N'en',  N'Machine learning and AI',         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Mobil
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'mobil', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatMOB = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatMOB, @FCatName,  N'tr',  N'Mobil',                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatMOB, @FCatName,  N'en',  N'Mobile',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatMOB, @FCatAbbr,  N'all', N'MOB',                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatMOB, @FCatColor, N'all', N'#F43F5E',                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatMOB, @FCatDesc,  N'tr',  N'Mobil uygulama geliştirme',       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatMOB, @FCatDesc,  N'en',  N'Mobile application development',  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Güvenlik
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'guvenlik', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatSEC = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatSEC, @FCatName,  N'tr',  N'Güvenlik',                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSEC, @FCatName,  N'en',  N'Security',                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSEC, @FCatAbbr,  N'all', N'SEC',                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSEC, @FCatColor, N'all', N'#EF4444',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSEC, @FCatDesc,  N'tr',  N'Yazılım ve sistem güvenliği',    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSEC, @FCatDesc,  N'en',  N'Software and system security',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Sistem
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCat, N'sistem', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatSYS = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @CatSYS, @FCatName,  N'tr',  N'Sistem',                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSYS, @FCatName,  N'en',  N'System',                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSYS, @FCatAbbr,  N'all', N'SYS',                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSYS, @FCatColor, N'all', N'#6366F1',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSYS, @FCatDesc,  N'tr',  N'Sistem tasarımı ve mimarisi',    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @CatSYS, @FCatDesc,  N'en',  N'System design and architecture', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 13: SERİLER (2 ADET)
  -- ════════════════════════════════════════════════════════════════
  DECLARE @Ser1 INT, @Ser2 INT;

  -- Ser1: Microservices 101
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSer, N'microservices-101', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Ser1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @Ser1, @FSerName, N'tr',  N'Microservices 101',                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser1, @FSerName, N'en',  N'Microservices 101',                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser1, @FSerDesc, N'tr',  N'Kubernetes ve konteyner tabanlı mikroservis mimarisi',     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser1, @FSerDesc, N'en',  N'Kubernetes and container-based microservice architecture', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser1, @FSerSort, N'all', NULL,                                                        1,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Ser2: PostgreSQL Derinlemesine
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSer, N'postgresql-derinlemesine', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Ser2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @Ser2, @FSerName, N'tr',  N'PostgreSQL Derinlemesine',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser2, @FSerName, N'en',  N'PostgreSQL Deep Dive',                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser2, @FSerDesc, N'tr',  N'PostgreSQL ileri düzey özellikler ve performans', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser2, @FSerDesc, N'en',  N'PostgreSQL advanced features and performance',    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Ser2, @FSerSort, N'all', NULL,                                               2,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 14: VİDEO LİSTELERİ (4 ADET)
  -- ════════════════════════════════════════════════════════════════
  DECLARE @VL1 INT, @VL2 INT, @VL3 INT, @VL4 INT;

  -- VL1: Backend & Mimari
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVL, N'backend-ve-mimari', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @VL1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @VL1, @FVLName, N'tr',  N'Backend & Mimari',                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL1, @FVLName, N'en',  N'Backend & Architecture',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL1, @FVLDesc, N'tr',  N'Backend sistemler ve yazılım mimarisi videoları', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL1, @FVLDesc, N'en',  N'Backend systems and software architecture videos',NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL1, @FVLSort, N'all', NULL,                                                1,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- VL2: DevOps & Altyapı
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVL, N'devops-ve-altyapi', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @VL2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @VL2, @FVLName, N'tr',  N'DevOps & Altyapı',                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL2, @FVLName, N'en',  N'DevOps & Infrastructure',               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL2, @FVLDesc, N'tr',  N'Docker, Kubernetes ve CI/CD videoları', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL2, @FVLDesc, N'en',  N'Docker, Kubernetes and CI/CD videos',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL2, @FVLSort, N'all', NULL,                                       2,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- VL3: Veri & Yapay Zeka
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVL, N'veri-ve-yapay-zeka', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @VL3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @VL3, @FVLName, N'tr',  N'Veri & Yapay Zeka',                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL3, @FVLName, N'en',  N'Data & AI',                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL3, @FVLDesc, N'tr',  N'Veri mühendisliği ve makine öğrenmesi videoları',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL3, @FVLDesc, N'en',  N'Data engineering and machine learning videos',      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL3, @FVLSort, N'all', NULL,                                                  3,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- VL4: Frontend & İstemci
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVL, N'frontend-ve-istemci', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @VL4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @VL4, @FVLName, N'tr',  N'Frontend & İstemci',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL4, @FVLName, N'en',  N'Frontend & Client',                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL4, @FVLDesc, N'tr',  N'React, Next.js ve modern frontend videoları', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL4, @FVLDesc, N'en',  N'React, Next.js and modern frontend videos',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @VL4, @FVLSort, N'all', NULL,                                             4,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 15: BLOG YAZILARI (14 ADET)
  -- ════════════════════════════════════════════════════════════════
  DECLARE @P1 INT, @P2 INT, @P3 INT, @P4 INT, @P5 INT, @P6 INT, @P7 INT,
          @P8 INT, @P9 INT, @P10 INT, @P11 INT, @P12 INT, @P13 INT, @P14 INT;

  -- P1: Kubernetes ile Mikroservis Orkestrasyonu (Ser1 serOrd=1, featured=1, cat=DevOps)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'kubernetes-ile-mikroservis-orkestrasyonu', N'published', @AdminId, @AdminId, CAST(N'2026-06-10T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P1, @FPostTitle,     N'tr',  N'Kubernetes ile Mikroservis Orkestrasyonu',                                                                                                                                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostTitle,     N'en',  N'Microservice Orchestration with Kubernetes',                                                                                                                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostSummary,   N'tr',  N'Kubernetes üzerinde production-grade mikroservis cluster kurulum rehberi.',                                                                                                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostSummary,   N'en',  N'A guide to setting up a production-grade microservice cluster on Kubernetes.',                                                                                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostBody,      N'tr',  N'<p>Bu yazıda Kubernetes ile mikroservis orkestrasyonunu adım adım inceliyoruz. Deployment, Service ve Ingress yapılandırmalarından başlayarak ileri düzey konulara değiniyoruz.</p>', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostBody,      N'en',  N'<p>In this post, we explore microservice orchestration with Kubernetes step by step, from Deployment and Service configurations to advanced topics.</p>',                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostReadMin,   N'all', NULL,                                                                                                                                                                          8,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostViewCount, N'all', NULL,                                                                                                                                                                          0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostFeatured,  N'all', NULL,                                                                                                                                                                          NULL, 1,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostTags,      N'all', N'["devops","kubernetes","microservices"]',                                                                                                                                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostPubAt,     N'all', NULL,                                                                                                                                                                          NULL, NULL, CAST(N'2026-06-10T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE()),
    (@TId, @P1, @FPostSerOrd,    N'all', NULL,                                                                                                                                                                          1,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- P2: TypeScript ile Tip Güvenliği (no series, featured=0, cat=Frontend)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'typescript-tip-guvenligi', N'published', @AdminId, @AdminId, CAST(N'2026-05-28T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P2, @FPostTitle,     N'tr',  N'TypeScript ile Tip Güvenliği',                                                                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostTitle,     N'en',  N'Type Safety with TypeScript',                                                                                                                                     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostSummary,   N'tr',  N'TypeScript generic tipler, utility types ve ileri düzey tip sistemi teknikleri.',                                                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostSummary,   N'en',  N'TypeScript generics, utility types and advanced type system techniques.',                                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostBody,      N'tr',  N'<p>TypeScript tip sistemi, büyük ölçekli uygulamalarda hata yakalamayı mümkün kılar. Bu yazıda ileri düzey tip tekniklerini inceliyoruz.</p>',                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostBody,      N'en',  N'<p>TypeScript type system makes error detection possible in large-scale applications. This post examines advanced type techniques.</p>',                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostReadMin,   N'all', NULL,                                                                                                                                                               6,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostViewCount, N'all', NULL,                                                                                                                                                               0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostFeatured,  N'all', NULL,                                                                                                                                                               NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostTags,      N'all', N'["frontend","typescript","javascript"]',                                                                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P2, @FPostPubAt,     N'all', NULL,                                                                                                                                                               NULL, NULL, CAST(N'2026-05-28T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P3: SQL Server Performans İpuçları (no series, featured=0, cat=Veritabanı)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'sql-server-performans-ipuclari', N'published', @AdminId, @AdminId, CAST(N'2026-05-15T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P3, @FPostTitle,     N'tr',  N'SQL Server Performans İpuçları',                                                                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostTitle,     N'en',  N'SQL Server Performance Tips',                                                                                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostSummary,   N'tr',  N'Index stratejileri, sorgu optimizasyonu ve execution plan analizi.',                                                                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostSummary,   N'en',  N'Index strategies, query optimization and execution plan analysis.',                                                                                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostBody,      N'tr',  N'<p>SQL Server performansı doğru index stratejisi ve sorgu yazımıyla büyük ölçüde artırılabilir. Bu yazıda temel teknikleri paylaşıyoruz.</p>',                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostBody,      N'en',  N'<p>SQL Server performance can be greatly improved with the right index strategy and query writing. This post shares key techniques.</p>',                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostReadMin,   N'all', NULL,                                                                                                                                                                  10,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostViewCount, N'all', NULL,                                                                                                                                                                  0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostFeatured,  N'all', NULL,                                                                                                                                                                  NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostTags,      N'all', N'["database","sql-server","performance"]',                                                                                                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P3, @FPostPubAt,     N'all', NULL,                                                                                                                                                                  NULL, NULL, CAST(N'2026-05-15T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P4: Docker ile Geliştirme Ortamı Kurulumu (no series, featured=1, cat=DevOps)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'docker-ile-gelistirme-ortami', N'published', @AdminId, @AdminId, CAST(N'2026-05-02T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P4, @FPostTitle,     N'tr',  N'Docker ile Geliştirme Ortamı Kurulumu',                                                                                                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostTitle,     N'en',  N'Setting Up Dev Environment with Docker',                                                                                                                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostSummary,   N'tr',  N'Docker Compose ile izole, tekrarlanabilir geliştirme ortamları oluşturma.',                                                                                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostSummary,   N'en',  N'Creating isolated, reproducible development environments with Docker Compose.',                                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostBody,      N'tr',  N'<p>Docker ile geliştirme ortamı kurmak, ekip genelinde tutarlılığı sağlar ve "bende çalışıyor" sorununu ortadan kaldırır.</p>',                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostBody,      N'en',  N'<p>Setting up a development environment with Docker ensures consistency across the team and eliminates the "works on my machine" problem.</p>',                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostReadMin,   N'all', NULL,                                                                                                                                                                     12,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostViewCount, N'all', NULL,                                                                                                                                                                     0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostFeatured,  N'all', NULL,                                                                                                                                                                     NULL, 1,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostTags,      N'all', N'["devops","docker","containers"]',                                                                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P4, @FPostPubAt,     N'all', NULL,                                                                                                                                                                     NULL, NULL, CAST(N'2026-05-02T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P5: PostgreSQL JSON Operatörleri (Ser2 serOrd=1, featured=0, cat=Veritabanı)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'postgresql-json-operatorleri', N'published', @AdminId, @AdminId, CAST(N'2026-04-18T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P5 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P5, @FPostTitle,     N'tr',  N'PostgreSQL JSON Operatörleri',                                                                                                                                                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostTitle,     N'en',  N'PostgreSQL JSON Operators',                                                                                                                                                     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostSummary,   N'tr',  N'JSONB tipi, JSON sorgu operatörleri ve index stratejileri ile yarı-yapısal veri yönetimi.',                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostSummary,   N'en',  N'Managing semi-structured data with JSONB, JSON query operators and index strategies.',                                                                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostBody,      N'tr',  N'<p>PostgreSQL JSONB tipi, ilişkisel veritabanında yarı-yapısal veri saklamanın güçlü bir yoludur. Bu yazıda JSON operatörlerini ve GIN indexlerini inceliyoruz.</p>',          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostBody,      N'en',  N'<p>PostgreSQL JSONB is a powerful way to store semi-structured data in a relational database. This post examines JSON operators and GIN indexes.</p>',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostReadMin,   N'all', NULL,                                                                                                                                                                              7,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostViewCount, N'all', NULL,                                                                                                                                                                              0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostFeatured,  N'all', NULL,                                                                                                                                                                              NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostTags,      N'all', N'["database","postgresql","json"]',                                                                                                                                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostPubAt,     N'all', NULL,                                                                                                                                                                              NULL, NULL, CAST(N'2026-04-18T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE()),
    (@TId, @P5, @FPostSerOrd,    N'all', NULL,                                                                                                                                                                              1,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- P6: PostgreSQL Window Fonksiyonları (Ser2 serOrd=2, featured=0, cat=Veritabanı)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'postgresql-window-fonksiyonlari', N'published', @AdminId, @AdminId, CAST(N'2026-04-04T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P6 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P6, @FPostTitle,     N'tr',  N'PostgreSQL Window Fonksiyonları',                                                                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostTitle,     N'en',  N'PostgreSQL Window Functions',                                                                                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostSummary,   N'tr',  N'ROW_NUMBER, RANK, LAG/LEAD ve gelişmiş analitik sorgular.',                                                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostSummary,   N'en',  N'ROW_NUMBER, RANK, LAG/LEAD and advanced analytical queries.',                                                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostBody,      N'tr',  N'<p>Window fonksiyonları, verileri gruplamadan satır bazlı hesaplamalar yapmanıza olanak tanır. Bu yazıda pratik kullanım senaryolarını ele alıyoruz.</p>',          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostBody,      N'en',  N'<p>Window functions allow row-level calculations without grouping data. This post covers practical usage scenarios.</p>',                                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostReadMin,   N'all', NULL,                                                                                                                                                                   8,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostViewCount, N'all', NULL,                                                                                                                                                                   0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostFeatured,  N'all', NULL,                                                                                                                                                                   NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostTags,      N'all', N'["database","postgresql","analytics"]',                                                                                                                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostPubAt,     N'all', NULL,                                                                                                                                                                   NULL, NULL, CAST(N'2026-04-04T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE()),
    (@TId, @P6, @FPostSerOrd,    N'all', NULL,                                                                                                                                                                   2,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- P7: Kubernetes Monitoring: Prometheus & Grafana (no series, featured=1, cat=DevOps)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'kubernetes-monitoring-prometheus', N'published', @AdminId, @AdminId, CAST(N'2026-03-21T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P7 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P7, @FPostTitle,     N'tr',  N'Kubernetes Monitoring: Prometheus & Grafana',                                                                                                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostTitle,     N'en',  N'Kubernetes Monitoring: Prometheus & Grafana',                                                                                                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostSummary,   N'tr',  N'Kubernetes cluster monitoring için Prometheus ve Grafana kurulumu ve dashboard tasarımı.',                                                                                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostSummary,   N'en',  N'Setting up Prometheus and Grafana for Kubernetes cluster monitoring and dashboard design.',                                                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostBody,      N'tr',  N'<p>Prometheus ve Grafana, Kubernetes ortamında gözlemlenebilirlik için en yaygın araç çiftidir. Bu yazıda sıfırdan kurulum ve metrik toplama sürecini anlatıyoruz.</p>',                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostBody,      N'en',  N'<p>Prometheus and Grafana are the most common tool pair for observability in Kubernetes environments. This post explains the setup from scratch and metric collection process.</p>',                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostReadMin,   N'all', NULL,                                                                                                                                                                                                 15,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostViewCount, N'all', NULL,                                                                                                                                                                                                 0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostFeatured,  N'all', NULL,                                                                                                                                                                                                 NULL, 1,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostTags,      N'all', N'["devops","kubernetes","monitoring"]',                                                                                                                                                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P7, @FPostPubAt,     N'all', NULL,                                                                                                                                                                                                 NULL, NULL, CAST(N'2026-03-21T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P8: Mikroservisler Arası İletişim (Ser1 serOrd=2, featured=0, cat=Backend)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'mikroservisler-arasi-iletisim', N'published', @AdminId, @AdminId, CAST(N'2026-03-07T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P8 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P8, @FPostTitle,     N'tr',  N'Mikroservisler Arası İletişim: gRPC vs REST',                                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostTitle,     N'en',  N'Microservice Communication: gRPC vs REST',                                                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostSummary,   N'tr',  N'Mikroservis mimarisinde senkron ve asenkron iletişim pattern''ları, protokol seçimi.',                                                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostSummary,   N'en',  N'Synchronous and asynchronous communication patterns in microservice architecture, protocol selection.',                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostBody,      N'tr',  N'<p>Mikroservisler arası iletişim, sistemin genel performansını ve güvenilirliğini doğrudan etkiler. gRPC ve REST arasındaki seçim kritik bir mimari karardır.</p>', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostBody,      N'en',  N'<p>Communication between microservices directly affects the overall performance and reliability of the system. The choice between gRPC and REST is a critical architectural decision.</p>', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostReadMin,   N'all', NULL,                                                                                                                                               9,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostViewCount, N'all', NULL,                                                                                                                                               0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostFeatured,  N'all', NULL,                                                                                                                                               NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostTags,      N'all', N'["backend","microservices","grpc"]',                                                                                                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostPubAt,     N'all', NULL,                                                                                                                                               NULL, NULL, CAST(N'2026-03-07T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE()),
    (@TId, @P8, @FPostSerOrd,    N'all', NULL,                                                                                                                                               2,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- P9: Mikroservis Mimarisinde Veritabanı Stratejileri (Ser1 serOrd=3, featured=1, cat=Veritabanı)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'mikroservis-mimarisinde-veritabani', N'published', @AdminId, @AdminId, CAST(N'2026-02-14T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P9 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P9, @FPostTitle,     N'tr',  N'Mikroservis Mimarisinde Veritabanı Stratejileri',                                                                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostTitle,     N'en',  N'Database Strategies in Microservice Architecture',                                                                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostSummary,   N'tr',  N'Database per service, CQRS, event sourcing ve saga pattern ile veri tutarlılığı.',                                                                                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostSummary,   N'en',  N'Database per service, CQRS, event sourcing and data consistency with saga pattern.',                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostBody,      N'tr',  N'<p>Mikroservis mimarisinde her servis kendi veritabanına sahip olmalıdır. Bu yaklaşım gevşek bağlılık sağlar ancak veri tutarlılığı yeni zorluklar doğurur.</p>',       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostBody,      N'en',  N'<p>In microservice architecture, each service should own its database. This approach provides loose coupling but data consistency introduces new challenges.</p>',       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostReadMin,   N'all', NULL,                                                                                                                                                                      11,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostViewCount, N'all', NULL,                                                                                                                                                                      0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostFeatured,  N'all', NULL,                                                                                                                                                                      NULL, 1,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostTags,      N'all', N'["backend","microservices","database"]',                                                                                                                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostPubAt,     N'all', NULL,                                                                                                                                                                      NULL, NULL, CAST(N'2026-02-14T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE()),
    (@TId, @P9, @FPostSerOrd,    N'all', NULL,                                                                                                                                                                      3,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- P10: Sistem Tasarımı 101: URL Kısaltma (no series, featured=0, cat=Sistem)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'sistem-tasarimi-101-url-kisaltma', N'published', @AdminId, @AdminId, CAST(N'2026-05-05T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P10 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P10, @FPostTitle,     N'tr',  N'Sistem Tasarımı 101: URL Kısaltma',                                                                                                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostTitle,     N'en',  N'System Design 101: URL Shortener',                                                                                                                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostSummary,   N'tr',  N'URL kısaltma servisi tasarımı: hashing, önbellekleme, ölçeklendirme ve yük dengeleme.',                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostSummary,   N'en',  N'URL shortener service design: hashing, caching, scaling and load balancing.',                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostBody,      N'tr',  N'<p>URL kısaltma servisi, sistem tasarımı görüşmelerinin klasik sorusu haline gelmiştir. Bu yazıda temel bileşenleri ve ölçeklendirme stratejilerini inceliyoruz.</p>', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostBody,      N'en',  N'<p>URL shortener service has become a classic system design interview question. This post examines the key components and scaling strategies.</p>',               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostReadMin,   N'all', NULL,                                                                                                                                                               13,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostViewCount, N'all', NULL,                                                                                                                                                               0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostFeatured,  N'all', NULL,                                                                                                                                                               NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostTags,      N'all', N'["system-design","scalability","architecture"]',                                                                                                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P10, @FPostPubAt,     N'all', NULL,                                                                                                                                                               NULL, NULL, CAST(N'2026-05-05T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P11: Next.js App Router ile Full-Stack Uygulama (no series, featured=0, cat=Frontend)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'nextjs-app-router-ile-full-stack', N'published', @AdminId, @AdminId, CAST(N'2026-04-20T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P11 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P11, @FPostTitle,     N'tr',  N'Next.js App Router ile Full-Stack Uygulama',                                                                                                                                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostTitle,     N'en',  N'Full-Stack App with Next.js App Router',                                                                                                                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostSummary,   N'tr',  N'Next.js 14 App Router, Server Components, Server Actions ve streaming ile modern full-stack geliştirme.',                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostSummary,   N'en',  N'Modern full-stack development with Next.js 14 App Router, Server Components, Server Actions and streaming.',                                                                     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostBody,      N'tr',  N'<p>Next.js App Router, React Server Components ile full-stack geliştirmeyi yeniden tanımlıyor. Bu yazıda temel kavramları ve pratik kullanım senaryolarını ele alıyoruz.</p>',  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostBody,      N'en',  N'<p>Next.js App Router redefines full-stack development with React Server Components. This post covers key concepts and practical usage scenarios.</p>',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostReadMin,   N'all', NULL,                                                                                                                                                                                9,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostViewCount, N'all', NULL,                                                                                                                                                                                0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostFeatured,  N'all', NULL,                                                                                                                                                                                NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostTags,      N'all', N'["frontend","nextjs","typescript"]',                                                                                                                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P11, @FPostPubAt,     N'all', NULL,                                                                                                                                                                                NULL, NULL, CAST(N'2026-04-20T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P12: Redis ile Önbellekleme Stratejileri (no series, featured=0, cat=Veritabanı)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'redis-ile-onbellekleme-stratejileri', N'published', @AdminId, @AdminId, CAST(N'2026-04-07T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P12 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P12, @FPostTitle,     N'tr',  N'Redis ile Önbellekleme Stratejileri',                                                                                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostTitle,     N'en',  N'Caching Strategies with Redis',                                                                                                                                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostSummary,   N'tr',  N'Cache-aside, write-through, cache invalidation ve Redis Cluster ile yüksek performanslı önbellekleme.',                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostSummary,   N'en',  N'High-performance caching with cache-aside, write-through, cache invalidation and Redis Cluster.',                                                                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostBody,      N'tr',  N'<p>Redis, in-memory veri yapısı ile modern uygulamalarda önbellekleme için standart araç haline gelmiştir. Bu yazıda pratik stratejileri inceliyoruz.</p>',                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostBody,      N'en',  N'<p>Redis has become the standard tool for caching in modern applications with its in-memory data structure. This post examines practical strategies.</p>',                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostReadMin,   N'all', NULL,                                                                                                                                                                            7,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostViewCount, N'all', NULL,                                                                                                                                                                            0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostFeatured,  N'all', NULL,                                                                                                                                                                            NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostTags,      N'all', N'["database","redis","caching"]',                                                                                                                                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P12, @FPostPubAt,     N'all', NULL,                                                                                                                                                                            NULL, NULL, CAST(N'2026-04-07T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P13: .NET ile Clean Architecture (no series, featured=0, cat=Backend)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'net-ile-clean-architecture', N'published', @AdminId, @AdminId, CAST(N'2026-03-16T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P13 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P13, @FPostTitle,     N'tr',  N'.NET ile Clean Architecture',                                                                                                                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostTitle,     N'en',  N'Clean Architecture with .NET',                                                                                                                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostSummary,   N'tr',  N'Domain-driven design, katmanlı mimari ve dependency inversion prensipleri ile sürdürülebilir .NET uygulaması.',                                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostSummary,   N'en',  N'Sustainable .NET application with domain-driven design, layered architecture and dependency inversion principles.',                                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostBody,      N'tr',  N'<p>Clean Architecture, uygulamanızın iş mantığını dış bağımlılıklardan ayırarak test edilebilir ve sürdürülebilir bir yapı oluşturmanızı sağlar.</p>',                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostBody,      N'en',  N'<p>Clean Architecture allows you to create a testable and maintainable structure by separating your application''s business logic from external dependencies.</p>',                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostReadMin,   N'all', NULL,                                                                                                                                                                                                    14,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostViewCount, N'all', NULL,                                                                                                                                                                                                    0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostFeatured,  N'all', NULL,                                                                                                                                                                                                    NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostTags,      N'all', N'["backend","dotnet","clean-architecture"]',                                                                                                                                                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P13, @FPostPubAt,     N'all', NULL,                                                                                                                                                                                                    NULL, NULL, CAST(N'2026-03-16T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- P14: Flutter ile Cross-Platform Uygulama Geliştirme (no series, featured=0, cat=Mobil)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'flutter-ile-cross-platform-uygulama', N'published', @AdminId, @AdminId, CAST(N'2026-02-25T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @P14 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId, @P14, @FPostTitle,     N'tr',  N'Flutter ile Cross-Platform Uygulama Geliştirme',                                                                                                                                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostTitle,     N'en',  N'Cross-Platform App Development with Flutter',                                                                                                                                                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostSummary,   N'tr',  N'Flutter ve Dart ile iOS ve Android için tek kod tabanından native performanslı uygulama geliştirme.',                                                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostSummary,   N'en',  N'Developing native-performance apps for iOS and Android from a single codebase with Flutter and Dart.',                                                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostBody,      N'tr',  N'<p>Flutter, tek bir kod tabanından hem iOS hem Android için yüksek performanslı uygulamalar geliştirmenizi sağlar. Bu yazıda temel widget sistemi ve state management konularını ele alıyoruz.</p>',           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostBody,      N'en',  N'<p>Flutter allows you to develop high-performance apps for both iOS and Android from a single codebase. This post covers the basic widget system and state management topics.</p>',                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostReadMin,   N'all', NULL,                                                                                                                                                                                                            10,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostViewCount, N'all', NULL,                                                                                                                                                                                                            0,    NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostFeatured,  N'all', NULL,                                                                                                                                                                                                            NULL, 0,    NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostTags,      N'all', N'["mobile","flutter","dart"]',                                                                                                                                                                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @P14, @FPostPubAt,     N'all', NULL,                                                                                                                                                                                                            NULL, NULL, CAST(N'2026-02-25T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 16: VİDEOLAR (8 adet)
  -- ════════════════════════════════════════════════════════════════
  DECLARE @V1 INT, @V2 INT, @V3 INT, @V4 INT, @V5 INT, @V6 INT, @V7 INT, @V8 INT;
  DECLARE @Exp1 INT, @Exp2 INT, @Exp3 INT;
  DECLARE @Edu1 INT, @Edu2 INT;
  DECLARE @SK1 INT, @SK2 INT, @SK3 INT, @SK4 INT, @SK5 INT;
  DECLARE @Bk1 INT, @Bk2 INT, @Bk3 INT, @Bk4 INT, @Bk5 INT, @Bk6 INT, @Bk7 INT;
  DECLARE @Mv1 INT, @Mv2 INT, @Mv3 INT, @Mv4 INT, @Mv5 INT;
  DECLARE @Act1 INT, @Act2 INT, @Act3 INT, @Act4 INT;
  DECLARE @About1 INT;

  -- V1: kubernetes-intro → VL2 (DevOps & Altyapı)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'kubernetes-intro', N'published', @AdminId, @AdminId, CAST(N'2026-01-15T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V1, @FVidTitle,    N'tr',  N'Kubernetes''e Giriş',                                                                                  NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V1, @FVidTitle,    N'en',  N'Introduction to Kubernetes',                                                                           NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V1, @FVidYtId,     N'all', N'aqz-KE-bpKQ',                                                                                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V1, @FVidDesc,     N'tr',  N'Kubernetes temel kavramlar, pod, deployment ve service objelerini keşfediyoruz.',                       NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V1, @FVidDesc,     N'en',  N'Exploring Kubernetes core concepts: pods, deployments and service objects.',                            NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V1, @FVidDuration, N'all', N'1:12:20',                                                                                              NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V1, @FVidTags,     N'all', N'["devops","kubernetes","containers"]',                                                                  NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V1, @FVidPubAt,    N'all', NULL,                                                                                                    NULL, NULL, CAST(N'2026-01-15T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- V2: docker-containers-explained → VL2
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'docker-containers-explained', N'published', @AdminId, @AdminId, CAST(N'2025-12-10T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V2, @FVidTitle,    N'tr',  N'Docker Container''lar Açıklandı',                                                                      NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V2, @FVidTitle,    N'en',  N'Docker Containers Explained',                                                                          NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V2, @FVidYtId,     N'all', N'Gjnup-PuquQ',                                                                                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V2, @FVidDesc,     N'tr',  N'Docker imaj yapısı, katmanlar ve container yaşam döngüsü detaylı inceleme.',                           NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V2, @FVidDesc,     N'en',  N'Detailed examination of Docker image structure, layers and container lifecycle.',                       NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V2, @FVidDuration, N'all', N'45:30',                                                                                                NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V2, @FVidTags,     N'all', N'["devops","docker","containers"]',                                                                     NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V2, @FVidPubAt,    N'all', NULL,                                                                                                    NULL, NULL, CAST(N'2025-12-10T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- V3: postgresql-advanced-queries → VL3 (content_items INSERT start; SET @V3 comes from part_c.sql)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'postgresql-advanced-queries', N'published', @AdminId, @AdminId, CAST(N'2025-11-20T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V3, @FVidTitle,    N'tr',  N'PostgreSQL İleri Seviye Sorgular',                                                                     NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V3, @FVidTitle,    N'en',  N'PostgreSQL Advanced Queries',                                                                          NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V3, @FVidYtId,     N'all', N'lYRn5HQcRpc',                                                                                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V3, @FVidDesc,     N'tr',  N'CTE, window fonksiyonlar, lateral join ve recursive sorgularla güçlü PostgreSQL kullanımı.',           NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V3, @FVidDesc,     N'en',  N'Powerful PostgreSQL usage with CTEs, window functions, lateral join and recursive queries.',            NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V3, @FVidDuration, N'all', N'55:10',                                                                                                NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V3, @FVidTags,     N'all', N'["database","postgresql","sql"]',                                                                      NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V3, @FVidPubAt,    N'all', NULL,                                                                                                    NULL, NULL, CAST(N'2025-11-20T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- V4: kafka-dagitik-mesajlasma
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'kafka-dagitik-mesajlasma', N'published', @AdminId, @AdminId, CAST(N'2025-10-05T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V4, @FVidTitle,    N'tr',  N'Kafka ile Dağıtık Mesajlaşma',                                                                        NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V4, @FVidTitle,    N'en',  N'Distributed Messaging with Kafka',                                                                     NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V4, @FVidYtId,     N'all', N'2Ggf9dShJvA',                                                                                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V4, @FVidDesc,     N'tr',  N'Apache Kafka topic, partition, consumer group ve exactly-once semantics konuları.',                    NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V4, @FVidDesc,     N'en',  N'Apache Kafka topics, partitions, consumer groups and exactly-once semantics.',                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V4, @FVidDuration, N'all', N'42:50',                                                                                                NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V4, @FVidTags,     N'all', N'["backend","kafka","messaging"]',                                                                      NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V4, @FVidPubAt,    N'all', NULL,                                                                                                    NULL, NULL, CAST(N'2025-10-05T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- V5: kubernetes-ileri-kavramlar
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'kubernetes-ileri-kavramlar', N'published', @AdminId, @AdminId, CAST(N'2025-09-18T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V5 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V5, @FVidTitle,    N'tr',  N'Kubernetes İleri Kavramlar',                                                                          NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V5, @FVidTitle,    N'en',  N'Kubernetes Advanced Concepts',                                                                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V5, @FVidYtId,     N'all', N'X48VuDVv0do',                                                                                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V5, @FVidDesc,     N'tr',  N'StatefulSet, DaemonSet, resource limits, HPA ve Kubernetes operatörleri.',                            NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V5, @FVidDesc,     N'en',  N'StatefulSet, DaemonSet, resource limits, HPA and Kubernetes operators.',                               NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V5, @FVidDuration, N'all', N'1:05:30',                                                                                             NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V5, @FVidTags,     N'all', N'["devops","kubernetes","advanced"]',                                                                   NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V5, @FVidPubAt,    N'all', NULL,                                                                                                    NULL, NULL, CAST(N'2025-09-18T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- V6: makine-ogrenmesi-temelleri
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'makine-ogrenmesi-temelleri', N'published', @AdminId, @AdminId, CAST(N'2025-08-22T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V6 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V6, @FVidTitle,    N'tr',  N'Makine Öğrenmesi Temelleri',                                                                          NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V6, @FVidTitle,    N'en',  N'Machine Learning Fundamentals',                                                                        NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V6, @FVidYtId,     N'all', N'MnfitTTcqpE',                                                                                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V6, @FVidDesc,     N'tr',  N'Denetimli ve denetimsiz öğrenme, model değerlendirme ve scikit-learn uygulamaları.',                  NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V6, @FVidDesc,     N'en',  N'Supervised and unsupervised learning, model evaluation and scikit-learn applications.',                NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V6, @FVidDuration, N'all', N'38:20',                                                                                               NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V6, @FVidTags,     N'all', N'["ai","machine-learning","python"]',                                                                   NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V6, @FVidPubAt,    N'all', NULL,                                                                                                    NULL, NULL, CAST(N'2025-08-22T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- V7: react-performans-optimizasyonu
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'react-performans-optimizasyonu', N'published', @AdminId, @AdminId, CAST(N'2025-07-30T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V7 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V7, @FVidTitle,    N'tr',  N'React Performans Optimizasyonu',                                                                                           NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V7, @FVidTitle,    N'en',  N'React Performance Optimization',                                                                                           NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V7, @FVidYtId,     N'all', N'bUHFg8CZgZU',                                                                                                             NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V7, @FVidDesc,     N'tr',  N'React.memo, useMemo, useCallback, code splitting ve lazy loading ile performans artırma.',                                 NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V7, @FVidDesc,     N'en',  N'Performance improvements with React.memo, useMemo, useCallback, code splitting and lazy loading.',                         NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V7, @FVidDuration, N'all', N'30:15',                                                                                                                   NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V7, @FVidTags,     N'all', N'["frontend","react","performance"]',                                                                                       NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V7, @FVidPubAt,    N'all', NULL,                                                                                                                        NULL, NULL, CAST(N'2025-07-30T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

  -- V8: sistem-tasarimi-temelleri
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVid, N'sistem-tasarimi-temelleri', N'published', @AdminId, @AdminId, CAST(N'2025-06-15T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE(), 0);
  SET @V8 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @V8, @FVidTitle,    N'tr',  N'Sistem Tasarımı Temelleri',                                                                           NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V8, @FVidTitle,    N'en',  N'System Design Fundamentals',                                                                          NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V8, @FVidYtId,     N'all', N'sVcwVQRHIc8',                                                                                        NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V8, @FVidDesc,     N'tr',  N'Yük dengeleme, önbellekleme, veritabanı sharding ve mikroservis mimari prensipleri.',                 NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V8, @FVidDesc,     N'en',  N'Load balancing, caching, database sharding and microservice architecture principles.',                NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V8, @FVidDuration, N'all', N'58:40',                                                                                               NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V8, @FVidTags,     N'all', N'["system-design","architecture","scalability"]',                                                       NULL, NULL, NULL,                                       GETUTCDATE(), GETUTCDATE()),
    (@TId, @V8, @FVidPubAt,    N'all', NULL,                                                                                                    NULL, NULL, CAST(N'2025-06-15T09:00:00' AS DATETIME2), GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 17: Deneyimler (3 adet)
-- ============================================================

  -- Exp1: techweb-senior-backend (current job — no end date)
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtExp, N'techweb-senior-backend', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Exp1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Exp1, @FExpCo,    N'all', N'TechWeb Yazılım A.Ş.',                                                                                                                                           NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp1, @FExpRole,  N'tr',  N'Kıdemli Backend Developer',                                                                                                                                     NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp1, @FExpRole,  N'en',  N'Senior Backend Developer',                                                                                                                                      NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp1, @FExpStart, N'all', NULL,                                                                                                                                                              NULL, NULL, CAST(N'2022-03-01' AS DATETIME2),       GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp1, @FExpDesc,  N'tr',  N'Kurumsal e-ticaret platformunun backend mimarisini yönetiyorum. ASP.NET Core, mikroservis ve Kubernetes altyapısı kurdum.',                                     NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp1, @FExpDesc,  N'en',  N'Managing the backend architecture of an enterprise e-commerce platform. Built ASP.NET Core microservice and Kubernetes infrastructure.',                        NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp1, @FExpSort,  N'all', NULL,                                                                                                                                                              1,    NULL, NULL,                                    GETUTCDATE(), GETUTCDATE());

  -- Exp2: datasoft-backend-developer
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtExp, N'datasoft-backend-developer', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Exp2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Exp2, @FExpCo,    N'all', N'DataSoft Ltd.',                                                                                                                                                  NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp2, @FExpRole,  N'tr',  N'Backend Developer',                                                                                                                                             NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp2, @FExpRole,  N'en',  N'Backend Developer',                                                                                                                                             NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp2, @FExpStart, N'all', NULL,                                                                                                                                                              NULL, NULL, CAST(N'2019-06-01' AS DATETIME2),       GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp2, @FExpEnd,   N'all', NULL,                                                                                                                                                              NULL, NULL, CAST(N'2022-02-28' AS DATETIME2),       GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp2, @FExpDesc,  N'tr',  N'Fintech startup''ında ödeme sistemleri ve API entegrasyonları geliştirdim. Go ve PostgreSQL kullandım.',                                                        NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp2, @FExpDesc,  N'en',  N'Developed payment systems and API integrations at a fintech startup. Used Go and PostgreSQL.',                                                                   NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp2, @FExpSort,  N'all', NULL,                                                                                                                                                              2,    NULL, NULL,                                    GETUTCDATE(), GETUTCDATE());

  -- Exp3: startup-junior-developer
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtExp, N'startup-junior-developer', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Exp3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Exp3, @FExpCo,    N'all', N'StartupXYZ',                                                                                                                                                    NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp3, @FExpRole,  N'tr',  N'Junior Developer',                                                                                                                                              NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp3, @FExpRole,  N'en',  N'Junior Developer',                                                                                                                                              NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp3, @FExpStart, N'all', NULL,                                                                                                                                                              NULL, NULL, CAST(N'2017-09-01' AS DATETIME2),       GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp3, @FExpEnd,   N'all', NULL,                                                                                                                                                              NULL, NULL, CAST(N'2019-05-31' AS DATETIME2),       GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp3, @FExpDesc,  N'tr',  N'Web uygulamaları ve REST API geliştirme. Python, Django ve PostgreSQL ile çalıştım.',                                                                           NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp3, @FExpDesc,  N'en',  N'Developed web applications and REST APIs. Worked with Python, Django and PostgreSQL.',                                                                           NULL, NULL, NULL,                                    GETUTCDATE(), GETUTCDATE()),
    (@TId, @Exp3, @FExpSort,  N'all', NULL,                                                                                                                                                              3,    NULL, NULL,                                    GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 18: Eğitimler (2 adet)
-- ============================================================

  -- Edu1: odtu-bilgisayar-muhendisligi
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtEdu, N'odtu-bilgisayar-muhendisligi', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Edu1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Edu1, @FEduInst,   N'all', N'Orta Doğu Teknik Üniversitesi',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu1, @FEduDegree, N'tr',  N'Bilgisayar Mühendisliği Lisans',  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu1, @FEduDegree, N'en',  N'BSc Computer Engineering',         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu1, @FEduField,  N'tr',  N'Mühendislik',                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu1, @FEduField,  N'en',  N'Engineering',                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu1, @FEduStartY, N'all', NULL,                                2013, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu1, @FEduEndY,   N'all', NULL,                                2017, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu1, @FEduSort,   N'all', NULL,                                1,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Edu2: ankara-fen-lisesi
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtEdu, N'ankara-fen-lisesi', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Edu2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Edu2, @FEduInst,   N'all', N'Ankara Fen Lisesi',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu2, @FEduDegree, N'tr',  N'Lise Diploması',       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu2, @FEduDegree, N'en',  N'High School Diploma',  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu2, @FEduField,  N'tr',  N'Fen Bilimleri',        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu2, @FEduField,  N'en',  N'Sciences',             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu2, @FEduStartY, N'all', NULL,                    2009, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu2, @FEduEndY,   N'all', NULL,                    2013, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Edu2, @FEduSort,   N'all', NULL,                    2,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 19: Yetkinlik Grupları (5 adet)
-- ============================================================

  -- SK1: backend-skills
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSK, N'backend-skills', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @SK1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @SK1, @FSKName,  N'tr',  N'Backend',                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK1, @FSKName,  N'en',  N'Backend',                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK1, @FSKItems, N'all', N'["C#",".NET","Go","Python","Node.js","REST API","gRPC"]', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK1, @FSKSort,  N'all', NULL,                                                    1,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- SK2: frontend-skills
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSK, N'frontend-skills', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @SK2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @SK2, @FSKName,  N'tr',  N'Frontend',                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK2, @FSKName,  N'en',  N'Frontend',                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK2, @FSKItems, N'all', N'["TypeScript","React","Next.js","Tailwind CSS","HTML5","CSS3"]',               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK2, @FSKSort,  N'all', NULL,                                                                            2,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- SK3: devops-skills
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSK, N'devops-skills', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @SK3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @SK3, @FSKName,  N'tr',  N'DevOps',                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK3, @FSKName,  N'en',  N'DevOps',                                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK3, @FSKItems, N'all', N'["Docker","Kubernetes","GitHub Actions","Terraform","Linux","Nginx"]',           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK3, @FSKSort,  N'all', NULL,                                                                              3,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- SK4: database-skills
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSK, N'database-skills', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @SK4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @SK4, @FSKName,  N'tr',  N'Veritabanı',                                                                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK4, @FSKName,  N'en',  N'Database',                                                                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK4, @FSKItems, N'all', N'["PostgreSQL","SQL Server","Redis","MongoDB","Elasticsearch"]',                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK4, @FSKSort,  N'all', NULL,                                                                              4,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- SK5: other-skills
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtSK, N'other-skills', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @SK5 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @SK5, @FSKName,  N'tr',  N'Diğer',                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK5, @FSKName,  N'en',  N'Other',                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK5, @FSKItems, N'all', N'["Git","Agile/Scrum","System Design","DDD","TDD"]',    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @SK5, @FSKSort,  N'all', NULL,                                                    5,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 20: Kitaplar (7 adet)
-- ============================================================

  -- Bk1: clean-code
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtBk, N'clean-code', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Bk1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Bk1, @FBkTitle,    N'all', N'Clean Code',           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk1, @FBkAuthor,   N'all', N'Robert C. Martin',     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk1, @FBkYear,     N'all', NULL,                    2008, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk1, @FBkStatus,   N'all', N'read',                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk1, @FBkProgress, N'all', NULL,                    100,  NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk1, @FBkSort,     N'all', NULL,                    1,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Bk2: pragmatik-programci
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtBk, N'pragmatik-programci', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Bk2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Bk2, @FBkTitle,    N'all', N'Pragmatik Programcı',              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk2, @FBkAuthor,   N'all', N'David Thomas & Andrew Hunt',       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk2, @FBkYear,     N'all', NULL,                                1999, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk2, @FBkStatus,   N'all', N'read',                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk2, @FBkProgress, N'all', NULL,                                100,  NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk2, @FBkSort,     N'all', NULL,                                2,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Bk3: sapiens
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtBk, N'sapiens', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Bk3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Bk3, @FBkTitle,    N'all', N'Sapiens',                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk3, @FBkAuthor,   N'all', N'Yuval Noah Harari',      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk3, @FBkYear,     N'all', NULL,                      2011, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk3, @FBkStatus,   N'all', N'read',                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk3, @FBkProgress, N'all', NULL,                      100,  NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk3, @FBkSort,     N'all', NULL,                      3,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Bk4: dune
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtBk, N'dune', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Bk4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Bk4, @FBkTitle,    N'all', N'Dune',            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk4, @FBkAuthor,   N'all', N'Frank Herbert',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk4, @FBkYear,     N'all', NULL,               1965, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk4, @FBkStatus,   N'all', N'read',            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk4, @FBkProgress, N'all', NULL,               100,  NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk4, @FBkSort,     N'all', NULL,               4,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Bk5: hayvan-ciftligi
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtBk, N'hayvan-ciftligi', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Bk5 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Bk5, @FBkTitle,    N'all', N'Hayvan Çiftliği',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk5, @FBkAuthor,   N'all', N'George Orwell',     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk5, @FBkYear,     N'all', NULL,                 1945, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk5, @FBkStatus,   N'all', N'read',              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk5, @FBkProgress, N'all', NULL,                 100,  NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk5, @FBkSort,     N'all', NULL,                 5,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Bk6: designing-data-intensive-applications
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtBk, N'designing-data-intensive-applications', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Bk6 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Bk6, @FBkTitle,    N'all', N'Designing Data-Intensive Applications',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk6, @FBkAuthor,   N'all', N'Martin Kleppmann',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk6, @FBkYear,     N'all', NULL,                                       2017, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk6, @FBkStatus,   N'all', N'reading',                                 NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk6, @FBkProgress, N'all', NULL,                                       60,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk6, @FBkSort,     N'all', NULL,                                       6,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Bk7: atomik-aliskanliklar
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtBk, N'atomik-aliskanliklar', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Bk7 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Bk7, @FBkTitle,    N'all', N'Atomik Alışkanlıklar',   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk7, @FBkAuthor,   N'all', N'James Clear',            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk7, @FBkYear,     N'all', NULL,                      2018, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk7, @FBkStatus,   N'all', N'reading',                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk7, @FBkProgress, N'all', NULL,                      30,   NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Bk7, @FBkSort,     N'all', NULL,                      7,    NULL, NULL, GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 21: Film & Diziler (5 adet)
-- ============================================================

  -- Mv1: inception
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtMv, N'inception', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Mv1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Mv1, @FMvTitle,   N'all', N'Inception',                                                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv1, @FMvCreator, N'all', N'Christopher Nolan',                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv1, @FMvYear,    N'all', NULL,                                                                2010, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv1, @FMvType,    N'all', N'film',                                                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv1, @FMvNote,    N'tr',  N'Düşlerin katmanları arasında kaybolan bir zihinsel yolculuk.',     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv1, @FMvNote,    N'en',  N'A mental journey lost in layers of dreams.',                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Mv2: the-matrix
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtMv, N'the-matrix', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Mv2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Mv2, @FMvTitle,   N'all', N'The Matrix',                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv2, @FMvCreator, N'all', N'Wachowski Kardeşler',                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv2, @FMvYear,    N'all', NULL,                                                               1999, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv2, @FMvType,    N'all', N'film',                                                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv2, @FMvNote,    N'tr',  N'Gerçeklik algısını sorgulatan efsanevi bilim kurgu.',              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv2, @FMvNote,    N'en',  N'Legendary sci-fi that questions the perception of reality.',       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Mv3: breaking-bad
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtMv, N'breaking-bad', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Mv3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Mv3, @FMvTitle,   N'all', N'Breaking Bad',                                                                                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv3, @FMvCreator, N'all', N'Vince Gilligan',                                                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv3, @FMvYear,    N'all', NULL,                                                                                            2008, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv3, @FMvType,    N'all', N'dizi',                                                                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv3, @FMvNote,    N'tr',  N'Bir kimya öğretmeninin dönüşüm hikayesi. TV tarihinin en iyi yazımı.',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv3, @FMvNote,    N'en',  N'The transformation story of a chemistry teacher. The best writing in TV history.',               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Mv4: interstellar
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtMv, N'interstellar', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Mv4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Mv4, @FMvTitle,   N'all', N'Interstellar',                                                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv4, @FMvCreator, N'all', N'Christopher Nolan',                                                                     NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv4, @FMvYear,    N'all', NULL,                                                                                    2014, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv4, @FMvType,    N'all', N'film',                                                                                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv4, @FMvNote,    N'tr',  N'Zaman ve uzay boyutlarında insanlığın geleceği arayışı.',                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv4, @FMvNote,    N'en',  N'The search for humanity''s future across time and space dimensions.',                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Mv5: black-mirror
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtMv, N'black-mirror', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Mv5 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Mv5, @FMvTitle,   N'all', N'Black Mirror',                                                                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv5, @FMvCreator, N'all', N'Charlie Brooker',                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv5, @FMvYear,    N'all', NULL,                                                                                    2011, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv5, @FMvType,    N'all', N'dizi',                                                                                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv5, @FMvNote,    N'tr',  N'Teknolojinin karanlık yüzüne ayna tutan distopik hikayeler.',                           NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Mv5, @FMvNote,    N'en',  N'Dystopian stories that mirror the dark side of technology.',                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 22: Aktiviteler (4 adet)
-- ============================================================

  -- Act1: fotografcilik
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAct, N'fotografcilik', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Act1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Act1, @FActName, N'tr',  N'Fotoğrafçılık',                                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act1, @FActName, N'en',  N'Photography',                                                                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act1, @FActIcon, N'all', N'bi-camera',                                                                    NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act1, @FActDesc, N'tr',  N'Şehir ve doğa fotoğrafçılığı ile anları ölümsüzleştiriyorum.',                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act1, @FActDesc, N'en',  N'I immortalize moments through urban and nature photography.',                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Act2: dag-yuruyusu
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAct, N'dag-yuruyusu', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Act2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Act2, @FActName, N'tr',  N'Dağ Yürüyüşü',                                                                NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act2, @FActName, N'en',  N'Hiking',                                                                       NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act2, @FActIcon, N'all', N'bi-geo-alt',                                                                   NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act2, @FActDesc, N'tr',  N'Hafta sonları doğa yürüyüşleriyle zihin temizliyorum.',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act2, @FActDesc, N'en',  N'I clear my mind with nature hikes on weekends.',                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Act3: satranc
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAct, N'satranc', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Act3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Act3, @FActName, N'tr',  N'Satranç',                                                                      NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act3, @FActName, N'en',  N'Chess',                                                                         NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act3, @FActIcon, N'all', N'bi-grid-3x3',                                                                  NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act3, @FActDesc, N'tr',  N'Stratejik düşünme ve problem çözme için satranç.',                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act3, @FActDesc, N'en',  N'Chess for strategic thinking and problem solving.',                             NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  -- Act4: podcast
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAct, N'podcast', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @Act4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @Act4, @FActName, N'tr',  N'Podcast Dinleme',                                                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act4, @FActName, N'en',  N'Podcast Listening',                                                                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act4, @FActIcon, N'all', N'bi-headphones',                                                                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act4, @FActDesc, N'tr',  N'Teknoloji ve yazılım konularında güncel kalmak için podcast takibi.',                          NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @Act4, @FActDesc, N'en',  N'Following podcasts to stay current on technology and software topics.',                        NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 23: Hakkımda (singleton)
-- ============================================================

  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'mert-ozen', N'published', @AdminId, @AdminId, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @About1 = SCOPE_IDENTITY();

  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES (@TId, @About1, @FAbName, N'all', N'Mert Özen', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId, @About1, @FAbTitle,    N'tr', N'Yazılım Mühendisi & Teknoloji Meraklısı',                                                                                                                                                                                                                                                                                                              NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @About1, @FAbTitle,    N'en', N'Software Engineer & Technology Enthusiast',                                                                                                                                                                                                                                                                                                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @About1, @FAbBioShort, N'tr', N'Backend sistemler ve bulut altyapısı üzerine çalışan bir yazılım mühendisiyim. Öğrendiğimi paylaşmaktan keyif alıyorum.',                                                                                                                                                                                                                            NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE()),
    (@TId, @About1, @FAbBioShort, N'en', N'A software engineer working on backend systems and cloud infrastructure. I enjoy sharing what I learn.',                                                                                                                                                                                                                                               NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES (@TId, @About1, @FAbBioLong, N'tr', N'<p>7+ yıldır yazılım geliştiriyorum. Kariyer yolculuğum boyunca fintech''ten e-ticarete kadar farklı sektörlerde çalışma fırsatı buldum.</p><p>ASP.NET Core, Go ve TypeScript günlük kullandığım diller. Kubernetes üzerinde çalışan mikroservis mimarileri ve dağıtık sistemler özellikle ilgi alanım.</p><p>Bu blog, öğrendiklerimi sistematik hale getirdiğim ve paylaştığım bir alan. Umarım faydalı olur.</p>', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES (@TId, @About1, @FAbBioLong, N'en', N'<p>I have been developing software for 7+ years. Throughout my career I have had the opportunity to work in various sectors from fintech to e-commerce.</p><p>ASP.NET Core, Go and TypeScript are the languages I use daily. Microservice architectures running on Kubernetes and distributed systems are my special area of interest.</p><p>This blog is a place where I systematize and share what I learn. I hope it is useful.</p>', NULL, NULL, NULL, GETUTCDATE(), GETUTCDATE());

-- ============================================================
-- ADIM 24: content_field_relations
-- ============================================================

  -- Post categories
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @P1,  @FPostCat, @CatOPS, 0),
    (@TId, @P2,  @FPostCat, @CatFE,  0),
    (@TId, @P3,  @FPostCat, @CatDB,  0),
    (@TId, @P4,  @FPostCat, @CatOPS, 0),
    (@TId, @P5,  @FPostCat, @CatDB,  0),
    (@TId, @P6,  @FPostCat, @CatDB,  0),
    (@TId, @P7,  @FPostCat, @CatOPS, 0),
    (@TId, @P8,  @FPostCat, @CatBE,  0),
    (@TId, @P9,  @FPostCat, @CatDB,  0),
    (@TId, @P10, @FPostCat, @CatSYS, 0),
    (@TId, @P11, @FPostCat, @CatFE,  0),
    (@TId, @P12, @FPostCat, @CatDB,  0),
    (@TId, @P13, @FPostCat, @CatBE,  0),
    (@TId, @P14, @FPostCat, @CatMOB, 0);

  -- Post series (only posts with a series)
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @P1, @FPostSeries, @Ser1, 0),
    (@TId, @P5, @FPostSeries, @Ser2, 0),
    (@TId, @P6, @FPostSeries, @Ser2, 0),
    (@TId, @P8, @FPostSeries, @Ser1, 0),
    (@TId, @P9, @FPostSeries, @Ser1, 0);

  -- Video -> video-list
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @V1, @FVidList, @VL2, 0),
    (@TId, @V2, @FVidList, @VL2, 0),
    (@TId, @V3, @FVidList, @VL3, 0),
    (@TId, @V4, @FVidList, @VL3, 0),
    (@TId, @V5, @FVidList, @VL2, 0),
    (@TId, @V6, @FVidList, @VL3, 0),
    (@TId, @V7, @FVidList, @VL4, 0),
    (@TId, @V8, @FVidList, @VL1, 0);

  -- About: experiences
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @About1, @FAbExp, @Exp1, 1),
    (@TId, @About1, @FAbExp, @Exp2, 2),
    (@TId, @About1, @FAbExp, @Exp3, 3);

  -- About: educations
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @About1, @FAbEdu, @Edu1, 1),
    (@TId, @About1, @FAbEdu, @Edu2, 2);

  -- About: skill groups
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @About1, @FAbSK, @SK1, 1),
    (@TId, @About1, @FAbSK, @SK2, 2),
    (@TId, @About1, @FAbSK, @SK3, 3),
    (@TId, @About1, @FAbSK, @SK4, 4),
    (@TId, @About1, @FAbSK, @SK5, 5);

  -- About: books
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @About1, @FAbBk, @Bk1, 1),
    (@TId, @About1, @FAbBk, @Bk2, 2),
    (@TId, @About1, @FAbBk, @Bk3, 3),
    (@TId, @About1, @FAbBk, @Bk4, 4),
    (@TId, @About1, @FAbBk, @Bk5, 5),
    (@TId, @About1, @FAbBk, @Bk6, 6),
    (@TId, @About1, @FAbBk, @Bk7, 7);

  -- About: movies
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @About1, @FAbMv, @Mv1, 1),
    (@TId, @About1, @FAbMv, @Mv2, 2),
    (@TId, @About1, @FAbMv, @Mv3, 3),
    (@TId, @About1, @FAbMv, @Mv4, 4),
    (@TId, @About1, @FAbMv, @Mv5, 5);

  -- About: activities
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order) VALUES
    (@TId, @About1, @FAbAct, @Act1, 1),
    (@TId, @About1, @FAbAct, @Act2, 2),
    (@TId, @About1, @FAbAct, @Act3, 3),
    (@TId, @About1, @FAbAct, @Act4, 4);

-- ============================================================
-- ADIM 25: content_item_titles
-- ============================================================

  -- 25a: Site Settings (localized @FSsTitle)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FSsTitle AND tenant_id = @TId;

  -- 25b: Categories (localized @FCatName)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FCatName AND tenant_id = @TId;

  -- 25c: Series (localized @FSerName)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FSerName AND tenant_id = @TId;

  -- 25d: Posts (localized @FPostTitle)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FPostTitle AND tenant_id = @TId;

  -- 25e: Videos (localized @FVidTitle)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FVidTitle AND tenant_id = @TId;

  -- 25f: About (non-localized @FAbName -> CROSS JOIN with active languages)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT cfv.tenant_id, cfv.content_item_id, l.code, cfv.value_text, 1
  FROM content_field_values cfv
  CROSS JOIN (SELECT code FROM languages WHERE tenant_id = @TId AND is_active = 1) l
  WHERE cfv.content_field_id = @FAbName AND cfv.language_code = N'all' AND cfv.tenant_id = @TId;

  -- 25g: Video Lists (localized @FVLName)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FVLName AND tenant_id = @TId;

  -- 25h: Experiences (localized @FExpRole)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FExpRole AND tenant_id = @TId;

  -- 25i: Educations (localized @FEduDegree)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FEduDegree AND tenant_id = @TId;

  -- 25j: Skill Groups (localized @FSKName)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FSKName AND tenant_id = @TId;

  -- 25k: Books (non-localized @FBkTitle -> CROSS JOIN with active languages)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT cfv.tenant_id, cfv.content_item_id, l.code, cfv.value_text, 1
  FROM content_field_values cfv
  CROSS JOIN (SELECT code FROM languages WHERE tenant_id = @TId AND is_active = 1) l
  WHERE cfv.content_field_id = @FBkTitle AND cfv.language_code = N'all' AND cfv.tenant_id = @TId;

  -- 25l: Movies (non-localized @FMvTitle -> CROSS JOIN with active languages)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT cfv.tenant_id, cfv.content_item_id, l.code, cfv.value_text, 1
  FROM content_field_values cfv
  CROSS JOIN (SELECT code FROM languages WHERE tenant_id = @TId AND is_active = 1) l
  WHERE cfv.content_field_id = @FMvTitle AND cfv.language_code = N'all' AND cfv.tenant_id = @TId;

  -- 25m: Activities (localized @FActName)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active)
  SELECT tenant_id, content_item_id, language_code, value_text, 1
  FROM content_field_values
  WHERE content_field_id = @FActName AND tenant_id = @TId;

-- ============================================================
-- ADIM 26: Final
-- ============================================================
  PRINT '=== Mert Özen Seed v3 Tamamlandı ===';
  PRINT '13 content type: site-settings, category, series, post, video, about, video-list, experience, education, skill-group, book, movie, activity';
  PRINT 'API Key: vk_1_M3rtOzen_SecretKey2026!';
  PRINT 'API Base: http://localhost:5267/api/v1/mert-ozen/';

  COMMIT TRANSACTION;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  DECLARE @ErrMsg  NVARCHAR(4000) = ERROR_MESSAGE();
  DECLARE @ErrSev  INT            = ERROR_SEVERITY();
  DECLARE @ErrStt  INT            = ERROR_STATE();
  RAISERROR(@ErrMsg, @ErrSev, @ErrStt);
END CATCH;
