using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;

namespace VaryoCms.Infrastructure.Storage;

// Saves files to the web root under /uploads/{tenantId}. Swap for an S3-backed implementation later.
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private readonly string _requestPath;   // e.g. "/uploads"
    private readonly string _uploadRoot;    // canonical path for path-traversal guard

    public LocalFileStorageService(string webRootPath, string requestPath = "/uploads")
    {
        _webRootPath = webRootPath;
        _requestPath = requestPath.TrimEnd('/');
        _uploadRoot  = Path.GetFullPath(Path.Combine(webRootPath, requestPath.TrimStart('/')));
    }

    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, int tenantId,
        CancellationToken ct = default)
        => await SaveAsync(content, originalFileName, tenantId, mimeType: null, ct);

    // Preferred overload: pass the server-validated MIME type so the extension is server-controlled.
    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, int tenantId,
        string? mimeType, CancellationToken ct = default)
    {
        string folderName = _requestPath.TrimStart('/');
        string tenantFolder = Path.Combine(_webRootPath, folderName, tenantId.ToString());
        Directory.CreateDirectory(tenantFolder);

        // Extension is ALWAYS server-controlled: resolved from the validated MIME type via the
        // single allow-map. We never fall back to the user-supplied filename extension — that
        // fallback previously let an allowed MIME (e.g. image/jpg) be stored as an attacker-chosen
        // .html/.svg and served inline (stored XSS). An unmapped MIME here means MediaService let
        // through something the map doesn't cover, which is a bug: reject loudly.
        if (!MediaAllowedTypes.TryGetExtension(mimeType, out string extension))
            throw new InvalidOperationException(
                $"Unsupported or unvalidated media MIME type: '{mimeType ?? "(null)"}'. Upload rejected.");

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string fullPath = Path.Combine(tenantFolder, fileName);

        await using (var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(file, ct);
        }

        long size = new FileInfo(fullPath).Length;
        string relativePath = $"{_requestPath}/{tenantId}/{fileName}";
        return new StoredFile(fileName, relativePath, size);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        string? fullPath = ResolveAndValidate(relativePath);
        if (fullPath is not null && File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        string? fullPath = ResolveAndValidate(relativePath);
        Stream? stream = fullPath is not null && File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    // Returns the canonical full path only when it stays within _uploadRoot (path-traversal guard).
    private string? ResolveAndValidate(string relativePath)
    {
        string trimmed  = relativePath.TrimStart('/');
        string fullPath = Path.GetFullPath(Path.Combine(_webRootPath, trimmed));
        return fullPath.StartsWith(_uploadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? fullPath : null;
    }
}
