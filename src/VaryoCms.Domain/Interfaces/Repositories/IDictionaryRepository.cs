using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext).
// dictionary_translations has no tenant_id; it is reached only through tenant-scoped entry ids.
public interface IDictionaryRepository
{
    Task<(IReadOnlyList<DictionaryEntry> Items, int Total)> GetPagedAsync(
        string? search, string? category, int page, int pageSize, CancellationToken ct = default);
    Task<DictionaryEntry?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DictionaryTranslation>> GetTranslationsAsync(int entryId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> GetTranslatedCountsAsync(
        IReadOnlyList<int> entryIds, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(string keyName, int? excludeId = null, CancellationToken ct = default);
    Task<int> CreateAsync(DictionaryEntry entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(DictionaryEntry entity, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
    // Upserts non-empty values, deletes empty ones (one transaction). Keyed by (entry_id, language_code).
    Task SaveTranslationsAsync(
        int entryId, IReadOnlyDictionary<string, string?> translations, CancellationToken ct = default);
}
