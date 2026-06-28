using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;

namespace VaryoCms.Infrastructure.Storage;

// Saves files to the web root under /uploads/{tenantId}. Swap for an S3-backed implementation later.
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private readonly string _requestPath;   // e.g. "/uploads"
    private readonly string _uploadRoot;    // canonical path for path-traversal guard

    // Server-controlled extension derived from the validated MIME type (not user filename).
    private static readonly Dictionary<string, string> MimeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"]        = ".jpg",  ["image/png"]        = ".png",
        ["image/webp"]        = ".webp", ["image/gif"]        = ".gif",
        ["image/svg+xml"]     = ".svg",
        ["video/mp4"]         = ".mp4",  ["video/webm"]       = ".webm",
        ["video/quicktime"]   = ".mov",
        ["audio/mpeg"]        = ".mp3",  ["audio/ogg"]        = ".ogg",
        ["audio/wav"]         = ".wav",  ["audio/webm"]       = ".weba",
        ["application/pdf"]   = ".pdf",
        ["application/msword"]= ".doc",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
        ["application/vnd.ms-excel"] = ".xls",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ".xlsx",
        ["text/plain"]        = ".txt",
    };

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

        // Extension: MIME-derived (server-controlled) → sanitized original fallback.
        string extension = mimeType is not null && MimeExtensions.TryGetValue(mimeType, out string? mapped)
            ? mapped
            : SanitizeExtension(Path.GetExtension(originalFileName));

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

    // Allows only ASCII alphanumeric extensions to block executable-looking names (.php, .aspx, …).
    private static string SanitizeExtension(string rawExt)
    {
        if (string.IsNullOrWhiteSpace(rawExt)) return string.Empty;
        string clean = rawExt.TrimStart('.');
        return clean.All(char.IsAsciiLetterOrDigit) && clean.Length is > 0 and <= 8
            ? $".{clean.ToLowerInvariant()}"
            : string.Empty;
    }
}
