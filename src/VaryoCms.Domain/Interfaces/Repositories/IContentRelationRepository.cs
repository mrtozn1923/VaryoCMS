namespace VaryoCms.Domain.Interfaces.Repositories;

// Admin-side relation storage + target lookups (content_field_relations). Tenant scope via ITenantContext.
public interface IContentRelationRepository
{
    // Ordered target item ids for a relation field on an item.
    Task<IReadOnlyList<int>> GetTargetIdsAsync(int sourceItemId, int sourceFieldId, CancellationToken ct = default);

    // Replaces all targets for (item, field) in one transaction, preserving the given order.
    Task ReplaceAsync(int sourceItemId, int sourceFieldId, IReadOnlyList<int> targetIds, CancellationToken ct = default);

    // Searches a target content type's published items by display field (or slug), for the picker.
    Task<IReadOnlyList<(int Id, string Display)>> SearchTargetsAsync(
        int targetContentTypeId, string? displayFieldSlug, string? query, string lang, int limit, CancellationToken ct = default);

    // Display values for a set of target items (for rendering current selections).
    Task<IReadOnlyDictionary<int, string>> GetDisplayValuesAsync(
        int targetContentTypeId, string? displayFieldSlug, IReadOnlyList<int> itemIds, string lang, CancellationToken ct = default);
}
