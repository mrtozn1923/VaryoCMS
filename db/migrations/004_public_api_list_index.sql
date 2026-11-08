-- 004_public_api_list_index.sql
-- Public API list query (PublicApiRepository.GetItemsAsync) filters content_items by
-- tenant_id + content_type_id + is_deleted = 0 + status and orders by created_at (default sort).
-- With hundreds of published posts and several list calls per page view, the existing
-- single-column indexes forced wide scans; this covering index serves the filter and the
-- default ordering. Idempotent: safe to run more than once.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_content_items_public_list' AND object_id = OBJECT_ID('content_items')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_content_items_public_list
        ON content_items (tenant_id, content_type_id, status, created_at)
        INCLUDE (slug, published_at, updated_at, created_by, updated_by)
        WHERE is_deleted = 0;
END
GO
