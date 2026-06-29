-- ================================================================
-- 003_mert_ozen_seed.sql
-- Mert Özen kişisel blog platformu — tam sıfırlama + tohum verisi
--
-- ⚠  DİKKAT: Mevcut TÜM tenant/içerik/medya/sözlük/denetim
--    verilerini siler. Sadece geliştirme ortamında çalıştır.
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

  -- ── ADIM 1: Tüm tenant verisini temizle (FK sırasına göre) ────
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
  -- parent_id self-FK: önce null yap
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

  DBCC CHECKIDENT ('tenants',              RESEED, 0);
  DBCC CHECKIDENT ('users',                RESEED, 0);
  DBCC CHECKIDENT ('languages',            RESEED, 0);
  DBCC CHECKIDENT ('content_types',        RESEED, 0);
  DBCC CHECKIDENT ('content_fields',       RESEED, 0);
  DBCC CHECKIDENT ('content_items',        RESEED, 0);
  DBCC CHECKIDENT ('content_field_values', RESEED, 0);
  DBCC CHECKIDENT ('audit_logs',           RESEED, 0);

  -- ── ADIM 2: Tenant ────────────────────────────────────────────
  DECLARE @TId INT;
  INSERT INTO tenants (name, slug, is_active, created_at, updated_at, is_deleted)
  VALUES (N'Mert Özen', N'mert-ozen', 1, GETUTCDATE(), GETUTCDATE(), 0);
  SET @TId = SCOPE_IDENTITY();

  -- ── ADIM 3: TenantAdmin kullanıcı (şifre: Admin123!) ─────────
  DECLARE @AdminId INT;
  INSERT INTO users (tenant_id, email, password_hash, full_name, role,
                     is_active, created_at, updated_at, is_deleted)
  VALUES (@TId, N'admin@mert-ozen.local',
          N'$2a$12$4lCE3hHfiyShtwR7bC9CG.YVgp5pny2Qx.cSxGzCJkqvXw89r25EW',
          N'Mert Özen', N'TenantAdmin', 1, GETUTCDATE(), GETUTCDATE(), 0);
  SET @AdminId = SCOPE_IDENTITY();

  -- ── ADIM 4: Diller ────────────────────────────────────────────
  INSERT INTO languages (tenant_id, code, name, is_default, is_active, flag_icon)
  VALUES
    (@TId, N'tr', N'Türkçe', 1, 1, N'fi fi-tr'),
    (@TId, N'en', N'English', 0, 1, N'fi fi-gb');

  -- ── ADIM 5: Content Type'lar ─────────────────────────────────
  DECLARE @CtCategory INT, @CtPost INT, @CtVideo INT, @CtAbout INT;

  INSERT INTO content_types (tenant_id, name, slug, description, icon,
                              is_published, sort_order, created_at, updated_at, is_deleted)
  VALUES (@TId, N'Kategori', N'category', N'Blog ve video kategorileri',
          N'bi-tag', 1, 1, GETUTCDATE(), GETUTCDATE(), 0);
  SET @CtCategory = SCOPE_IDENTITY();

  INSERT INTO content_types (tenant_id, name, slug, description, icon,
                              is_published, sort_order, created_at, updated_at, is_deleted)
  VALUES (@TId, N'Blog Yazısı', N'post', N'Teknik blog yazıları',
          N'bi-file-text', 1, 2, GETUTCDATE(), GETUTCDATE(), 0);
  SET @CtPost = SCOPE_IDENTITY();

  INSERT INTO content_types (tenant_id, name, slug, description, icon,
                              is_published, sort_order, created_at, updated_at, is_deleted)
  VALUES (@TId, N'Video', N'video', N'YouTube video içerikleri',
          N'bi-camera-video', 1, 3, GETUTCDATE(), GETUTCDATE(), 0);
  SET @CtVideo = SCOPE_IDENTITY();

  INSERT INTO content_types (tenant_id, name, slug, description, icon,
                              is_published, sort_order, created_at, updated_at, is_deleted)
  VALUES (@TId, N'Hakkımda', N'about', N'Yazar profil sayfası (tek kayıt)',
          N'bi-person-circle', 1, 4, GETUTCDATE(), GETUTCDATE(), 0);
  SET @CtAbout = SCOPE_IDENTITY();

  -- ── ADIM 6: Kategori alanları ────────────────────────────────
  DECLARE @FCatName INT, @FCatAbbr INT, @FCatColor INT, @FCatDesc INT;

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'Ad', N'name', N'Text',
          1, 1, 1, N'{"max_length":100}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FCatName = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'Kısaltma', N'abbreviation', N'Text',
          1, 0, 2, N'{"max_length":5}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FCatAbbr = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'Renk', N'color', N'Color',
          0, 0, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FCatColor = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'Açıklama', N'description', N'Text',
          0, 1, 4, N'{"max_length":300}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FCatDesc = SCOPE_IDENTITY();

  -- ── ADIM 7: Blog Yazısı alanları ─────────────────────────────
  DECLARE @FPostTitle    INT, @FPostSlug       INT, @FPostSummary  INT,
          @FPostBody     INT, @FPostCoverImage INT, @FPostCategory  INT,
          @FPostReadTime INT, @FPostViewCount  INT, @FPostFeatured  INT,
          @FPostSeries   INT, @FPostSeriesOrd  INT, @FPostPubAt    INT;

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Başlık', N'title', N'Text',
          1, 1, 1, N'{"max_length":300}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostTitle = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Slug', N'slug', N'Slug',
          1, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostSlug = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Özet', N'summary', N'Text',
          0, 1, 3, N'{"max_length":500}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostSummary = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'İçerik', N'body', N'RichText',
          0, 1, 4, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostBody = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Kapak Görseli', N'cover-image', N'Image',
          0, 0, 5, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostCoverImage = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order,
                               options_json, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Kategori', N'category', N'Relation',
          1, 0, 6,
          N'{"target_content_type_id":' + CAST(@CtCategory AS NVARCHAR(10))
            + N',"display_field_slug":"name","value_field_slug":"id"}',
          GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostCategory = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Okuma Süresi (dk)', N'read-time-min', N'Number',
          0, 0, 7, N'{"min":1,"max":120}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostReadTime = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Görüntülenme', N'view-count', N'Number',
          0, 0, 8, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostViewCount = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Öne Çıkan', N'is-featured', N'Boolean',
          0, 0, 9, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostFeatured = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Seri Etiketi', N'series-label', N'Text',
          0, 0, 10, N'{"max_length":20,"placeholder":"SERİ 01"}',
          GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostSeries = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Seri Sırası', N'series-order', N'Number',
          0, 0, 11, N'{"min":1,"max":99}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostSeriesOrd = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'Yayın Tarihi', N'published-at', N'DateTime',
          0, 0, 12, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FPostPubAt = SCOPE_IDENTITY();

  -- ── ADIM 8: Video alanları ────────────────────────────────────
  DECLARE @FVidTitle INT, @FVidSlug INT, @FVidThumb INT, @FVidUrl   INT,
          @FVidDur   INT, @FVidViews INT, @FVidPubAt INT;

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'Başlık', N'title', N'Text',
          1, 1, 1, N'{"max_length":300}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FVidTitle = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'Slug', N'slug', N'Slug',
          1, 0, 2, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FVidSlug = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'Kapak Görseli', N'thumbnail', N'Image',
          0, 0, 3, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FVidThumb = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'Video URL', N'video-url', N'URL',
          1, 0, 4, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FVidUrl = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'Süre', N'duration', N'Text',
          0, 0, 5, N'{"max_length":10,"placeholder":"18:24"}',
          GETUTCDATE(), GETUTCDATE(), 0);
  SET @FVidDur = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'İzlenme', N'view-count', N'Number',
          0, 0, 6, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FVidViews = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'Yayın Tarihi', N'published-at', N'DateTime',
          0, 0, 7, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FVidPubAt = SCOPE_IDENTITY();

  -- ── ADIM 9: Hakkımda alanları ─────────────────────────────────
  DECLARE @FAFullName INT, @FATitleLine INT, @FABioShort INT, @FABioLong  INT,
          @FAAvatar   INT, @FALinkedIn  INT, @FAGitHub   INT, @FAYouTube  INT,
          @FAInstagram INT, @FAEmail    INT;

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'Ad Soyad', N'full-name', N'Text',
          1, 0, 1, N'{"max_length":200}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FAFullName = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'Ünvan', N'title-line', N'Text',
          0, 1, 2, N'{"max_length":200}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FATitleLine = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'Kısa Bio', N'bio-short', N'Text',
          0, 1, 3, N'{"max_length":500}', GETUTCDATE(), GETUTCDATE(), 0);
  SET @FABioShort = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'Uzun Bio', N'bio-long', N'RichText',
          0, 1, 4, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FABioLong = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'Profil Fotoğrafı', N'avatar', N'Image',
          0, 0, 5, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FAAvatar = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'LinkedIn URL', N'linkedin-url', N'URL',
          0, 0, 6, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FALinkedIn = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'GitHub URL', N'github-url', N'URL',
          0, 0, 7, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FAGitHub = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'YouTube URL', N'youtube-url', N'URL',
          0, 0, 8, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FAYouTube = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'Instagram URL', N'instagram-url', N'URL',
          0, 0, 9, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FAInstagram = SCOPE_IDENTITY();

  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type,
                               is_required, is_localized, sort_order, options_json,
                               created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'E-posta', N'email-address', N'Email',
          0, 0, 10, NULL, GETUTCDATE(), GETUTCDATE(), 0);
  SET @FAEmail = SCOPE_IDENTITY();

  -- ── ADIM 10a: api_credentials RESEED (varsa) ─────────────────
  -- api_configurations kendine ait RESEED listede, ekle
  IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='api_credentials')
      DBCC CHECKIDENT ('api_credentials', RESEED, 0);
  DBCC CHECKIDENT ('api_configurations', RESEED, 0);

  -- ── ADIM 10: API yapılandırmaları (herkese açık, salt okunur) ─
  INSERT INTO api_configurations
    (tenant_id, content_type_id, is_enabled, auth_type,
     allow_filtering, allow_sorting, allow_pagination,
     is_public, allow_read, allow_create, allow_update, allow_delete,
     rate_limit_per_min, cache_seconds, created_at, updated_at)
  VALUES
    (@TId, @CtCategory, 1, N'None', 1, 1, 1, 1, 1, 0, 0, 0, 120, 300,  GETUTCDATE(), GETUTCDATE()),
    (@TId, @CtPost,     1, N'None', 1, 1, 1, 1, 1, 0, 0, 0, 120, 60,   GETUTCDATE(), GETUTCDATE()),
    (@TId, @CtVideo,    1, N'None', 1, 1, 1, 1, 1, 0, 0, 0, 120, 60,   GETUTCDATE(), GETUTCDATE()),
    (@TId, @CtAbout,    1, N'None', 0, 0, 0, 1, 1, 0, 0, 0, 60,  600,  GETUTCDATE(), GETUTCDATE());

  -- ── ADIM 10b: API alan görünürlüğü (tüm alanlar, alias yok) ──
  -- api_field_visibility yoksa hiç alan dönmez. Tüm content type'ların
  -- tüm alanlarını varsayılan olarak görünür işaretle.
  INSERT INTO api_field_visibility (api_configuration_id, content_field_id, is_visible, response_key_alias)
  SELECT ac.id, cf.id, 1, NULL
  FROM api_configurations ac
  JOIN content_fields cf
    ON cf.content_type_id = ac.content_type_id
   AND cf.is_deleted = 0
   AND cf.tenant_id = ac.tenant_id
  WHERE ac.tenant_id = @TId;

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 11: KATEGORİ İÇERİKLERİ (8 adet)
  -- Non-localized alanlar → language_code = 'all'
  -- Localized alanlar     → language_code = 'tr' VE 'en'
  -- ════════════════════════════════════════════════════════════════
  DECLARE @CatBE INT, @CatFE INT, @CatOPS INT, @CatDB INT,
          @CatAI INT, @CatMOB INT, @CatSEC INT, @CatSYS INT;

  -- Backend
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'backend', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatBE = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatBE, @FCatName, N'tr', N'Backend',  GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatBE, @FCatName, N'en', N'Backend',  GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatBE, @FCatAbbr, N'all', N'BE',      GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatBE, @FCatColor, N'all', N'#3B82F6', GETUTCDATE(), GETUTCDATE());

  -- Frontend
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'frontend', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatFE = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatFE, @FCatName, N'tr', N'Frontend', GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatFE, @FCatName, N'en', N'Frontend', GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatFE, @FCatAbbr, N'all', N'FE',      GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatFE, @FCatColor, N'all', N'#8B5CF6', GETUTCDATE(), GETUTCDATE());

  -- DevOps
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'devops', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatOPS = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatOPS, @FCatName, N'tr', N'DevOps',   GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatOPS, @FCatName, N'en', N'DevOps',   GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatOPS, @FCatAbbr, N'all', N'OPS',     GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatOPS, @FCatColor, N'all', N'#10B981', GETUTCDATE(), GETUTCDATE());

  -- Veritabanı
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'veritabani', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatDB = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatDB, @FCatName, N'tr', N'Veritabanı', GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatDB, @FCatName, N'en', N'Database',   GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatDB, @FCatAbbr, N'all', N'DB',        GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatDB, @FCatColor, N'all', N'#F59E0B',  GETUTCDATE(), GETUTCDATE());

  -- Yapay Zeka
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'yapay-zeka', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatAI = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatAI, @FCatName, N'tr', N'Yapay Zeka',           GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatAI, @FCatName, N'en', N'Artificial Intelligence', GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatAI, @FCatAbbr, N'all', N'AI',                  GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatAI, @FCatColor, N'all', N'#EC4899',             GETUTCDATE(), GETUTCDATE());

  -- Mobil
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'mobil', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatMOB = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatMOB, @FCatName, N'tr', N'Mobil',   GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatMOB, @FCatName, N'en', N'Mobile',  GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatMOB, @FCatAbbr, N'all', N'MOB',    GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatMOB, @FCatColor, N'all', N'#6366F1', GETUTCDATE(), GETUTCDATE());

  -- Güvenlik
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'guvenlik', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatSEC = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatSEC, @FCatName, N'tr', N'Güvenlik', GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatSEC, @FCatName, N'en', N'Security', GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatSEC, @FCatAbbr, N'all', N'SEC',     GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatSEC, @FCatColor, N'all', N'#EF4444', GETUTCDATE(), GETUTCDATE());

  -- Sistem
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtCategory, N'sistem', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @CatSYS = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES (@TId, @CatSYS, @FCatName, N'tr', N'Sistem',  GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatSYS, @FCatName, N'en', N'System',  GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatSYS, @FCatAbbr, N'all', N'SYS',    GETUTCDATE(), GETUTCDATE()),
         (@TId, @CatSYS, @FCatColor, N'all', N'#14B8A6', GETUTCDATE(), GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 12: HAKKIMDA
  -- ════════════════════════════════════════════════════════════════
  DECLARE @AboutItem INT;
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtAbout, N'mert-ozen', N'published', @AdminId, @AdminId,
          GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0);
  SET @AboutItem = SCOPE_IDENTITY();

  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, created_at, updated_at)
  VALUES
    (@TId, @AboutItem, @FAFullName,   N'all', N'Mert Özen', GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FATitleLine,  N'tr',  N'Bilgisayar Mühendisi · İçerik Üreticisi', GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FATitleLine,  N'en',  N'Computer Engineer · Content Creator',     GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FABioShort,   N'tr',
     N'Yazılım dünyasını sade ve uygulamalı anlatıyorum. Backend, mimari ve geliştirici verimliliği üzerine yazıyor, video üretiyorum.',
     GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FABioShort,   N'en',
     N'I explain the software world in a clear and practical way. I write and create videos about backend development, architecture, and developer productivity.',
     GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FABioLong,    N'tr',
     N'<p>Yazılım geliştirmeye olan ilgim lisans dönemimde başladı ve o günden bu yana sürekli öğrenme ve üretme üzerine kurulu bir kariyer inşa ettim. Backend sistemler, dağıtık mimari ve geliştirici araçları benim için hem bir iş hem de bir tutku.</p><p>Bu platformda öğrendiklerimi, deneyimlerimi ve yazılım dünyasındaki gözlemlerimi paylaşıyorum. Her içeriği mümkün olduğunca sade, uygulamalı ve gerçek dünya örnekleriyle beslemeye çalışıyorum.</p>',
     GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FABioLong,    N'en',
     N'<p>My passion for software development started during my undergraduate years, and since then I have built a career centered on continuous learning and creation. Backend systems, distributed architecture, and developer tooling are both my work and my passion.</p><p>On this platform I share what I have learned, my experiences, and my observations from the software world — always aiming to keep content clear, practical, and grounded in real-world examples.</p>',
     GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FALinkedIn,   N'all', N'https://linkedin.com/in/mertozen',  GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FAGitHub,     N'all', N'https://github.com/mrtozln1923',    GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FAYouTube,    N'all', N'https://youtube.com/@mertozen',     GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FAInstagram,  N'all', N'https://instagram.com/mertozen',    GETUTCDATE(), GETUTCDATE()),
    (@TId, @AboutItem, @FAEmail,      N'all', N'mertozen.seyirmobil@gmail.com',     GETUTCDATE(), GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 13: BLOG YAZILARI (9 adet)
  -- content_field_relations → kategori bağlantısı
  -- ════════════════════════════════════════════════════════════════
  DECLARE @P1 INT, @P2 INT, @P3 INT, @P4 INT, @P5 INT,
          @P6 INT, @P7 INT, @P8 INT, @P9 INT;

  -- ── P1: Sıfırdan Mikroservis Mimarisi ──────────────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'sifradan-mikroservis-mimarisi', N'published', @AdminId, @AdminId,
          '2026-06-20 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P1,@FPostTitle,    N'tr', N'Sıfırdan Mikroservis Mimarisi', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostTitle,    N'en', N'Microservice Architecture from Scratch', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostSlug,     N'all',N'sifradan-mikroservis-mimarisi', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostSummary,  N'tr', N'Monolitten mikroservise geçişin temellerini, servis sınırlarını ve iletişim desenlerini adım adım ele alıyoruz.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostSummary,  N'en', N'We cover the fundamentals of transitioning from monolith to microservices, service boundaries, and communication patterns step by step.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostBody,     N'tr', N'<h2>Giriş</h2><p>Mikroservis mimarisi, büyük ölçekli sistemleri küçük, bağımsız servisler halinde yapılandırmanın bir yöntemidir.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostBody,     N'en', N'<h2>Introduction</h2><p>Microservice architecture is a method of structuring large-scale systems as small, independent services.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostReadTime, N'all',NULL, 12,   NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostViewCount,N'all',NULL, 18400,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostFeatured, N'all',NULL, NULL, 1,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostSeries,   N'all',N'SERİ 01', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostSeriesOrd,N'all',NULL, 1, NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P1,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-06-20 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P1, @FPostCategory, @CatBE, 0);

  -- ── P2: React 19 ile Gelen Yenilikler ──────────────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'react-19-ile-gelen-yenilikler', N'published', @AdminId, @AdminId,
          '2026-06-18 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P2,@FPostTitle,    N'tr', N'React 19 ile Gelen Yenilikler', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostTitle,    N'en', N'What''s New in React 19', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostSlug,     N'all',N'react-19-ile-gelen-yenilikler', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostSummary,  N'tr', N'Actions, use() kancası ve sunucu bileşenlerinin olgunlaşması ile değişen geliştirme deneyimi.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostSummary,  N'en', N'The evolving developer experience with Actions, the use() hook, and the maturation of Server Components.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostBody,     N'tr', N'<h2>React 19 Neler Getiriyor?</h2><p>React 19, geliştirici deneyimini köklü biçimde değiştiren birçok yeni özellik sunuyor.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostBody,     N'en', N'<h2>What Does React 19 Bring?</h2><p>React 19 introduces several new features that fundamentally change the developer experience.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostReadTime, N'all',NULL, 8,    NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostViewCount,N'all',NULL, 22700,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostFeatured, N'all',NULL, NULL, 1,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P2,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-06-18 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P2, @FPostCategory, @CatFE, 0);

  -- ── P3: Kubernetes'e Pratik Giriş ──────────────────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'kubernetese-pratik-giris', N'published', @AdminId, @AdminId,
          '2026-06-14 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P3,@FPostTitle,    N'tr', N'Kubernetes''e Pratik Giriş', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostTitle,    N'en', N'A Practical Introduction to Kubernetes', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostSlug,     N'all',N'kubernetese-pratik-giris', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostSummary,  N'tr', N'Pod, Deployment ve Service kavramlarını gerçek bir örnek üzerinden anlıyoruz.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostSummary,  N'en', N'Understanding Pods, Deployments, and Services through a real-world example.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostBody,     N'tr', N'<h2>Neden Kubernetes?</h2><p>Kubernetes, konteyner orkestrasyonu için endüstri standardı haline gelmiştir.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostBody,     N'en', N'<h2>Why Kubernetes?</h2><p>Kubernetes has become the industry standard for container orchestration.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostReadTime, N'all',NULL, 14,   NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostViewCount,N'all',NULL, 15300,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostFeatured, N'all',NULL, NULL, 1,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P3,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-06-14 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P3, @FPostCategory, @CatOPS, 0);

  -- ── P4: LLM'leri Üretim Ortamında Çalıştırmak ─────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'llmleri-uretim-ortaminda-calistirmak', N'published', @AdminId, @AdminId,
          '2026-06-22 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P4,@FPostTitle,    N'tr', N'LLM''leri Üretim Ortamında Çalıştırmak', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostTitle,    N'en', N'Running LLMs in Production', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostSlug,     N'all',N'llmleri-uretim-ortaminda-calistirmak', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostSummary,  N'tr', N'Gecikme, maliyet ve güvenilirliği dengeleyen bir servis mimarisi nasıl kurulur?', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostSummary,  N'en', N'How to build a service architecture that balances latency, cost, and reliability.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostBody,     N'tr', N'<h2>Üretimde LLM Zorlukları</h2><p>Bir dil modelini üretime almak, demo geliştirmekten çok daha karmaşık bir süreçtir.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostBody,     N'en', N'<h2>LLM Production Challenges</h2><p>Taking a language model to production is far more complex than building a demo.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostReadTime, N'all',NULL, 13,   NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostViewCount,N'all',NULL, 19600,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostFeatured, N'all',NULL, NULL, 1,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P4,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-06-22 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P4, @FPostCategory, @CatAI, 0);

  -- ── P5: PostgreSQL Performans İpuçları ─────────────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'postgresql-performans-ipuclari', N'published', @AdminId, @AdminId,
          '2026-06-08 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P5 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P5,@FPostTitle,    N'tr', N'PostgreSQL Performans İpuçları', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostTitle,    N'en', N'PostgreSQL Performance Tips', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostSlug,     N'all',N'postgresql-performans-ipuclari', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostSummary,  N'tr', N'Yavaş sorguları teşhis etmek ve veritabanını ölçeklenebilir tutmak için pratik teknikler.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostSummary,  N'en', N'Practical techniques for diagnosing slow queries and keeping your database scalable.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostBody,     N'tr', N'<h2>Yavaş Sorgu Analizi</h2><p>EXPLAIN ANALYZE komutu, sorgu planlarını anlamak için temel araçtır.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostBody,     N'en', N'<h2>Slow Query Analysis</h2><p>The EXPLAIN ANALYZE command is the fundamental tool for understanding query plans.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostReadTime, N'all',NULL, 11,   NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostViewCount,N'all',NULL, 13900,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostFeatured, N'all',NULL, NULL, 0,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostSeries,   N'all',N'SERİ 01', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostSeriesOrd,N'all',NULL, 1, NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P5,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-06-08 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P5, @FPostCategory, @CatDB, 0);

  -- ── P6: Veritabanı Index'leri Nasıl Çalışır? ──────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'veritabani-indexleri-nasil-calisir', N'published', @AdminId, @AdminId,
          '2026-05-20 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P6 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P6,@FPostTitle,    N'tr', N'Veritabanı Index''leri Nasıl Çalışır?', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostTitle,    N'en', N'How Do Database Indexes Work?', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostSlug,     N'all',N'veritabani-indexleri-nasil-calisir', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostSummary,  N'tr', N'B-Tree yapısından kapsayıcı indekslere, doğru indeksi seçmenin mantığı.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostSummary,  N'en', N'From B-Tree structures to covering indexes, the logic behind choosing the right index.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostBody,     N'tr', N'<h2>B-Tree İndeksi</h2><p>Çoğu veritabanının varsayılan indeks türü B-Tree''dir ve dengeli ağaç yapısı üzerine kuruludur.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostBody,     N'en', N'<h2>B-Tree Index</h2><p>The default index type for most databases is B-Tree, built on a balanced tree structure.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostReadTime, N'all',NULL, 9,    NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostViewCount,N'all',NULL, 11200,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostFeatured, N'all',NULL, NULL, 0,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostSeries,   N'all',N'SERİ 02', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostSeriesOrd,N'all',NULL, 2, NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P6,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-05-20 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P6, @FPostCategory, @CatDB, 0);

  -- ── P7: JWT ile Güvenli Kimlik Doğrulama ──────────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'jwt-ile-guvenli-kimlik-dogrulama', N'published', @AdminId, @AdminId,
          '2026-03-30 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P7 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P7,@FPostTitle,    N'tr', N'JWT ile Güvenli Kimlik Doğrulama', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostTitle,    N'en', N'Secure Authentication with JWT', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostSlug,     N'all',N'jwt-ile-guvenli-kimlik-dogrulama', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostSummary,  N'tr', N'Token tabanlı kimlik doğrulamada sık yapılan hatalar ve doğru desenler.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostSummary,  N'en', N'Common mistakes in token-based authentication and the correct patterns to follow.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostBody,     N'tr', N'<h2>JWT Nedir?</h2><p>JSON Web Token, taraflar arasında bilgi güvenli biçimde aktarmak için kullanılan kompakt, URL güvenli bir standarttır.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostBody,     N'en', N'<h2>What is JWT?</h2><p>JSON Web Token is a compact, URL-safe standard for securely transmitting information between parties.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostReadTime, N'all',NULL, 9,    NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostViewCount,N'all',NULL, 10500,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostFeatured, N'all',NULL, NULL, 0,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P7,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-03-30 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P7, @FPostCategory, @CatSEC, 0);

  -- ── P8: Mikroserviste Servisler Arası İletişim ─────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'mikroserviste-servisler-arasi-iletisim', N'published', @AdminId, @AdminId,
          '2026-06-02 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P8 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P8,@FPostTitle,    N'tr', N'Mikroserviste Servisler Arası İletişim', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostTitle,    N'en', N'Inter-Service Communication in Microservices', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostSlug,     N'all',N'mikroserviste-servisler-arasi-iletisim', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostSummary,  N'tr', N'Senkron ve asenkron iletişim arasında seçim yaparken nelere dikkat etmeli?', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostSummary,  N'en', N'What to consider when choosing between synchronous and asynchronous communication.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostBody,     N'tr', N'<h2>Senkron mu, Asenkron mu?</h2><p>Bu seçim, sistemin dayanıklılığını ve ölçeklenebilirliğini doğrudan etkiler.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostBody,     N'en', N'<h2>Synchronous or Asynchronous?</h2><p>This choice directly impacts the resilience and scalability of the system.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostReadTime, N'all',NULL, 10,   NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostViewCount,N'all',NULL, 9800, NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostFeatured, N'all',NULL, NULL, 0,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostSeries,   N'all',N'SERİ 02', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostSeriesOrd,N'all',NULL, 2, NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P8,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-06-02 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P8, @FPostCategory, @CatBE, 0);

  -- ── P9: REST mi gRPC mi? ───────────────────────────────────────
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by,
                              published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtPost, N'rest-mi-grpc-mi-dogru-secim', N'published', @AdminId, @AdminId,
          '2026-05-28 09:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @P9 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at)
  VALUES
    (@TId,@P9,@FPostTitle,    N'tr', N'REST mi gRPC mi? Doğru Seçim', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostTitle,    N'en', N'REST vs gRPC: Making the Right Choice', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostSlug,     N'all',N'rest-mi-grpc-mi-dogru-secim', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostSummary,  N'tr', N'İki protokolü performans, geliştirici deneyimi ve ekosistem açısından karşılaştırıyoruz.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostSummary,  N'en', N'Comparing the two protocols across performance, developer experience, and ecosystem.', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostBody,     N'tr', N'<h2>REST''in Gücü</h2><p>REST, basitliği ve evrensel desteği sayesinde hâlâ en yaygın API tasarım stili olmaya devam ediyor.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostBody,     N'en', N'<h2>The Power of REST</h2><p>REST remains the most common API design style thanks to its simplicity and universal support.</p>', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostReadTime, N'all',NULL, 9,    NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostViewCount,N'all',NULL, 9100, NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostFeatured, N'all',NULL, NULL, 0,   NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@P9,@FPostPubAt,    N'all',NULL, NULL,NULL,'2026-05-28 09:00:00', GETUTCDATE(),GETUTCDATE());
  INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
  VALUES (@TId, @P9, @FPostCategory, @CatBE, 0);

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 14: VİDEOLAR (8 adet)
  -- ════════════════════════════════════════════════════════════════
  DECLARE @V1 INT, @V2 INT, @V3 INT, @V4 INT,
          @V5 INT, @V6 INT, @V7 INT, @V8 INT;

  -- V1
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'mikroservis-mimarisini-18-dakikada-anlatiyorum', N'published', @AdminId, @AdminId, '2026-06-21 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V1 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V1,@FVidTitle, N'tr',  N'Mikroservis Mimarisini 18 Dakikada Anlatıyorum', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V1,@FVidTitle, N'en',  N'Microservice Architecture in 18 Minutes',        NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V1,@FVidSlug,  N'all', N'mikroservis-mimarisini-18-dakikada-anlatiyorum', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V1,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v1', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V1,@FVidDur,   N'all', N'18:24',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V1,@FVidViews, N'all', NULL, 24000,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V1,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-06-21 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- V2
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'docker-sifirdan-ileri-seviyeye', N'published', @AdminId, @AdminId, '2026-06-10 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V2 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V2,@FVidTitle, N'tr',  N'Docker: Sıfırdan İleri Seviyeye', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V2,@FVidTitle, N'en',  N'Docker: Beginner to Advanced',    NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V2,@FVidSlug,  N'all', N'docker-sifirdan-ileri-seviyeye',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V2,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v2', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V2,@FVidDur,   N'all', N'42:10',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V2,@FVidViews, N'all', NULL, 31000,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V2,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-06-10 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- V3
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'postgresql-index-optimizasyonu-canli-demo', N'published', @AdminId, @AdminId, '2026-05-26 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V3 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V3,@FVidTitle, N'tr',  N'PostgreSQL Index Optimizasyonu — Canlı Demo', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V3,@FVidTitle, N'en',  N'PostgreSQL Index Optimization — Live Demo',   NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V3,@FVidSlug,  N'all', N'postgresql-index-optimizasyonu-canli-demo',   NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V3,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v3', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V3,@FVidDur,   N'all', N'27:55',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V3,@FVidViews, N'all', NULL, 12000,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V3,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-05-26 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- V4
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'react-server-components-derinlemesine', N'published', @AdminId, @AdminId, '2026-05-12 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V4 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V4,@FVidTitle, N'tr',  N'React Server Components Derinlemesine', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V4,@FVidTitle, N'en',  N'React Server Components Deep Dive',     NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V4,@FVidSlug,  N'all', N'react-server-components-derinlemesine', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V4,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v4', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V4,@FVidDur,   N'all', N'21:40',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V4,@FVidViews, N'all', NULL, 18000,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V4,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-05-12 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- V5
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'sifirdan-kubernetes-cluster-kurulumu', N'published', @AdminId, @AdminId, '2026-04-28 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V5 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V5,@FVidTitle, N'tr',  N'Sıfırdan Kubernetes Cluster Kurulumu',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V5,@FVidTitle, N'en',  N'Setting Up a Kubernetes Cluster from Scratch', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V5,@FVidSlug,  N'all', N'sifirdan-kubernetes-cluster-kurulumu',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V5,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v5', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V5,@FVidDur,   N'all', N'35:18',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V5,@FVidViews, N'all', NULL, 9400, NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V5,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-04-28 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- V6
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'takimlar-icin-git-workflowlari', N'published', @AdminId, @AdminId, '2026-04-14 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V6 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V6,@FVidTitle, N'tr',  N'Takımlar İçin Git Workflow''ları', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V6,@FVidTitle, N'en',  N'Git Workflows for Teams',          NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V6,@FVidSlug,  N'all', N'takimlar-icin-git-workflowlari',   NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V6,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v6',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V6,@FVidDur,   N'all', N'16:02',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V6,@FVidViews, N'all', NULL, 14000,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V6,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-04-14 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- V7
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'sistem-tasarimi-mulakatini-birlikte-cozuyoruz', N'published', @AdminId, @AdminId, '2026-03-29 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V7 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V7,@FVidTitle, N'tr',  N'Sistem Tasarımı Mülakatını Birlikte Çözüyoruz', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V7,@FVidTitle, N'en',  N'Solving a System Design Interview Together',    NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V7,@FVidSlug,  N'all', N'sistem-tasarimi-mulakatini-birlikte-cozuyoruz', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V7,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v7', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V7,@FVidDur,   N'all', N'48:33',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V7,@FVidViews, N'all', NULL, 27000,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V7,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-03-29 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- V8
  INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted)
  VALUES (@TId, @CtVideo, N'llm-tabanli-uygulama-gelistirme', N'published', @AdminId, @AdminId, '2026-03-16 10:00:00', GETUTCDATE(), GETUTCDATE(), 0);
  SET @V8 = SCOPE_IDENTITY();
  INSERT INTO content_field_values (tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, created_at, updated_at) VALUES
    (@TId,@V8,@FVidTitle, N'tr',  N'LLM Tabanlı Uygulama Geliştirme', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V8,@FVidTitle, N'en',  N'Building LLM-Powered Applications', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V8,@FVidSlug,  N'all', N'llm-tabanli-uygulama-gelistirme', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V8,@FVidUrl,   N'all', N'https://youtu.be/placeholder-v8', NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V8,@FVidDur,   N'all', N'24:47',  NULL,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V8,@FVidViews, N'all', NULL, 16000,NULL,NULL, GETUTCDATE(),GETUTCDATE()),
    (@TId,@V8,@FVidPubAt, N'all', NULL, NULL, NULL,'2026-03-16 10:00:00', GETUTCDATE(),GETUTCDATE());

  -- ════════════════════════════════════════════════════════════════
  -- ADIM 15: content_item_titles (API liste sorgusunun zorunlu)
  -- Her içerik öğesi için dil başına bir başlık satırı gerekiyor.
  -- ════════════════════════════════════════════════════════════════

  -- Kategoriler: FCatName (localized tr+en)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active, created_at, updated_at)
  SELECT cfv.tenant_id, cfv.content_item_id, cfv.language_code, cfv.value_text, 1, GETUTCDATE(), GETUTCDATE()
  FROM content_field_values cfv
  WHERE cfv.content_field_id = @FCatName AND cfv.language_code IN (N'tr', N'en')
    AND cfv.tenant_id = @TId;

  -- Hakkımda: full-name (non-localized → 'all') için tr ve en başlık oluştur
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active, created_at, updated_at)
  VALUES (@TId, @AboutItem, N'tr', N'Mert Özen', 1, GETUTCDATE(), GETUTCDATE()),
         (@TId, @AboutItem, N'en', N'Mert Özen', 1, GETUTCDATE(), GETUTCDATE());

  -- Blog yazıları: FPostTitle (localized tr+en)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active, created_at, updated_at)
  SELECT cfv.tenant_id, cfv.content_item_id, cfv.language_code, cfv.value_text, 1, GETUTCDATE(), GETUTCDATE()
  FROM content_field_values cfv
  WHERE cfv.content_field_id = @FPostTitle AND cfv.language_code IN (N'tr', N'en')
    AND cfv.tenant_id = @TId;

  -- Videolar: FVidTitle (localized tr+en)
  INSERT INTO content_item_titles (tenant_id, content_item_id, language_code, title, is_active, created_at, updated_at)
  SELECT cfv.tenant_id, cfv.content_item_id, cfv.language_code, cfv.value_text, 1, GETUTCDATE(), GETUTCDATE()
  FROM content_field_values cfv
  WHERE cfv.content_field_id = @FVidTitle AND cfv.language_code IN (N'tr', N'en')
    AND cfv.tenant_id = @TId;

  -- ────────────────────────────────────────────────────────────────
  COMMIT TRANSACTION;
  PRINT '✅ 003_mert_ozen_seed.sql başarıyla uygulandı.';
  PRINT '   Tenant : mert-ozen';
  PRINT '   Kullanıcı: admin@mert-ozen.local / Admin123!';
  PRINT '   CMS URL : http://localhost:5267 (DevTenantSlug=mert-ozen)';
  PRINT '   API URL : http://localhost:5267/api/v1/mert-ozen/{category|post|video|about}';

END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();
  DECLARE @Line INT = ERROR_LINE();
  PRINT '❌ Hata (Satır ' + CAST(@Line AS NVARCHAR) + '): ' + @Msg;
  THROW;
END CATCH;
