namespace VaryoCms.Application.Common;

// Bound from appsettings.json section "Media".
// PublicBaseUrl (e.g. "https://cms.example.com") is prefixed onto the relative
// media_assets.file_path so URLs resolve correctly for consumers on a different
// origin (public API clients, RichText <img src> embeds). Empty = keep relative
// (fine when the consumer shares the CMS's own origin, e.g. local admin UI).
public class MediaSettings
{
    public string PublicBaseUrl { get; set; } = "";

    // Prefixes PublicBaseUrl onto a relative media path. No-op if PublicBaseUrl isn't a real
    // http(s) URL (unset, or still the appsettings.Production.json placeholder), the path is
    // null/empty, or the path is already absolute.
    public string? ToAbsolute(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl)) return relativeUrl;
        if (relativeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relativeUrl;
        if (!PublicBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !PublicBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relativeUrl;
        return PublicBaseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
    }
}
