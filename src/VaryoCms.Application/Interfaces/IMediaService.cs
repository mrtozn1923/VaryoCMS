using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Media;

namespace VaryoCms.Application.Interfaces;

public interface IMediaService
{
    Task<Result<MediaAssetDto>> UploadAsync(
        Stream content, string originalFileName, string? contentType, long sizeBytes, CancellationToken ct = default);
    Task<Result<PagedResult<MediaAssetDto>>> GetListAsync(
        string? mediaType, int page, int pageSize, CancellationToken ct = default);
    Task<Result<MediaAssetDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    // Crops an image in place (replaces its file, updates dimensions). Image media only.
    Task<Result<MediaAssetDto>> CropAsync(int id, int x, int y, int width, int height, CancellationToken ct = default);

    // Updates the display name (base part, extension preserved) and alt text. Physical file unchanged.
    Task<Result> UpdateMetaAsync(int id, string baseName, string? altText, CancellationToken ct = default);

    // Media picker support.
    Task<IReadOnlyList<MediaAssetDto>> SearchAsync(
        string? query, string? mediaType, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAssetDto>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
}
