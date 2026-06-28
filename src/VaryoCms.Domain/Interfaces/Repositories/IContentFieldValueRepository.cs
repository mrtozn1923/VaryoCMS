using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// EAV value access for a content item. Tenant scope applied inside the implementation.
public interface IContentFieldValueRepository
{
    // Values for an item across the given language plus non-localized ('all') rows.
    Task<IReadOnlyList<ContentFieldValue>> GetByItemAsync(
        int contentItemId, string languageCode, CancellationToken ct = default);

    // Upserts all provided values for the item in a single transaction
    // (unique key: content_item_id + content_field_id + language_code).
    Task SaveValuesAsync(
        int contentItemId, IReadOnlyList<ContentFieldValue> values, CancellationToken ct = default);
}
