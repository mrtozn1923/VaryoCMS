using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext).
public interface IContentFieldRepository
{
    Task<IReadOnlyList<ContentField>> GetByContentTypeAsync(int contentTypeId, CancellationToken ct = default);
    Task<ContentField?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(ContentField entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(ContentField entity, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(int contentTypeId, string slug, int? excludeId = null, CancellationToken ct = default);

    // Bulk sort_order update in a single transaction. Returns the number of rows updated.
    Task<int> ReorderAsync(int contentTypeId, IReadOnlyList<int> orderedFieldIds, CancellationToken ct = default);
}
