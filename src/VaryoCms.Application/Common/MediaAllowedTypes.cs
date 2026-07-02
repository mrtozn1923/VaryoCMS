namespace VaryoCms.Application.Common;

public static class MediaAllowedTypes
{
    // Single source of truth: validated MIME type → server-controlled file extension.
    // The stored extension is ALWAYS taken from this map — never from the user-supplied filename —
    // so an allowed MIME can never be paired with an attacker-chosen extension (e.g. image/jpg + .html).
    // SVG is intentionally excluded: it is an active-content (script-capable) format served inline
    // from /uploads and would be a stored-XSS vector. Likewise no text/html, text/xml or scripts.
    private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images (raster only — SVG deliberately omitted)
        ["image/jpeg"]  = ".jpg",  ["image/jpg"]  = ".jpg",
        ["image/png"]   = ".png",  ["image/gif"]  = ".gif",
        ["image/webp"]  = ".webp", ["image/bmp"]  = ".bmp",
        ["image/tiff"]  = ".tiff", ["image/x-icon"] = ".ico",
        // Video
        ["video/mp4"]        = ".mp4", ["video/quicktime"]  = ".mov",
        ["video/x-msvideo"]  = ".avi", ["video/x-matroska"] = ".mkv",
        ["video/webm"]       = ".webm", ["video/x-ms-wmv"]  = ".wmv",
        ["video/mpeg"]       = ".mpeg",
        // Audio
        ["audio/mpeg"] = ".mp3", ["audio/mp3"]  = ".mp3",
        ["audio/wav"]  = ".wav", ["audio/x-wav"] = ".wav",
        ["audio/ogg"]  = ".ogg", ["audio/flac"] = ".flac",
        ["audio/aac"]  = ".aac", ["audio/mp4"]  = ".m4a",
        ["audio/x-m4a"] = ".m4a", ["audio/webm"] = ".weba",
        // Documents & archives
        ["application/pdf"]    = ".pdf",
        ["application/msword"] = ".doc",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
        ["application/vnd.ms-excel"] = ".xls",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ".xlsx",
        ["application/vnd.ms-powerpoint"] = ".ppt",
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = ".pptx",
        ["text/plain"] = ".txt", ["text/csv"] = ".csv",
        ["application/json"] = ".json",
        ["application/zip"] = ".zip", ["application/x-zip-compressed"] = ".zip",
    };

    public static bool IsAllowed(string? mimeType) =>
        !string.IsNullOrWhiteSpace(mimeType) && MimeToExtension.ContainsKey(mimeType);

    // Resolves the server-controlled extension for a validated MIME type. Returns false for any
    // MIME not in the allow-map, so callers must reject rather than trust the client filename.
    public static bool TryGetExtension(string? mimeType, out string extension)
    {
        if (!string.IsNullOrWhiteSpace(mimeType) && MimeToExtension.TryGetValue(mimeType, out string? ext))
        {
            extension = ext;
            return true;
        }
        extension = string.Empty;
        return false;
    }

    // Used as the <input accept="..."> attribute value in the upload form.
    public const string AcceptAttribute =
        "image/*,video/*,audio/*," +
        "application/pdf,.pdf," +
        ".doc,.docx,.xls,.xlsx,.ppt,.pptx," +
        "text/plain,.txt,.csv,.json,.zip";
}
