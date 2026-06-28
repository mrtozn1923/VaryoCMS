using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Admin-side API config access. Tenant scope is applied inside the implementation (via ITenantContext).
// Auth (ApiKey/JWT) is now managed via IApiCredentialRepository; this handles only per-content-type config.
public interface IApiConfigurationRepository
{
    Task<IReadOnlyList<ApiConfiguration>> GetAllAsync(CancellationToken ct = default);
    Task<ApiConfiguration?> GetByContentTypeIdAsync(int contentTypeId, CancellationToken ct = default);
    // Inserts or updates the (tenant, content type) config. Returns the config id.
    // Writes is_enabled, is_public, allow_* flags, rate_limit_per_min, cache_seconds.
    Task<int> UpsertAsync(ApiConfiguration entity, CancellationToken ct = default);

    Task<IReadOnlyList<ApiFieldVisibility>> GetFieldVisibilityAsync(int apiConfigId, CancellationToken ct = default);
    // Replaces all visibility rows for the config in one transaction (delete-then-insert).
    Task ReplaceFieldVisibilityAsync(
        int apiConfigId, IReadOnlyList<ApiFieldVisibility> rows, CancellationToken ct = default);
}
