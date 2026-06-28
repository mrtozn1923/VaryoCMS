using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Cross-tenant tenant management — operates on the root `tenants` table (plus first language/admin on create),
// so it does NOT use ITenantContext. All ids are passed explicitly.
public interface ITenantProvisioningRepository
{
    Task<(IReadOnlyList<TenantSummary> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<Tenant?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken ct = default);

    // Creates tenant + its default language + its first TenantAdmin in a single transaction; returns tenant id.
    Task<int> ProvisionAsync(NewTenant data, CancellationToken ct = default);

    Task<bool> UpdateAsync(int id, string name, bool isActive, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
}

// Read model for the tenant list: tenant fields + per-tenant counts.
public record TenantSummary(
    int Id, string Name, string Slug, bool IsActive, DateTime CreatedAt, int UserCount, int ContentTypeCount);

// Provisioning input. AdminPasswordHash is already hashed by the Application layer.
public record NewTenant(
    string Name, string Slug,
    string LanguageCode, string LanguageName,
    string AdminEmail, string AdminPasswordHash, string? AdminFullName);
