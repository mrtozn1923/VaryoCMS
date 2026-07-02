-- ================================================================
-- 005_inbox_and_notify.sql
-- İki platform özelliği (tenant-agnostik, tüm tenant'lara uygulanır):
--
-- 1) content_items.is_read  — "okundu/okunmadı" durumu. Public API ile oluşan
--    yeni kayıtlar okunmamış (0) başlar. Admin gelen-kutusu arayüzü bunu kullanır.
--
-- 2) content_types.notify_on_create — bir içerik tipini "form gönderim kutusu"
--    olarak işaretler. TRUE ise: (a) public API ile yeni kayıt gelince tenant'a
--    e-posta bildirimi gider, (b) admin liste görünümü okundu/okunmadı + filtre
--    (gelen-kutusu) UI'sını gösterir. contact-message için 1'e set edilir.
--
-- ADDITIVE + IDEMPOTENT. Her ifade IF NOT EXISTS ile korunur; GO ile batch'lere
-- ayrılır (yeni kolonlar sonraki batch'lerde görünür olsun diye). 001'den sonra
-- herhangi bir sırada güvenle (tekrar tekrar) çalıştırılabilir.
--
-- Uygula:
--   docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P "Varyo_Dev2024!" -C -d Varyo \
--     -i /migrations/005_inbox_and_notify.sql -b
-- ================================================================
SET NOCOUNT ON;
-- Filtered index oluşturmak QUOTED_IDENTIFIER/ANSI_NULLS ON gerektirir.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1) content_items.is_read
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('content_items') AND name = 'is_read')
BEGIN
    ALTER TABLE content_items
      ADD is_read BIT NOT NULL CONSTRAINT DF_content_items_is_read DEFAULT 0;
    PRINT '=== content_items.is_read eklendi ===';
END
ELSE PRINT '=== content_items.is_read zaten var — atlaniyor ===';
GO

-- Okunmamışları hızlı filtrelemek için filtered index (yalnızca is_read=0 satırlar)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_content_items_unread')
BEGIN
    CREATE NONCLUSTERED INDEX IX_content_items_unread
      ON content_items (tenant_id, content_type_id)
      WHERE is_read = 0 AND is_deleted = 0;
    PRINT '=== IX_content_items_unread olusturuldu ===';
END
GO

-- 2) content_types.notify_on_create
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('content_types') AND name = 'notify_on_create')
BEGIN
    ALTER TABLE content_types
      ADD notify_on_create BIT NOT NULL CONSTRAINT DF_content_types_notify_on_create DEFAULT 0;
    PRINT '=== content_types.notify_on_create eklendi ===';
END
ELSE PRINT '=== content_types.notify_on_create zaten var — atlaniyor ===';
GO

-- contact-message içerik tiplerini "form gönderim kutusu" olarak işaretle (tüm tenant'larda)
UPDATE content_types
SET notify_on_create = 1, updated_at = GETUTCDATE()
WHERE slug = N'contact-message' AND is_deleted = 0 AND notify_on_create = 0;
PRINT '=== contact-message notify_on_create=1 isaretlendi ===';
GO

-- Gelen-kutusu (okundu/okunmadı) UI çevirileri — DB-backed admin localizer (ui_translations)
MERGE ui_translations AS target
USING (VALUES
    ('tr', N'ContentItem.Unread',             N'Okunmadı'),
    ('en', N'ContentItem.Unread',             N'Unread'),
    ('tr', N'ContentItem.MarkRead',           N'Okundu işaretle'),
    ('en', N'ContentItem.MarkRead',           N'Mark as read'),
    ('tr', N'ContentItem.MarkUnread',         N'Okunmadı işaretle'),
    ('en', N'ContentItem.MarkUnread',         N'Mark as unread'),
    ('tr', N'ContentItem.Filter.UnreadOnly',  N'Yalnızca okunmamışlar'),
    ('en', N'ContentItem.Filter.UnreadOnly',  N'Unread only')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO
