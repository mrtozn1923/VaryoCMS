-- ================================================================
-- 004_contact_messages.sql
-- Mert Özen tenant — "İletişim Mesajı" (contact-message) içerik tipi.
-- Frontend iletişim formu (FrontendSite /iletisim) verilerini bu tabloya POST eder.
--
-- ADDITIVE + IDEMPOTENT: full reset YAPMAZ. 003'ten SONRA çalıştırılır. Tekrar
-- çalıştırılırsa content type zaten varsa hiçbir şey yapmaz. (003 bir dev reset
-- olduğu için re-run edilirse bu tipi de siler — o durumda 004'ü yeniden çalıştır.)
--
-- API: POST http://localhost:5267/api/v1/dev-tenant/contact-message
--      Header: X-API-Key: vk_1_M3rtOzen_SecretKey2026!  (allow_create=1, is_public=0)
--      Body:   { "lang":"tr", "status":"published", "title":"<konu>",
--                "fields": { "name":..., "email":..., "subject":..., "message":... } }
--
-- Uygula:
--   docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P "Varyo_Dev2024!" -C -d Varyo \
--     -i /migrations/004_contact_messages.sql -b
-- ================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRY
  BEGIN TRANSACTION;

  DECLARE @TId INT = (SELECT id FROM tenants WHERE slug = N'dev-tenant' AND is_deleted = 0);
  IF @TId IS NULL
  BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Tenant ''dev-tenant'' bulunamadi — once 003_dev_tenant_seed.sql calistir.', 16, 1);
    RETURN;
  END

  IF EXISTS (SELECT 1 FROM content_types
             WHERE tenant_id = @TId AND slug = N'contact-message' AND is_deleted = 0)
  BEGIN
    ROLLBACK TRANSACTION;
    PRINT '=== contact-message zaten mevcut — atlaniyor ===';
    RETURN;
  END

  -- ----------------------------------------------------------------
  -- Content type
  -- ----------------------------------------------------------------
  DECLARE @CtContact INT;
  INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, created_at, updated_at, is_deleted)
  VALUES (@TId, N'İletişim Mesajı', N'contact-message',
          N'Sitedeki iletişim formundan gönderilen mesajlar', N'bi-envelope',
          1, 13, GETUTCDATE(), GETUTCDATE(), 0);
  SET @CtContact = SCOPE_IDENTITY();

  -- ----------------------------------------------------------------
  -- Fields — submission data, hepsi is_localized = 0 (dile bağlı değil)
  -- ----------------------------------------------------------------
  INSERT INTO content_fields (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted)
  VALUES
    (@TId, @CtContact, N'Ad Soyad', N'name',    N'Text',  1, 0, 0, N'{"max_length":200}',  GETUTCDATE(), GETUTCDATE(), 0),
    (@TId, @CtContact, N'E-posta',  N'email',   N'Email', 1, 0, 1, NULL,                    GETUTCDATE(), GETUTCDATE(), 0),
    (@TId, @CtContact, N'Konu',     N'subject', N'Text',  1, 0, 2, N'{"max_length":300}',  GETUTCDATE(), GETUTCDATE(), 0),
    (@TId, @CtContact, N'Mesaj',    N'message', N'Text',  1, 0, 3, N'{"max_length":5000}', GETUTCDATE(), GETUTCDATE(), 0);

  -- ----------------------------------------------------------------
  -- API configuration — sadece CREATE açık (frontend yazar; okuma/güncelleme/silme yok).
  -- is_public=0: yazma her zaman credential ister. rate_limit düşük (spam'a karşı).
  -- ----------------------------------------------------------------
  DECLARE @CfgContact INT;
  INSERT INTO api_configurations (tenant_id, content_type_id, is_enabled, auth_type, allow_filtering, allow_sorting, allow_pagination, is_public, allow_read, allow_create, allow_update, allow_delete, rate_limit_per_min, cache_seconds, created_at, updated_at)
  VALUES (@TId, @CtContact, 1, N'ApiKey', 0, 0, 0, 0, 0, 1, 0, 0, 20, 0, GETUTCDATE(), GETUTCDATE());
  SET @CfgContact = SCOPE_IDENTITY();

  -- Tüm alanlar API'de görünür (yine de yazma için gerekli — write yalnızca görünür alanları kabul eder)
  INSERT INTO api_field_visibility (api_configuration_id, content_field_id, is_visible, response_key_alias)
  SELECT @CfgContact, cf.id, 1, NULL
  FROM content_fields cf
  WHERE cf.content_type_id = @CtContact AND cf.is_deleted = 0;

  -- ----------------------------------------------------------------
  -- Mevcut Tenant API anahtarına contact-message erişimi ver
  -- (write auth kredensiyeli bu içerik tipi için grant'lı olmasını ister).
  -- ----------------------------------------------------------------
  DECLARE @CredContact INT = (
    SELECT TOP 1 id FROM api_credentials
    WHERE tenant_id = @TId AND is_deleted = 0 AND auth_type = N'ApiKey'
    ORDER BY id);

  IF @CredContact IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM api_credential_content_types
                     WHERE api_credential_id = @CredContact AND content_type_id = @CtContact)
  BEGIN
    INSERT INTO api_credential_content_types (tenant_id, api_credential_id, content_type_id)
    VALUES (@TId, @CredContact, @CtContact);
  END

  COMMIT TRANSACTION;
  PRINT '=== contact-message icerik tipi olusturuldu (create-only API, Tenant anahtarina grant verildi) ===';
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
  RAISERROR('004_contact_messages HATA: %s', 16, 1, @msg);
END CATCH
