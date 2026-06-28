using System.Text.RegularExpressions;

namespace VaryoCms.Application.Common;

// Server-side authority for slug generation. Mirrors the TR_MAP + kebab-case logic in site.js.
public static class Slugifier
{
    private static readonly Dictionary<char, char> TrMap = new()
    {
        { 'ğ', 'g' }, { 'ü', 'u' }, { 'ş', 's' }, { 'ı', 'i' }, { 'ö', 'o' }, { 'ç', 'c' },
        { 'Ğ', 'g' }, { 'Ü', 'u' }, { 'Ş', 's' }, { 'İ', 'i' }, { 'Ö', 'o' }, { 'Ç', 'c' }
    };

    public static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var mapped = new System.Text.StringBuilder(input.Length);
        foreach (char c in input)
            mapped.Append(TrMap.TryGetValue(c, out char r) ? r : c);

        return Regex.Replace(
            Regex.Replace(
                Regex.Replace(mapped.ToString().ToLowerInvariant(), @"[^a-z0-9\s\-]", "").Trim(),
                @"\s+", "-"),
            @"-+", "-");
    }

    // Converts a kebab-case slug (or alias) to camelCase for use as a public API key.
    // "hero-title" → "heroTitle", "seo-meta-desc" → "seoMetaDesc", "title" → "title"
    public static string ToCamelCase(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return slug ?? string.Empty;
        var parts = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return slug;
        var sb = new System.Text.StringBuilder(slug.Length);
        sb.Append(parts[0].ToLowerInvariant());
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            sb.Append(char.ToUpperInvariant(parts[i][0]));
            if (parts[i].Length > 1)
                sb.Append(parts[i][1..].ToLowerInvariant());
        }
        return sb.ToString();
    }
}
