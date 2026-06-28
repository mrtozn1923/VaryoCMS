using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Write access for the public API. Like IPublicApiRepository, tenant comes from the URL slug
// (explicit tenantId parameter) and this repo deliberately does NOT use ITenantContext.
// SQL mirrors ContentItemRepository / ContentFieldValueRepository / ContentRelationRepository
// but replaces _tenantContext.TenantId with an explicit @TenantId parameter.
public interface IPublicApiWriteRepository
{
    // Field definitions for a content type — needed for required-field validation + slug→fieldId mapping.
    Task<IReadOnlyList<ContentField>> GetContentTypeFieldsAsync(
        int tenantId, int contentTypeId, CancellationToken ct = default);

    // Returns true when a slug is already in use within the same tenant+content-type scope.
    Task<bool> SlugExistsAsync(
        int tenantId, int contentTypeId, string slug, int? excludeItemId, CancellationToken ct = default);

    // Inserts a new content item and returns the generated id.
    Task<int> InsertItemAsync(
        int tenantId, int contentTypeId, string? slug, string status,
        CancellationToken ct = default);

    // Updates status, slug, and updated_at. Returns false when the item is not found.
    Task<bool> UpdateItemAsync(
        int tenantId, int itemId, string? slug, string status, CancellationToken ct = default);

    // Soft-deletes a content item. Returns false when the item is not found.
    Task<bool> SoftDeleteItemAsync(int tenantId, int itemId, CancellationToken ct = default);

    // Fetches a single published item for the given tenant+content-type (for update validation).
    Task<ContentItem?> GetItemByIdAsync(int tenantId, int contentTypeId, int itemId, CancellationToken ct = default);

    // Upserts field values in one transaction (MERGE on item+field+language).
    Task SaveValuesAsync(
        int tenantId, int itemId, IReadOnlyList<ContentFieldValue> values, CancellationToken ct = default);

    // Replaces all relations for (itemId, fieldId) in one transaction (DELETE-then-INSERT).
    Task ReplaceRelationsAsync(
        int tenantId, int itemId, int fieldId, IReadOnlyList<int> targetIds, CancellationToken ct = default);

    // Upserts the content item title for (itemId, languageCode). Deletes when title is null or empty.
    Task SaveTitleAsync(
        int tenantId, int itemId, string languageCode, string? title, bool isActive, CancellationToken ct = default);
}
