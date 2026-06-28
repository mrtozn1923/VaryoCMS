using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;

namespace VaryoCms.Application.Interfaces;

// The public API (read + write). tenantSlug comes from the URL (not host-based middleware).
// Result.Error values: "NotFound" → 404, "Unauthorized" → 401, "Forbidden" → 403,
// strings starting with "Validation:" → 400.
public interface IPublicApiService
{
    Task<Result<ApiListResponse>> GetListAsync(
        string tenantSlug, string contentTypeSlug, ApiListRequest request, ApiCredentials credentials, CancellationToken ct = default);
    Task<Result<ApiItemDto>> GetByIdAsync(
        string tenantSlug, string contentTypeSlug, int id, string? lang, ApiCredentials credentials, CancellationToken ct = default);
    Task<Result<ApiItemDto>> GetBySlugAsync(
        string tenantSlug, string contentTypeSlug, string slug, string? lang, ApiCredentials credentials, CancellationToken ct = default);

    // Write operations. Credentials are ALWAYS required regardless of is_public.
    Task<Result<ApiItemDto>> CreateAsync(
        string tenantSlug, string contentTypeSlug, ApiWriteRequest body, ApiCredentials credentials, CancellationToken ct = default);
    Task<Result<ApiItemDto>> UpdateAsync(
        string tenantSlug, string contentTypeSlug, int id, ApiWriteRequest body, ApiCredentials credentials, CancellationToken ct = default);
    Task<Result> DeleteAsync(
        string tenantSlug, string contentTypeSlug, int id, ApiCredentials credentials, CancellationToken ct = default);
}
