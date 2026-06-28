using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext).
public interface IMediaRepository
{
    Task<(IReadOnlyList<MediaAsset> Items, int Total)> GetPagedAsync(
        string? mediaType, int page, int pageSize, CancellationToken ct = default);
    Task<MediaAsset?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(MediaAsset entity, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);

    // Updates the stored file + dimensions after an in-place crop/resize.
    Task UpdateFileAsync(
        int id, string fileName, string filePath, int width, int height, long sizeBytes, CancellationToken ct = default);

    // Updates the display name and alt text without touching the physical file.
    Task UpdateMetaAsync(int id, string originalName, string? altText, CancellationToken ct = default);

    Task<IReadOnlyList<MediaAsset>> SearchAsync(
        string? query, string? mediaType, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
}
