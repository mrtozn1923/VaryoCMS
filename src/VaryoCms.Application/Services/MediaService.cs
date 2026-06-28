using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Media;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class MediaService : IMediaService
{
    private const long MaxUploadBytes = 50 * 1024 * 1024;   // 50 MB

    private readonly IMediaRepository _repository;
    private readonly IFileStorageService _storage;
    private readonly IImageProcessor _imageProcessor;
    private readonly ITenantContext _tenant;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public MediaService(
        IMediaRepository repository, IFileStorageService storage,
        IImageProcessor imageProcessor, ITenantContext tenant,
        IStringLocalizer<SharedResource> t, IAuditLogger audit)
    {
        _repository = repository;
        _storage = storage;
        _imageProcessor = imageProcessor;
        _tenant = tenant;
        _t = t;
        _audit = audit;
    }

    public async Task<Result<MediaAssetDto>> UploadAsync(
        Stream content, string originalFileName, string? contentType, long sizeBytes, CancellationToken ct = default)
    {
        if (sizeBytes <= 0)
            return Result<MediaAssetDto>.Failure(_t["Err.EmptyFile"]);
        if (sizeBytes > MaxUploadBytes)
            return Result<MediaAssetDto>.Failure(_t["Err.FileExceedsLimit", MaxUploadBytes / (1024 * 1024)]);
        if (string.IsNullOrWhiteSpace(originalFileName))
            return Result<MediaAssetDto>.Failure(_t["Err.MissingFileName"]);
        if (!MediaAllowedTypes.IsAllowed(contentType))
            return Result<MediaAssetDto>.Failure(_t["Err.FileTypeNotAllowed"]);

        string mediaType = ResolveMediaType(contentType);

        ImageDimensions? dims = mediaType == "image" ? _imageProcessor.TryReadDimensions(content) : null;
        // Pass validated contentType so the storage layer derives the extension from the server-side MIME map.
        var stored = await _storage.SaveAsync(content, originalFileName, _tenant.TenantId, contentType, ct);

        var asset = new MediaAsset
        {
            FileName = stored.FileName,
            OriginalName = originalFileName,
            FilePath = stored.RelativePath,
            MediaType = mediaType,
            MimeType = contentType,
            FileSizeBytes = stored.SizeBytes,
            Width = dims?.Width,
            Height = dims?.Height
        };

        int id = await _repository.CreateAsync(asset, ct);
        asset.Id = id;
        await _audit.LogAsync(AuditActions.MediaUploaded, "Media", id, entityName: originalFileName, ct: ct);
        return Result<MediaAssetDto>.Success(MapToDto(asset));
    }

    public async Task<Result<PagedResult<MediaAssetDto>>> GetListAsync(
        string? mediaType, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 24 : pageSize;

        var (items, total) = await _repository.GetPagedAsync(mediaType, page, pageSize, ct);
        IReadOnlyList<MediaAssetDto> dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<MediaAssetDto>>.Success(
            new PagedResult<MediaAssetDto>(dtos, page, pageSize, total));
    }

    public async Task<Result<MediaAssetDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var asset = await _repository.GetByIdAsync(id, ct);
        return asset is null
            ? Result<MediaAssetDto>.Failure(_t["Err.MediaNotFound"])
            : Result<MediaAssetDto>.Success(MapToDto(asset));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var asset = await _repository.GetByIdAsync(id, ct);
        if (asset is null) return Result.Failure(_t["Err.MediaNotFound"]);

        await _repository.SoftDeleteAsync(id, ct);
        await _audit.LogAsync(AuditActions.MediaDeleted, "Media", id, entityName: asset.OriginalName, ct: ct);
        // Keep the physical file (soft delete); a cleanup job can remove orphaned files later.
        return Result.Success();
    }

    public async Task<Result<MediaAssetDto>> CropAsync(
        int id, int x, int y, int width, int height, CancellationToken ct = default)
    {
        var asset = await _repository.GetByIdAsync(id, ct);
        if (asset is null) return Result<MediaAssetDto>.Failure(_t["Err.MediaNotFound"]);
        if (asset.MediaType != "image") return Result<MediaAssetDto>.Failure(_t["Err.OnlyImagesCrop"]);

        await using var input = await _storage.OpenReadAsync(asset.FilePath, ct);
        if (input is null) return Result<MediaAssetDto>.Failure(_t["Err.SourceFileNotFound"]);

        CroppedImage cropped;
        try
        {
            cropped = _imageProcessor.Crop(input, x, y, width, height);
        }
        catch (Exception)
        {
            return Result<MediaAssetDto>.Failure(_t["Err.CropFailed"]);
        }

        string oldPath = asset.FilePath;
        var stored = await _storage.SaveAsync(new MemoryStream(cropped.Bytes), asset.OriginalName, _tenant.TenantId, ct);
        await _repository.UpdateFileAsync(id, stored.FileName, stored.RelativePath, cropped.Width, cropped.Height, stored.SizeBytes, ct);
        await _storage.DeleteAsync(oldPath, ct);   // remove the pre-crop file

        asset.FileName = stored.FileName;
        asset.FilePath = stored.RelativePath;
        asset.Width = cropped.Width;
        asset.Height = cropped.Height;
        asset.FileSizeBytes = stored.SizeBytes;
        await _audit.LogAsync(AuditActions.MediaCropped, "Media", id, entityName: asset.OriginalName, ct: ct);
        return Result<MediaAssetDto>.Success(MapToDto(asset));
    }

    public async Task<Result> UpdateMetaAsync(int id, string baseName, string? altText, CancellationToken ct = default)
    {
        baseName = baseName?.Trim() ?? string.Empty;
        // Strip path separators and leading dots to prevent path traversal / hidden-file names.
        baseName = baseName.Replace('/', '_').Replace('\\', '_').TrimStart('.');
        if (string.IsNullOrEmpty(baseName))
            return Result.Failure(_t["Err.InvalidBaseName"]);

        var asset = await _repository.GetByIdAsync(id, ct);
        if (asset is null) return Result.Failure(_t["Err.MediaNotFound"]);

        string ext = Path.GetExtension(asset.OriginalName);
        string newOriginalName = baseName + ext;

        await _repository.UpdateMetaAsync(id, newOriginalName, altText?.Trim(), ct);
        await _audit.LogAsync(AuditActions.MediaRenamed, "Media", id, entityName: newOriginalName, ct: ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<MediaAssetDto>> SearchAsync(
        string? query, string? mediaType, int limit, CancellationToken ct = default)
    {
        limit = limit is < 1 or > 50 ? 20 : limit;
        var items = await _repository.SearchAsync(
            string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
            string.IsNullOrWhiteSpace(mediaType) ? null : mediaType, limit, ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<MediaAssetDto>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return Array.Empty<MediaAssetDto>();
        var found = (await _repository.GetByIdsAsync(ids, ct)).ToDictionary(m => m.Id, MapToDto);
        // Preserve the requested order; skip ids that no longer exist.
        return ids.Where(found.ContainsKey).Select(id => found[id]).ToList();
    }

    private static string ResolveMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return "file";
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "image";
        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return "video";
        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "audio";
        return "file";
    }

    private static MediaAssetDto MapToDto(MediaAsset a) => new()
    {
        Id = a.Id,
        FileName = a.FileName,
        OriginalName = a.OriginalName,
        Url = a.FilePath,
        MediaType = a.MediaType,
        MimeType = a.MimeType,
        FileSizeBytes = a.FileSizeBytes,
        Width = a.Width,
        Height = a.Height,
        AltText = a.AltText,
        CreatedAt = a.CreatedAt
    };
}
