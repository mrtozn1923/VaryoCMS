using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext).
public interface IContentItemRepository
{
    Task<(IReadOnlyList<ContentItem> Items, int Total)> GetPagedAsync(
        int contentTypeId, int page, int pageSize, CancellationToken ct = default);

    // Returns enriched rows (title for the given language, creator/updater names) for the list grid.
    Task<(IReadOnlyList<ContentItemListRow> Rows, int Total)> GetPagedListAsync(
        int contentTypeId, string languageCode, int page, int pageSize,
        string? searchQuery = null, string? statusFilter = null, string? languageFilter = null,
        CancellationToken ct = default);

    Task<bool> SlugExistsAsync(int contentTypeId, string slug, int? excludeItemId, CancellationToken ct = default);

    Task<ContentItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(ContentItem entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(ContentItem entity, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
}
