using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Read-only access for the public API. Tenant is resolved from the URL slug, so every method takes
// an explicit tenantId and deliberately does NOT depend on ITenantContext (which is host-based).
public interface IPublicApiRepository
{
    // Enabled config for (tenant, content type slug). Returns null if missing, disabled, or deleted.
    Task<ApiConfiguration?> GetEnabledConfigAsync(int tenantId, string contentTypeSlug, CancellationToken ct = default);

    // Visible fields for the config, paired with their optional response-key alias, in sort order.
    Task<IReadOnlyList<(ContentField Field, string? Alias)>> GetVisibleFieldsAsync(
        int apiConfigId, CancellationToken ct = default);

    Task<(IReadOnlyList<ContentItem> Items, int Total)> GetItemsAsync(
        ApiItemQuery query, CancellationToken ct = default);

    // lang is required to enforce the is_active check — item must be activated for the requested language.
    Task<ContentItem?> GetItemByIdAsync(int tenantId, int contentTypeId, int id, string lang, CancellationToken ct = default);
    Task<ContentItem?> GetItemBySlugAsync(int tenantId, int contentTypeId, string slug, string lang, CancellationToken ct = default);

    Task<IReadOnlyList<ContentFieldValue>> GetValuesAsync(
        int tenantId, IReadOnlyList<int> itemIds, string lang, CancellationToken ct = default);

    // Relation rows (ordered) for the given source items — for expanding Relation/MultiRelation fields.
    Task<IReadOnlyList<(int SourceItemId, int SourceFieldId, int TargetItemId)>> GetRelationsAsync(
        int tenantId, IReadOnlyList<int> sourceItemIds, CancellationToken ct = default);

    // Display values for target items of a content type (display field's value_text, else slug, else #id).
    Task<IReadOnlyDictionary<int, string>> GetDisplayValuesAsync(
        int tenantId, int targetContentTypeId, string? displayFieldSlug,
        IReadOnlyList<int> targetItemIds, string lang, CancellationToken ct = default);

    // Media id -> web path, for expanding Image/Video/Audio/File/Gallery fields.
    Task<IReadOnlyDictionary<int, string>> GetMediaUrlsAsync(
        int tenantId, IReadOnlyList<int> mediaIds, CancellationToken ct = default);

    // Credential grant lookup for ApiKey auth: verifies that credentialId is active, covers contentTypeId,
    // and returns the BCrypt hash for verification. Returns null when no matching active grant exists.
    Task<string?> GetApiKeyGrantHashAsync(int tenantId, int credentialId, int contentTypeId, CancellationToken ct = default);

    // Returns content type ids granted by a given JWT credential (for multi-content-type JWT validation).
    Task<IReadOnlyList<string>> GetCredentialContentTypeSlugsAsync(int tenantId, int credentialId, CancellationToken ct = default);

    // Returns the credential name for audit logging purposes. Returns null when not found.
    Task<string?> GetCredentialNameAsync(int tenantId, int credentialId, CancellationToken ct = default);
}
