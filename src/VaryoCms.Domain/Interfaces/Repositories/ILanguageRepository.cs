using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext).
public interface ILanguageRepository
{
    Task<IReadOnlyList<Language>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Language>> GetAllAsync(CancellationToken ct = default);
    Task<Language?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken ct = default);
    Task<int> ActiveCountAsync(CancellationToken ct = default);

    // Create/Update clear other defaults in a single transaction when the row is marked default.
    Task<int> CreateAsync(Language entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(Language entity, CancellationToken ct = default);   // code is immutable; not updated
    Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
}
