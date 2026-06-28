using VaryoCms.Application.Common;

namespace VaryoCms.Application.Interfaces;

// Persists uploaded files under per-tenant folders. Implemented by Infrastructure (local disk for now,
// swappable to S3 later).
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, int tenantId, CancellationToken ct = default);
    // Preferred: pass mimeType so the implementation can derive a server-controlled extension.
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, int tenantId, string? mimeType, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
    // Opens an existing stored file for reading, or null if it doesn't exist.
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default);
}
