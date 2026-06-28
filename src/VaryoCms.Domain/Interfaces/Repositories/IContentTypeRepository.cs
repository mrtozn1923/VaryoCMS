using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext), not passed in.
public interface IContentTypeRepository
{
    Task<ContentType?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ContentType>> GetAllAsync(CancellationToken ct = default);
    Task<int> CreateAsync(ContentType entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(ContentType entity, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken ct = default);
}
