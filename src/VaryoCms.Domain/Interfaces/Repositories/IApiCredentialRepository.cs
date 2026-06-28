using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Admin-side API credential access. Tenant scope is applied inside the implementation (via ITenantContext).
public interface IApiCredentialRepository
{
    // Returns all active credentials for the current tenant with a count of granted content types.
    Task<IReadOnlyList<(ApiCredential Credential, int GrantedCount)>> GetAllAsync(CancellationToken ct = default);

    // Returns the credential plus the list of granted content type ids. Null when not found or soft-deleted.
    Task<(ApiCredential Credential, IReadOnlyList<int> ContentTypeIds)?> GetByIdAsync(int id, CancellationToken ct = default);

    // Inserts a new credential. Returns the generated id.
    Task<int> CreateAsync(ApiCredential credential, CancellationToken ct = default);

    // Updates name and is_active (auth_type is immutable after creation).
    Task UpdateAsync(ApiCredential credential, CancellationToken ct = default);

    // Stores the BCrypt hash for the ApiKey credential (shown plaintext only once; called after Create/Rotate).
    Task UpdateApiKeyAsync(int credentialId, string? apiKeyHash, CancellationToken ct = default);

    // Replaces the full set of content type grants in one transaction (delete-then-insert).
    Task ReplaceContentTypesAsync(int credentialId, IReadOnlyList<int> contentTypeIds, CancellationToken ct = default);

    // Soft-deletes the credential.
    Task DeleteAsync(int id, CancellationToken ct = default);
}
