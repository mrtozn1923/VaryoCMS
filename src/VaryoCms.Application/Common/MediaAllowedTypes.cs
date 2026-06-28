namespace VaryoCms.Application.Common;

public static class MediaAllowedTypes
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
        "image/svg+xml", "image/bmp", "image/tiff", "image/x-icon",
        // Video
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska",
        "video/webm", "video/x-ms-wmv", "video/mpeg",
        // Audio
        "audio/mpeg", "audio/mp3", "audio/wav", "audio/x-wav", "audio/ogg",
        "audio/flac", "audio/aac", "audio/mp4", "audio/x-m4a",
        // Documents & archives
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain", "text/csv",
        "application/json", "application/zip", "application/x-zip-compressed",
    };

    public static bool IsAllowed(string? mimeType) =>
        !string.IsNullOrWhiteSpace(mimeType) && Allowed.Contains(mimeType);

    // Used as the <input accept="..."> attribute value in the upload form.
    public const string AcceptAttribute =
        "image/*,video/*,audio/*," +
        "application/pdf,.pdf," +
        ".doc,.docx,.xls,.xlsx,.ppt,.pptx," +
        "text/plain,.txt,.csv,.json,.zip";
}
