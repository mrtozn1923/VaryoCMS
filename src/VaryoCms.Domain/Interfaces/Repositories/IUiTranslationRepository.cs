using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Global (cross-tenant) admin-UI translations. NOT tenant-scoped — managed by SystemAdmin, applies everywhere.
public interface IUiTranslationRepository
{
    // resource_key -> value for one culture.
    Task<IReadOnlyDictionary<string, string>> GetAllForCultureAsync(string culture, CancellationToken ct = default);

    // Active UI culture codes (e.g. "tr", "en").
    Task<IReadOnlyList<string>> GetActiveCultureCodesAsync(CancellationToken ct = default);

    // Default UI culture code (falls back to "tr" if none flagged).
    Task<string> GetDefaultCultureCodeAsync(CancellationToken ct = default);

    // --- Management (SystemAdmin) ---
    Task<IReadOnlyList<UiCulture>> GetCulturesAsync(bool activeOnly, CancellationToken ct = default);
    Task<bool> CultureExistsAsync(string code, CancellationToken ct = default);
    Task AddCultureAsync(string code, string name, CancellationToken ct = default);

    // Distinct resource keys (searchable, paged).
    Task<(IReadOnlyList<string> Keys, int Total)> GetKeysAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);

    // For the given keys: resource_key -> (culture -> value).
    Task<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> GetValuesForKeysAsync(
        IReadOnlyList<string> keys, CancellationToken ct = default);

    Task UpsertAsync(string culture, string resourceKey, string value, CancellationToken ct = default);

    // Bulk import for a culture (key -> value); returns rows affected.
    Task<int> BulkUpsertAsync(string culture, IReadOnlyDictionary<string, string> values, CancellationToken ct = default);
}
